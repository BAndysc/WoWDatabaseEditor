using TheEngine;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheEngine.Utils.ImGuiHelper;
using TheMaths;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Managers
{
    public class CameraManager
    {
        private readonly Engine engine;
        private readonly ICameraManager engineCamera;
        private readonly IInputManager inputManager;
        private readonly IUIManager uiManager;
        private readonly TimeManager timeManager;
        private readonly RaycastSystem raycastSystem;
        // NB: historical naming quirk kept throughout this file: `yaw` is the VERTICAL angle
        // (0 = straight down, 179 = up) and `pitch` is the HEADING around Z. Rotation is
        // FromEuler(0, pitch, yaw) = Rz(pitch)·Rx(yaw), so view forward =
        // (-sin yaw · sin pitch, sin yaw · cos pitch, -cos yaw).
        private float pitch;
        private float yaw = 26.9f + 90;

        private SimpleBox coordNotificationBox;

        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        private float currentSpeed = 0;

        // AFK stress test: MAPRENDERER_AUTOPILOT=1 flies the camera forward every frame and
        // slowly turns, to continuously stream chunks/models in and out. MAPRENDERER_AUTOPILOT_GC=1
        // additionally forces a GC each frame so the WeakReference-cached textures are finalized
        // and disposed promptly - the path that used to dangle bindless descriptors and GPU-fault.
        private static readonly bool Autopilot = Environment.GetEnvironmentVariable("MAPRENDERER_AUTOPILOT") == "1";
        private static readonly bool AutopilotGc = Environment.GetEnvironmentVariable("MAPRENDERER_AUTOPILOT_GC") == "1";
        private float autopilotTime;

        public CameraManager(Engine engine,
            ICameraManager engineCamera,
            IInputManager inputManager,
            IUIManager uiManager,
            TimeManager timeManager,
            RaycastSystem raycastSystem)
        {
            this.engine = engine;
            this.engineCamera = engineCamera;
            this.inputManager = inputManager;
            this.uiManager = uiManager;
            this.timeManager = timeManager;
            this.raycastSystem = raycastSystem;
            //
            // Position = new Vector3(285.396f, -4746.17f, 9.48428f + 20);
            // Rotation = Utilities.LookRotation(
            //     new Vector3(223.698f, -4745.11f, 10.1022f + 20) - Position, Vectors.Up);
            //
            engineCamera.MainCamera.FOV = 75;
            this.coordNotificationBox = new SimpleBox(engine, BoxPlacement.BottomLeft);
        }

        public (int, int) CurrentChunk => Position.WoWPositionToChunk();

        // Vector3 writes are not atomic, so off-game-thread callers (the minimap drag)
        // publish a request applied at the start of the next Update instead
        private Vector3 requestedRelocation;
        private volatile bool hasRequestedRelocation;

        // "fly camera here": a nearby jump glides smoothly over SmoothFlyDurationMs instead of cutting;
        // a far jump still cuts (a half-second glide across the map is just a slow blur). delta is in
        // milliseconds here (see the /16 per-frame movement below), so the duration is too.
        private const float SmoothFlyMaxDistance = 150f; // yards - the glide-vs-cut threshold
        private const float SmoothFlyDurationMs = 500f;
        // framing offset: land a few yards BACK from the target (so we view it instead of sitting on
        // top of it) and a touch UP and LEFT of it - keeping the current look direction, that puts the
        // target comfortably in front of the camera, a little below and right of centre.
        private const float FrameBackYards = 12f;
        private const float FrameUpYards = 4f;
        private const float FrameLeftYards = 4f;
        private bool flyActive;
        private Vector3 flyFrom;
        private Vector3 flyTarget;
        private float flyElapsedMs;

        // Blender-style navigation: MMB orbit / Shift+MMB pan / wheel zoom (wheel while
        // mouselooking adjusts the fly speed instead, UE-style).
        private const float ZoomStepYards = 8f;
        private bool orbiting;
        private bool panning;
        private Vector3 orbitPivot;
        private float orbitRadius = 30f;
        private float userSpeedMultiplier = 1f;
        private float speedNoteTimerMs;

        /// <summary>Optional orbit pivot override (the spawn editor plugs the selected spawn in).
        /// Returning null falls back to the point the camera is looking at (static-world raycast
        /// along the view direction).</summary>
        public Func<Vector3?>? OrbitPivotProvider { get; set; }

        /// <summary>Camera view forward in world space (derived from the current angles).</summary>
        public Vector3 Forward => Vectors.Down.Multiply(Rotation);

        /// <summary>Points the camera at <paramref name="direction"/> by rewriting the two euler
        /// angles this camera is driven by (see the naming note at the top of the class).</summary>
        private void FaceDirection(Vector3 direction)
        {
            if (direction.LengthSquared() < 1e-8f)
                return;
            var dir = Vectors.Normalize(direction);
            yaw = MathF.Acos(Math.Clamp(-dir.Z, -1f, 1f)) * (180f / MathF.PI);
            if (dir.X * dir.X + dir.Y * dir.Y > 1e-8f)
                pitch = MathF.Atan2(-dir.X, dir.Y) * (180f / MathF.PI);
            yaw = Math.Clamp(yaw, 1, 179);
            Rotation = Utilities.FromEuler(0, pitch, yaw);
        }

        /// <summary>Distance to whatever the camera is looking at (static world), for
        /// distance-proportional pan/orbit; falls back to the last known radius.</summary>
        private float LookDistance()
        {
            var hit = raycastSystem.Raycast(new Ray(Position, Forward), null, false, Collisions.COLLISION_MASK_STATIC);
            if (hit.HasValue)
                orbitRadius = Math.Max(1f, (hit.Value.Item2 - Position).Length());
            return orbitRadius;
        }

        /// <summary>Move the camera to <paramref name="pos"/>. <paramref name="flyHere"/> (the "fly
        /// camera here" UI actions) frames the target from a few yards back/up/left instead of landing
        /// on it, and eases a NEARBY jump over ~0.5s instead of cutting (a far one still cuts). Off it
        /// (the default), every caller - map-load teleports, the per-frame sniff follow cam, minimap
        /// drag - keeps the old land-exactly-here, instant behaviour.</summary>
        public void Relocate(Vector3 pos, bool flyHere = false)
        {
            // NB: FrameFor reads Rotation, so it must only run on the game thread; flyHere is only ever
            // passed by the on-thread SetMap path, never by the off-thread minimap drag.
            if (flyHere)
                pos = FrameFor(pos);

            if (engine.IsOnGameThread)
            {
                if (flyHere && Vector3.DistanceSquared(Position, pos) <= SmoothFlyMaxDistance * SmoothFlyMaxDistance)
                {
                    // glide from wherever we are now (works even mid-glide - it just re-aims)
                    flyFrom = Position;
                    flyTarget = pos;
                    flyElapsedMs = 0;
                    flyActive = true;
                }
                else
                {
                    flyActive = false;
                    Position = pos;
                }
            }
            else
            {
                requestedRelocation = pos;
                hasRequestedRelocation = true;
            }
        }

        // A pleasant viewing spot for a fly-to target: back along the current view direction (so the
        // target sits ahead of us), plus a small rise and a lean to the left. Because the offset is
        // built from the camera's OWN basis, the target lands at a fixed spot in view (dead ahead, a
        // little below and right of centre) no matter which way the camera is currently facing - so
        // it's always framed, never behind us.
        private Vector3 FrameFor(Vector3 target)
        {
            var rot = Rotation;
            // camera view basis, expressed via the world-axis constants the way Rotation maps them -
            // exactly as the WASDEQ movement above reads them: W=Down=forward, E=Left=up, A=Backward=left.
            var forward = Vector3.Transform(Vectors.Down, rot);     // into the screen
            var up = Vector3.Transform(Vectors.Left, rot);          // screen up
            var left = Vector3.Transform(Vectors.Backward, rot);    // screen left
            return target - forward * FrameBackYards + up * FrameUpYards + left * FrameLeftYards;
        }

        public void Update(float delta)
        {
            if (hasRequestedRelocation)
            {
                hasRequestedRelocation = false;
                flyActive = false; // an explicit (off-thread) relocate wins over an in-progress glide
                Position = requestedRelocation;
            }

            if (Autopilot)
            {
                AutopilotUpdate(delta);
                return;
            }

            if (flyActive && UpdateSmoothFly(delta))
                return;

            UpdateOrbitAndPan();
            UpdateWheel(delta);

            if (!orbiting && inputManager.Mouse.IsMouseDown(MouseButton.Right))
            {
                yaw += inputManager.Mouse.Delta.Y;
                pitch += inputManager.Mouse.Delta.X;
                yaw = Math.Clamp(yaw, 0, 179);
            }

            Rotation = Utilities.FromEuler(0, pitch, yaw);
            var movement = inputManager.Keyboard.GetAxis(Vectors.Down, Key.W, Key.S) +
                           inputManager.Keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) +
                           inputManager.Keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);

            if (movement.LengthSquared() == 0)
                currentSpeed = Math.Max(0, currentSpeed - delta * 0.01f);
            else if (currentSpeed < 1)
                currentSpeed = Math.Min(1, currentSpeed + delta * 0.001f * 0.5f);

            movement = Vectors.Normalize(movement);
            float modifier = 0.4f;
            if (inputManager.Keyboard.IsDown(Key.LeftShift))
                modifier = 15;
            if (inputManager.Keyboard.IsDown(Key.N))
                modifier = 0.1f;
            modifier *= userSpeedMultiplier;
            float speed = 1 * (delta / 16.0f) * modifier;
            Position += movement.Multiply(Rotation) * speed * currentSpeed;

            var camera = engineCamera.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;
        }

        // MMB drag = orbit around the pivot (selection if provided, else whatever the camera looks
        // at); Shift+MMB = pan (the world follows the cursor). Starts only on a press over the
        // world itself; keeps going even if the cursor leaves the view mid-drag.
        private void UpdateOrbitAndPan()
        {
            if (!inputManager.Mouse.RawIsMouseDown(MouseButton.Middle))
            {
                orbiting = false;
                panning = false;
            }
            else if (!orbiting && !panning && inputManager.Mouse.HasJustClicked(MouseButton.Middle))
            {
                if (inputManager.Keyboard.IsDown(Key.LeftShift) || inputManager.Keyboard.IsDown(Key.RightShift))
                {
                    panning = true;
                    LookDistance(); // refresh the distance the pan speed scales with
                }
                else
                {
                    orbiting = true;
                    var custom = OrbitPivotProvider?.Invoke();
                    if (custom.HasValue)
                    {
                        orbitPivot = custom.Value;
                        orbitRadius = Math.Max(1f, (Position - orbitPivot).Length());
                        FaceDirection(orbitPivot - Position);
                    }
                    else
                    {
                        orbitPivot = Position + Forward * LookDistance();
                    }
                }
            }

            if (orbiting)
            {
                // same axis mapping as right-drag mouselook, but the position swings around the pivot
                pitch += inputManager.Mouse.Delta.X;
                yaw = Math.Clamp(yaw + inputManager.Mouse.Delta.Y, 1, 179);
                Rotation = Utilities.FromEuler(0, pitch, yaw);
                Position = orbitPivot - Forward * orbitRadius;
            }
            else if (panning)
            {
                var d = inputManager.Mouse.Delta;
                if (d.LengthSquared() > 0)
                {
                    var up = Vector3.Transform(Vectors.Left, Rotation);
                    var left = Vector3.Transform(Vectors.Backward, Rotation);
                    float scale = orbitRadius * 0.002f;
                    Position += (left * -d.X - up * d.Y) * scale;
                }
            }
        }

        // Wheel = dolly zoom along the view direction; while mouselooking (RMB held) it adjusts the
        // permanent fly-speed multiplier instead (echoed next to the coordinates for a moment).
        private void UpdateWheel(float delta)
        {
            if (speedNoteTimerMs > 0)
                speedNoteTimerMs -= delta;

            float wheel = inputManager.Mouse.ViewWheelDelta.Y;
            if (wheel == 0)
                return;

            if (inputManager.Mouse.IsMouseDown(MouseButton.Right))
            {
                userSpeedMultiplier = Math.Clamp(userSpeedMultiplier * MathF.Pow(1.15f, wheel), 0.05f, 20f);
                speedNoteTimerMs = 1500;
            }
            else
            {
                bool fast = inputManager.Keyboard.IsDown(Key.LeftShift) || inputManager.Keyboard.IsDown(Key.RightShift);
                Position += Forward * (wheel * ZoomStepYards * userSpeedMultiplier * (fast ? 3f : 1f));
            }
        }

        // Eases the camera from flyFrom to flyTarget. Returns true while it owns the camera this frame
        // (so Update skips normal input handling); false the moment the user grabs control back, so
        // WASD/right-drag resume instantly - a fly-to must never fight the user.
        private bool UpdateSmoothFly(float delta)
        {
            var moveAxis = inputManager.Keyboard.GetAxis(Vectors.Down, Key.W, Key.S) +
                           inputManager.Keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) +
                           inputManager.Keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);
            if (inputManager.Mouse.IsMouseDown(MouseButton.Right) || moveAxis.LengthSquared() > 0
                || inputManager.Mouse.ViewWheelDelta.Y != 0 || inputManager.Mouse.RawIsMouseDown(MouseButton.Middle))
            {
                flyActive = false;
                return false;
            }

            flyElapsedMs += delta;
            float t = Math.Clamp(flyElapsedMs / SmoothFlyDurationMs, 0f, 1f);
            float eased = t * t * (3f - 2f * t); // smoothstep ease-in-out
            Position = Vector3.Lerp(flyFrom, flyTarget, eased);
            if (t >= 1f)
                flyActive = false;

            var camera = engineCamera.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;
            return true;
        }

        private void AutopilotUpdate(float delta)
        {
            autopilotTime += delta;
            // NB: in this file `yaw` is the vertical angle and `pitch` is the heading (see Update).
            // Keep the vertical angle at the startup default (~level, looking slightly down at the
            // terrain) and weave only the heading, so we cross fresh chunks without pitching up.
            yaw = 116.9f;
            pitch = 40f * (float)Math.Sin(autopilotTime * 0.0001);
            Rotation = Utilities.FromEuler(0, pitch, yaw);

            // forward is the W direction (Vectors.Down in local space, see the WASD axis above);
            // flatten it onto the X/Y plane (Z is up) so the camera travels level and never
            // climbs, regardless of the look angle.
            var forward = Vectors.Down.Multiply(Rotation);
            var flat = Vectors.Normalize(new Vector3(forward.X, forward.Y, 0));
            float speed = (delta / 16.0f) * 0.8f; // gentle stroll
            Position += flat * speed;

            var camera = engineCamera.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;

            if (AutopilotGc)
                GC.Collect();
        }

        private string coordLabel = "";
        private (int x, int y, int z, int minutes, int speedNote) coordLabelKey = (int.MinValue, 0, 0, 0, 0);

        public void RenderGUI()
        {
            // rebuilt only when the displayed precision actually changes - this draws every frame
            var time = timeManager.Time;
            var key = ((int)(Position.X * 100), (int)(Position.Y * 100), (int)(Position.Z * 100),
                time.Hour * 60 + time.Minute,
                speedNoteTimerMs > 0 ? (int)(userSpeedMultiplier * 100) : -1);
            if (key != coordLabelKey)
            {
                coordLabelKey = key;
                coordLabel = $"X: {Position.X:0.00} Y: {Position.Y:0.00} Z: {Position.Z:0.00}  ·  {time.Hour:00}:{time.Minute:00}";
                if (speedNoteTimerMs > 0)
                    coordLabel += $"  ·  fly speed ×{userSpeedMultiplier:0.0#}";
            }
            coordNotificationBox.Draw(coordLabel);
        }
    }
}
