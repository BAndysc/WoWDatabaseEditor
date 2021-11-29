using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class AreaTriggerManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private IMesh boxMesh;
        private IMesh sphereMesh;
        private Material transcluentMaterial;
        private Material wireframe, wireframeBehind;
        private Transform t = new Transform();

        public static float AreaTriggerVisibilityDistanceSquare = 900 * 900;
        
        public AreaTriggerManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            boxMesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            sphereMesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);
            
            wireframe = gameContext.Engine.MaterialManager.CreateMaterial("data/wireframe.json");
            wireframe.SetUniform("Width", 1);
            wireframe.SetUniform("Color", Vector4.One);
            wireframe.ZWrite = false;
            wireframe.DepthTesting = DepthCompare.Lequal;
            
            wireframeBehind = gameContext.Engine.MaterialManager.CreateMaterial("data/wireframe.json");
            wireframeBehind.SetUniform("Width", 0.5f);
            wireframeBehind.SetUniform("Color", new Vector4(1, 1, 1, 0.1f));
            wireframeBehind.ZWrite = false;
            wireframeBehind.DepthTesting = DepthCompare.Greater;
            wireframeBehind.BlendingEnabled = true;
            wireframeBehind.SourceBlending = Blending.SrcAlpha;
            wireframeBehind.DestinationBlending = Blending.OneMinusSrcAlpha;

            transcluentMaterial = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            transcluentMaterial.BlendingEnabled = true;
            transcluentMaterial.SourceBlending = Blending.SrcAlpha;
            transcluentMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            transcluentMaterial.DepthTesting = DepthCompare.Lequal;
            transcluentMaterial.ZWrite = false;
            transcluentMaterial.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.3f));
        }

        public void Dispose()
        {
            gameContext.Engine.MeshManager.DisposeMesh(boxMesh);
            gameContext.Engine.MeshManager.DisposeMesh(sphereMesh);
        }
        
        public void Render()
        {
            foreach (var areaTrigger in gameContext.DbcManager.AreaTriggerStore)
            {
                if (areaTrigger.ContinentId != gameContext.CurrentMap.Id)
                    continue;

                var areaTriggerPosition = new Vector3(areaTrigger.X, areaTrigger.Y, areaTrigger.Z);

                if ((gameContext.CameraManager.Position - areaTriggerPosition.ToOpenGlPosition()).LengthSquared() > AreaTriggerVisibilityDistanceSquare)
                    continue;
                
                t.Position = areaTriggerPosition.ToOpenGlPosition();
                float height = 0;
                
                if (areaTrigger.Shape == AreaTriggerShape.Box)
                {
                    height = areaTrigger.BoxHeight / 2;
                    t.Scale = new Vector3( areaTrigger.BoxWidth / 2, areaTrigger.BoxHeight/2, areaTrigger.BoxLength / 2);
                    t.Rotation = Quaternion.FromEuler(0, MathUtil.RadiansToDegrees(-areaTrigger.BoxYaw), 0.0f);
                    
                    gameContext.Engine.RenderManager.Render(boxMesh, transcluentMaterial, 0, t);
                    gameContext.Engine.RenderManager.Render(boxMesh, wireframe, 0, t);
                    gameContext.Engine.RenderManager.Render(boxMesh, wireframeBehind, 0, t);
                }
                else if (areaTrigger.Shape == AreaTriggerShape.Sphere)
                {
                    t.Scale = new Vector3(areaTrigger.Radius);
                    height = areaTrigger.Radius;

                    gameContext.Engine.RenderManager.Render(sphereMesh, transcluentMaterial, 0, t);
                    gameContext.Engine.RenderManager.Render(sphereMesh, wireframe, 0, t);
                    gameContext.Engine.RenderManager.Render(sphereMesh, wireframeBehind, 0, t);
                }
                
                gameContext.Engine.Ui.DrawWorldText("calibri", new Vector2(0.5f, 1f), "Areatrigger " + areaTrigger.Id, 2.5f, Matrix.TRS(t.Position + Vector3.Up * height, in Quaternion.Identity, in Vector3.One));
            }
        }
    }
}
