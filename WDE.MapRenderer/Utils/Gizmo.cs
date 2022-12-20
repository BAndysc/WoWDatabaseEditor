using Avalonia.Input;
using Avalonia.Threading;
using TheEngine;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Utils
{
    public class Gizmo : System.IDisposable
    {
        private readonly IMeshManager meshManager;
        public readonly Transform position = new();
        private readonly IMesh arrowMesh;
        private readonly IMesh dragPlaneMesh;
        private readonly Material material;
        private readonly Transform t = new Transform();
        private bool ownsMeshes;

        public Gizmo(IMeshManager meshManager, IMaterialManager materialManager)
        {
            this.meshManager = meshManager;
            arrowMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/arrow.obj").MeshData);
            dragPlaneMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/dragPlane.obj").MeshData);
            this.material = materialManager.CreateMaterial("data/gizmo.json");
            ownsMeshes = true;
        }

        public void Dispose()
        {
            if (ownsMeshes)
            {
                meshManager.DisposeMesh(arrowMesh);
                meshManager.DisposeMesh(dragPlaneMesh);
                ownsMeshes = false;
            }
        }

        public Gizmo(IMesh arrowMesh, IMesh dragPlaneMesh, Material material)
        {
            this.arrowMesh = arrowMesh;
            this.dragPlaneMesh = dragPlaneMesh;
            this.material = material;
        }

        private static readonly Quaternion ArrowX = Utilities.LookRotation(Vectors.Left, Vectors.Up);
        private static readonly Quaternion ArrowY = Utilities.LookRotation(Vectors.Backward, Vectors.Up);
        private static readonly Quaternion ArrowZ = Utilities.FromEuler(0, 0, -90);
        
        private static readonly Quaternion PlaneZ = Utilities.LookRotation(Vectors.Forward, Vectors.Up);
        private static readonly Quaternion PlaneY = Utilities.FromEuler(-90, 0, -90);
        private static readonly Quaternion PlaneX = Utilities.LookRotation(Vectors.Up, Vectors.Up);

        // wow direction hit test
        public enum HitType
        {
            None,
            TranslateX,
            TranslateZY,
            TranslateZ,
            TranslateXY,
            TranslateY,
            TranslateXZ,
            Snap,
            RotationX,
            RotationY,
            RotationZ,
        }

        public HitType HitTest(IInputManager inputManager, ICameraManager cameraManager, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.Zero;
            t.Position = position.Position;
            
            var ray = cameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);

            t.Rotation = PlaneX;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0,  t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateZY;
            
            t.Rotation = PlaneY;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0, t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateXZ;
            
            t.Rotation = PlaneZ;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0, t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateXY;
            
            t.Rotation = ArrowX;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateX;
            
            t.Rotation = ArrowY;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateY;

            t.Rotation = ArrowZ;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, null, out intersectionPoint))
                return HitType.TranslateZ;

            return HitType.None;
        }

        public void Render(ICameraManager cameraManager, IRenderManager renderManager)
        {
            InternalRender(cameraManager, renderManager, false);
            InternalRender(cameraManager, renderManager, true);
        }

        private void InternalRender(ICameraManager cameraManager, IRenderManager renderManager, bool transparent)
        {
            if (transparent)
            {
                material.BlendingEnabled = true;
                material.SourceBlending = Blending.SrcAlpha;
                material.DestinationBlending = Blending.OneMinusSrcAlpha;
                material.DepthTesting = DepthCompare.Greater;
                material.ZWrite = false;
            }
            else
            {
                material.BlendingEnabled = false;
                material.DepthTesting = DepthCompare.Lequal;
                material.ZWrite = true;
            }
            
            var dist = (position.Position - cameraManager.MainCamera.Transform.Position).Length();
            t.Position = position.Position;
            t.Scale = Vector3.One * (float)Math.Sqrt(Math.Clamp(dist, 0.5f, 500) / 15);
            // +X (wow)
            t.Rotation = ArrowX;
            material.SetUniform("objectColor", new Vector4(0, 0, 1, transparent ? 0.5f : 1f));
            renderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneX;
            renderManager.Render(dragPlaneMesh, material, 0, t);
                

            // +Y (wow)
            t.Rotation = ArrowY;
            material.SetUniform("objectColor", new Vector4(0, 1, 0, transparent ? 0.5f : 1f));
            renderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneY;
            renderManager.Render(dragPlaneMesh, material, 0, t);
                
            // +Z (wow)
            t.Rotation = ArrowZ;
            material.SetUniform("objectColor", new Vector4(1, 0, 0, transparent ? 0.5f : 1f));
            renderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneZ;
            renderManager.Render(dragPlaneMesh, material, 0, t);
        }
    }
    
    internal enum GizmoMode
    {
        NoDragging,
        MouseDrag,
        KeyboardDrag,
        Rotation
    }

    public enum RotationLockType
    {
        None,
        RotationX,
        RotationY,
        RotationZ,
    }
    
    public abstract class Dragger<T>
    {
        private readonly IMeshManager meshManager;
        private readonly IMaterialManager materialManager;
        private readonly ICameraManager cameraManager;
        private readonly IRenderManager renderManager;
        private readonly RaycastSystem raycastSystem;
        private readonly IInputManager inputManager = null!;
        private readonly uint collisionMask;
        private Gizmo gizmo = null!;
        
        private GizmoMode dragging = GizmoMode.NoDragging;

        private bool canFinishDragging;
        private TheMaths.Plane plane;
        private bool snappingMode;
        private Vector3? axis;
        private readonly List<(T item, Quaternion original_rotation)> rotable = new();
        private readonly List<(T item, Vector3 original_position, Vector3 offset)> draggable = new();
        private Vector3 originalTouch;
        private Vector2 originalTouch2D;
        public bool IsEnabled { get; set; }
        public bool CanRotate { get; set; }
        public RotationLockType RotationLock { get; set; }
        public Vector3 GizmoPosition { get; set; }
        
        public Dragger(IMeshManager meshManager,
            IMaterialManager materialManager,
            ICameraManager cameraManager,
            IRenderManager renderManager,
            RaycastSystem raycastSystem,
            IInputManager inputManager,
            uint collisionMask)
        {
            this.meshManager = meshManager;
            this.materialManager = materialManager;
            this.cameraManager = cameraManager;
            this.renderManager = renderManager;
            this.raycastSystem = raycastSystem;
            this.inputManager = inputManager;
            this.collisionMask = collisionMask;
            Initialize();
        }

        public void Dispose()
        {
            gizmo.Dispose();
        }
        
        public void Initialize()
        {
            gizmo = new Gizmo(meshManager, materialManager);
        }

        public void Render()
        {
            if (!IsEnabled)
                return;
            
            gizmo.position.Position = GizmoPosition + Vectors.Up;
            gizmo.Render(cameraManager, renderManager);

            if (dragging == GizmoMode.Rotation)
            {
                var from = GizmoPosition - axis.Value * 1000;
                var to = GizmoPosition + axis.Value * 1000;
                renderManager.DrawLine(from, to, new Vector4(1, 0, 0, 1));
            }
        }

        public abstract Quaternion GetRotation(T item);
        public abstract Vector3 GetPosition(T item);
        protected abstract void Move(T item, Vector3 position);
        protected abstract void Rotate(T item, Quaternion rotation);
        protected abstract IReadOnlyList<T>? PointsToDrag();
        protected abstract void FinishRotation(IEnumerable<T> objects);
        protected abstract void FinishDragging(IEnumerable<T> objects);

        public void StartXYDrag()
        {
            var ray = cameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
            StartDragging(Gizmo.HitType.TranslateXY, ref ray, GizmoMode.KeyboardDrag);
        }
        
        public void StartSnapDrag()
        {
            Ray ray = new Ray();
            StartDragging(Gizmo.HitType.Snap, ref ray, GizmoMode.KeyboardDrag);
        }
        
        public void Finish()
        {
            if (dragging is GizmoMode.KeyboardDrag or GizmoMode.MouseDrag)
                FinishDrag();
            else if (dragging == GizmoMode.Rotation)
                FinishRotation();
        }
        
        public bool Update(float f)
        {
            var ray = cameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);

            var stopDrag = (dragging == GizmoMode.MouseDrag && !inputManager.Mouse.IsMouseDown(MouseButton.Left)) ||
                           (dragging == GizmoMode.KeyboardDrag && inputManager.Mouse.HasJustClicked(MouseButton.Left));
            var stopRotate = (dragging == GizmoMode.Rotation && inputManager.Mouse.HasJustClicked(MouseButton.Left));
            
            if (stopDrag || stopRotate)
                dragging = GizmoMode.NoDragging;

            if (draggable.Count > 0 && stopDrag)
            {
                FinishDrag();
                return true;
            }

            if (rotable.Count > 0 && stopRotate)
            {
                FinishRotation();
                return true;
            }
            
            if (dragging != GizmoMode.NoDragging && inputManager.Keyboard.JustPressed(Key.Escape))
            {
                foreach (var d in draggable)
                    Move(d.item, d.original_position);
                foreach (var r in rotable)
                    Rotate(r.item, r.original_rotation);
                dragging = GizmoMode.NoDragging;
            }

            if (IsEnabled && dragging is GizmoMode.KeyboardDrag or GizmoMode.MouseDrag)
            {
                if (snappingMode)
                {
                    var hit = raycastSystem.RaycastMouse(0);
                    if (hit.HasValue)
                    {
                        foreach (var item in draggable)
                        {
                            Move(item.item, hit.Value.Item2);
                        }
                    }
                }
                else
                {
                    if (plane.Intersects(ref ray, out Vector3 originalTouch))
                    {
                        foreach (var item in draggable)
                        {
                            var touch = originalTouch;
                            if (axis.HasValue)
                                touch = item.original_position + axis.Value * Vector3.Dot(touch - this.originalTouch, axis.Value);
                            else
                                touch += item.offset;
                            Move(item.item, touch);   
                        }
                    }   
                }
            }

            if (IsEnabled && dragging is GizmoMode.Rotation)
            {
                if (plane.Intersects(ref ray, out Vector3 touch))
                {
                    for (var index = 0; index < rotable.Count; index++)
                    {
                        var item = rotable[index];
                        var rot = Quaternion.CreateFromAxisAngle(axis.Value, Vector3.Dot(touch - originalTouch, axis.Value)) * item.original_rotation;
                        Rotate(item.item, rot);
                    }
                }
            }

            if (dragging == GizmoMode.KeyboardDrag && inputManager.Keyboard.JustPressed(Key.G))
            {
                dragging = GizmoMode.NoDragging;
                List<(Entity, Vector3)> result = new();
                for (var index = 0; index < draggable.Count; index++)
                {
                    var dragged = draggable[index];
                    var position = GetPosition(dragged.item);
                    raycastSystem.RaycastAll(new Ray(position.WithZ(4000), Vectors.Down), position, result,
                        collisionMask);
                    if (result.Count > 0)
                    {
                        float minDistance = float.MaxValue;
                        foreach (var r in result)
                        {
                            float diff = Math.Abs(r.Item2.Z - position.Z);
                            if (diff < minDistance)
                            {
                                minDistance = diff;
                                var destPosition = new Vector3(position.X, position.Y, r.Item2.Z);
                                draggable[index] = (dragged.item, dragged.original_position, destPosition - dragged.original_position);
                                Move(dragged.item, destPosition);
                            }
                        }

                        result.Clear();
                    }
                }

                FinishDrag();
                return false;
            }
            
            if (dragging is not GizmoMode.KeyboardDrag or GizmoMode.MouseDrag && inputManager.Keyboard.JustPressed(Key.G))
            {
                if (dragging == GizmoMode.Rotation)
                    FinishRotation();
                StartDragging(Gizmo.HitType.TranslateXY, ref ray, GizmoMode.KeyboardDrag);
            }

            if (dragging != GizmoMode.Rotation && CanRotate && inputManager.Keyboard.JustPressed(Key.R))
            {
                if (dragging != GizmoMode.NoDragging)
                    FinishDrag();
                StartRotation(Gizmo.HitType.RotationZ, ref ray, GizmoMode.Rotation);
            }

            if (dragging == GizmoMode.Rotation)
            {
                if (inputManager.Keyboard.JustPressed(Key.X))
                    StartRotation(Gizmo.HitType.RotationX, ref ray, GizmoMode.Rotation);
                else if (inputManager.Keyboard.JustPressed(Key.Y))
                    StartRotation(Gizmo.HitType.RotationY, ref ray, GizmoMode.Rotation);
                else if (inputManager.Keyboard.JustPressed(Key.Z))
                    StartRotation(Gizmo.HitType.RotationZ, ref ray, GizmoMode.Rotation);
            }

            if (dragging == GizmoMode.KeyboardDrag)
            {
                Gizmo.HitType hitType = Gizmo.HitType.None;
                if (inputManager.Keyboard.JustPressed(Key.X))
                    hitType = inputManager.Keyboard.IsDown(Key.LeftShift) ? Gizmo.HitType.TranslateZY : Gizmo.HitType.TranslateX;
                else if (inputManager.Keyboard.JustPressed(Key.Y))
                    hitType = inputManager.Keyboard.IsDown(Key.LeftShift) ? Gizmo.HitType.TranslateXZ : Gizmo.HitType.TranslateY;
                else if (inputManager.Keyboard.JustPressed(Key.Z))
                    hitType = inputManager.Keyboard.IsDown(Key.LeftShift) ? Gizmo.HitType.TranslateXY : Gizmo.HitType.TranslateZ;
                
                if (hitType != Gizmo.HitType.None)
                    StartDragging(hitType, ref ray, GizmoMode.KeyboardDrag);
            }

            if (inputManager.Mouse.HasJustClicked(MouseButton.Left) && !stopDrag)
            {
                if (IsEnabled && dragging == GizmoMode.NoDragging)
                {
                    var result = gizmo.HitTest(inputManager, cameraManager, out _);
                    if (result != Gizmo.HitType.None)
                        StartDragging(result, ref ray, GizmoMode.MouseDrag);
                }
            }

            if (dragging == GizmoMode.MouseDrag && !canFinishDragging &&
                Vector2.Distance(inputManager.Mouse.LastClickScreenPosition, inputManager.Mouse.ScreenPoint) > 3)
                canFinishDragging = true;

            return IsDragging();
        }

        private void FinishRotation()
        {
            FinishRotation(rotable.Select(x => x.item));
            rotable.Clear();
        }
        
        private void FinishDrag()
        {
            if (canFinishDragging)
                FinishDragging(draggable.Select(x => x.item));
            draggable.Clear();
        }

        private void GetDragAxisAndPlane(Gizmo.HitType mode, out Vector3? axis, out TheMaths.Plane plane)
        {
            axis = null;
            plane = default;
            switch (mode)
            {
                case Gizmo.HitType.TranslateX:
                    axis = new Vector3(1, 0, 0);
                    break;
                case Gizmo.HitType.TranslateZY:
                    axis = null;
                    plane = new TheMaths.Plane(gizmo.position.Position, new Vector3(1, 0, 0));
                    break;
                case Gizmo.HitType.TranslateZ:
                    axis = new Vector3(0, 0, 1);
                    break;
                case Gizmo.HitType.TranslateXY:
                    axis = null;
                    plane = new TheMaths.Plane(gizmo.position.Position, new Vector3(0, 0, 1));
                    break;
                case Gizmo.HitType.TranslateY:
                    axis = new Vector3(0, 1, 0);
                    break;
                case Gizmo.HitType.TranslateXZ:
                    axis = null;
                    plane = new TheMaths.Plane(gizmo.position.Position, new Vector3(0, 1, 0));
                    break;
                case Gizmo.HitType.RotationX:
                    axis = new Vector3(1, 0, 0);
                    break;
                case Gizmo.HitType.RotationZ:
                    axis = new Vector3(0, 0, 1);
                    break;
                case Gizmo.HitType.RotationY:
                    axis = new Vector3(0, 1, 0);
                    break;
            }

            if (axis.HasValue)
            {
                Vector3 planeTangent = Vector3.Cross(axis.Value, gizmo.position.Position - cameraManager.MainCamera.Transform.Position);
                Vector3 planeNormal = Vector3.Cross(axis.Value, planeTangent);
                plane = new TheMaths.Plane(gizmo.position.Position, planeNormal);
            }
        }

        private bool IsOnTheGround()
        {
            var toDrag = PointsToDrag();
            if (toDrag == null || toDrag.Count == 0)
                return false;

            var position = GetPosition(toDrag[0]);
            var hit = raycastSystem.Raycast(new Ray(position + Vectors.Up * 3, Vectors.Down), null);

            if (!hit.HasValue)
                return false;
            
            return Math.Abs(position.Z - hit.Value.Item2.Z) < 3f;
        }

        private void StartDragging(Gizmo.HitType hitType, ref Ray ray, GizmoMode gizmoMode)
        {
            if (hitType == Gizmo.HitType.Snap || (IsOnTheGround() && hitType == Gizmo.HitType.TranslateXY))
                StartDragging(Vector3.Zero, null, plane, gizmoMode, true);
            else
            {
                GetDragAxisAndPlane(hitType, out var axis, out var plane);
                if (plane.Intersects(ref ray, out Vector3 touchPoint))
                    StartDragging(touchPoint, axis, plane, gizmoMode, false);   
            }
        }
        
        private void StartDragging(Vector3 startTouchPoint, Vector3? axis, TheMaths.Plane plane, GizmoMode gizmoMode, bool snapping)
        {
            this.snappingMode = snapping;
            originalTouch = startTouchPoint;
            dragging = gizmoMode;
            canFinishDragging = gizmoMode == GizmoMode.MouseDrag ? false : true;
            this.axis = axis;
            this.plane = plane;
            draggable.Clear();
            var toDrag = PointsToDrag();
            if (toDrag != null)
            {
                foreach (var selected in toDrag)
                {
                    var originalPosition = GetPosition(selected);
                    draggable.Add((selected, originalPosition, originalPosition - startTouchPoint));
                }   
            }
        }

        private void StartRotation(Gizmo.HitType hitType, ref Ray ray, GizmoMode gizmoMode)
        {
            if (RotationLock != RotationLockType.None)
            {
                switch (RotationLock)
                {
                    case RotationLockType.RotationX:
                        hitType = Gizmo.HitType.RotationX;
                        break;
                    case RotationLockType.RotationY:
                        hitType = Gizmo.HitType.RotationY;
                        break;
                    case RotationLockType.RotationZ:
                        hitType = Gizmo.HitType.RotationZ;
                        break;
                }
            }
            GetDragAxisAndPlane(hitType, out var axis, out var plane);
            if (plane.Intersects(ref ray, out Vector3 touchPoint))
                StartRotation(touchPoint, axis, plane, gizmoMode);
        }
        
        private void StartRotation(Vector3 startTouchPoint, Vector3? axis, TheMaths.Plane plane, GizmoMode gizmoMode)
        {
            rotable.Clear();
            originalTouch2D = inputManager.Mouse.NormalizedPosition;
            originalTouch = startTouchPoint;
            this.axis = axis;
            this.plane = plane;
            dragging = gizmoMode;
            var toDrag = PointsToDrag();
            if (toDrag != null)
            {
                foreach (var selected in toDrag)
                {
                    var originalRotation = GetRotation(selected);
                    rotable.Add((selected, originalRotation));
                }
            }
        }

        public bool IsDragging()
        {
            return dragging != GizmoMode.NoDragging;
        }
    }
}