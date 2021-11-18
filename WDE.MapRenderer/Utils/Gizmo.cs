using System;
using TheEngine;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers;

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

        private static readonly Quaternion ArrowX = Quaternion.FromEuler(0, 180, 0);
        private static readonly Quaternion ArrowY = Quaternion.FromEuler(0, 90, 0);
        private static readonly Quaternion ArrowZ = Quaternion.FromEuler(90, 90, 0);
        
        private static readonly Quaternion PlaneX = Quaternion.FromEuler(180, 180, 180);
        private static readonly Quaternion PlaneY = Quaternion.FromEuler(0, 90, 0);
        private static readonly Quaternion PlaneZ = Quaternion.FromEuler(90, 90, 180);

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

        public HitType HitTest(IGameContext context, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.Zero;
            t.Position = position.Position;
            
            var ray = context.Engine.CameraManager.MainCamera.NormalizedScreenPointToRay(context.Engine.InputManager.Mouse.NormalizedPosition);

            t.Rotation = PlaneX;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0,  t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateZY;
            
            t.Rotation = PlaneY;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0, t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateXZ;
            
            t.Rotation = PlaneZ;
            if (Physics.RayIntersectsObject(dragPlaneMesh, 0, t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateXY;
            
            t.Rotation = ArrowX;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateX;
            
            t.Rotation = ArrowY;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateY;

            t.Rotation = ArrowZ;
            if (Physics.RayIntersectsObject(arrowMesh, 0, t.LocalToWorldMatrix, ray, out intersectionPoint))
                return HitType.TranslateZ;

            return HitType.None;
        }

        public void Render(IGameContext context)
        {
            InternalRender(context, 0.5f);
            InternalRender(context, 1);
        }

        private void InternalRender(IGameContext context, float alpha)
        {
            if (alpha < 1)
            {
                material.BlendingEnabled = true;
                material.SourceBlending = Blending.SrcAlpha;
                material.DestinationBlending = Blending.OneMinusSrcAlpha;
                material.DepthTesting = false;
                material.ZWrite = false;
            }
            else
            {
                material.BlendingEnabled = false;
                material.DepthTesting = true;
                material.ZWrite = true;
            }
            
            var dist = (position.Position - context.Engine.CameraManager.MainCamera.Transform.Position).Length();
            t.Position = position.Position;
            t.Scale = Vector3.One * (float)Math.Sqrt(Math.Clamp(dist, 0.5f, 500) / 15);
            // +X (wow)
            t.Rotation = ArrowX;
            material.SetUniform("objectColor", new Vector4(0, 0, 1, alpha));
            context.Engine.RenderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneX;
            context.Engine.RenderManager.Render(dragPlaneMesh, material, 0, t);
                

            // +Y (wow)
            t.Rotation = ArrowY;
            material.SetUniform("objectColor", new Vector4(0, 1, 0, alpha));
            context.Engine.RenderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneY;
            context.Engine.RenderManager.Render(dragPlaneMesh, material, 0, t);
                
            // +Z (wow)
            t.Rotation = ArrowZ;
            material.SetUniform("objectColor", new Vector4(1, 0, 0, alpha));
            context.Engine.RenderManager.Render(arrowMesh, material, 0, t);
            t.Rotation = PlaneZ;
            context.Engine.RenderManager.Render(dragPlaneMesh, material, 0, t);
        }
    }
}