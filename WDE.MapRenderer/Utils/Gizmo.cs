using TheEngine;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;

namespace WDE.MapRenderer.Utils
{
    public class Gizmo
    {
        public readonly Transform position = new();
        private readonly IMesh arrowMesh;
        private readonly IMesh dragPlaneMesh;
        private readonly Material material;
        private readonly Transform t = new Transform();

        public Gizmo(IMesh arrowMesh, IMesh dragPlaneMesh, Material material)
        {
            this.arrowMesh = arrowMesh;
            this.dragPlaneMesh = dragPlaneMesh;
            this.material = material;
        }

        private static readonly Quaternion ArrowX = Quaternion.LookRotation(Vector3.Left, Vector3.Up);
        private static readonly Quaternion ArrowY = Quaternion.LookRotation(Vector3.Forward, Vector3.Up);
        private static readonly Quaternion ArrowZ = Quaternion.FromEuler(0, 0, -90);
        
        private static readonly Quaternion PlaneX = Quaternion.LookRotation(Vector3.Forward, Vector3.Up);
        private static readonly Quaternion PlaneY = Quaternion.LookRotation(Vector3.Left, Vector3.Up);
        private static readonly Quaternion PlaneZ = Quaternion.LookRotation(Vector3.Up, Vector3.Up);

        // wow direction hit test
        public enum HitType
        {
            None,
            TranslateX,
            TranslateZY,
            TranslateZ,
            TranslateXY,
            TranslateY,
            TranslateXZ
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
            InternalRender(cameraManager, renderManager, true);
            InternalRender(cameraManager, renderManager, false);
        }

        private void InternalRender(ICameraManager cameraManager, IRenderManager renderManager, bool transparent)
        {
            if (transparent)
            {
                material.BlendingEnabled = true;
                material.SourceBlending = Blending.SrcAlpha;
                material.DestinationBlending = Blending.OneMinusSrcAlpha;
                material.DepthTesting = DepthCompare.Always;
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
}