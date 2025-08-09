using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MapRenderer.Utils;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class AreaTriggerManager : IDisposable
    {
        private readonly IGameContext gameContext;
        private readonly IGameProperties gameProperties;
        private readonly IMeshManager meshManager;
        private readonly AreaTriggerStore areaTriggerStore;
        private readonly IRenderManager renderManager;
        private readonly IUIManager uiManager;
        private readonly CameraManager cameraManager;
        private IMesh boxMesh;
        private IMesh sphereMesh;
        private Material<Gizmo.material_data_t> transcluentMaterial;
        private Material<RenderManager.WireframeMaterialData_t> wireframe, wireframeBehind;
        private Transform t = new Transform();

        public static float AreaTriggerVisibilityDistanceSquare = 900 * 900;
        
        public AreaTriggerManager(IGameContext gameContext,
            IGameProperties gameProperties,
            IMeshManager meshManager,
            IMaterialManager materialManager,
            AreaTriggerStore areaTriggerStore,
            IRenderManager renderManager,
            IUIManager uiManager,
            CameraManager cameraManager)
        {
            this.gameContext = gameContext;
            this.gameProperties = gameProperties;
            this.meshManager = meshManager;
            this.areaTriggerStore = areaTriggerStore;
            this.renderManager = renderManager;
            this.uiManager = uiManager;
            this.cameraManager = cameraManager;
            boxMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            sphereMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);
            
            wireframe = materialManager.CreateMaterial<RenderManager.WireframeMaterialData_t>("data/wireframe.json");
            RenderManager.WireframeMaterialData_t wireframeMaterialData = new RenderManager.WireframeMaterialData_t()
            {
                width = 1,
                color = Vector4.One
            };
            wireframe.SetMaterialData(ref wireframeMaterialData);
            wireframe.ZWrite = false;
            wireframe.DepthTesting = DepthCompare.Lequal;
            
            wireframeBehind = materialManager.CreateMaterial<RenderManager.WireframeMaterialData_t>("data/wireframe.json");
            wireframeMaterialData.width = 0.5f;
            wireframeMaterialData.color = new Vector4(1, 1, 1, 0.1f);
            wireframeBehind.SetMaterialData(ref wireframeMaterialData);
            wireframeBehind.ZWrite = false;
            wireframeBehind.DepthTesting = DepthCompare.Greater;
            wireframeBehind.BlendingEnabled = true;
            wireframeBehind.SourceBlending = Blending.SrcAlpha;
            wireframeBehind.DestinationBlending = Blending.OneMinusSrcAlpha;

            transcluentMaterial = materialManager.CreateMaterial<Gizmo.material_data_t>("data/gizmo.json");
            transcluentMaterial.BlendingEnabled = true;
            transcluentMaterial.SourceBlending = Blending.SrcAlpha;
            transcluentMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            transcluentMaterial.DepthTesting = DepthCompare.Lequal;
            transcluentMaterial.ZWrite = false;
            Gizmo.material_data_t data = new() { objectColor = new Vector4(0.2f, 0.4f, 1f, 0.3f) };
            transcluentMaterial.SetMaterialData(ref data);
        }

        public void Dispose()
        {
            meshManager.DisposeMesh(boxMesh);
            meshManager.DisposeMesh(sphereMesh);
        }
        
        public void Render()
        {
            if (!gameProperties.ShowAreaTriggers)
                return;
            
            foreach (var areaTrigger in areaTriggerStore)
            {
                if (areaTrigger.ContinentId != gameContext.CurrentMap.Id)
                    continue;

                var areaTriggerPosition = new Vector3(areaTrigger.X, areaTrigger.Y, areaTrigger.Z);

                if ((cameraManager.Position - areaTriggerPosition).LengthSquared() > AreaTriggerVisibilityDistanceSquare)
                    continue;
                
                t.Position = areaTriggerPosition;
                float height = 0;
                
                if (areaTrigger.Shape == AreaTriggerShape.Box)
                {
                    height = areaTrigger.BoxHeight / 2;
                    t.Scale = new Vector3( areaTrigger.BoxLength / 2, areaTrigger.BoxWidth / 2, areaTrigger.BoxHeight/2);
                    t.Rotation = Utilities.FromEuler(0, MathUtil.RadiansToDegrees(areaTrigger.BoxYaw), 0.0f);
                    
                    renderManager.Render(boxMesh, transcluentMaterial, 0, t);
                    renderManager.Render(boxMesh, wireframe, 0, t);
                    renderManager.Render(boxMesh, wireframeBehind, 0, t);
                }
                else if (areaTrigger.Shape == AreaTriggerShape.Sphere)
                {
                    t.Scale = new Vector3(areaTrigger.Radius);
                    height = areaTrigger.Radius;

                    renderManager.Render(sphereMesh, transcluentMaterial, 0, t);
                    renderManager.Render(sphereMesh, wireframe, 0, t);
                    renderManager.Render(sphereMesh, wireframeBehind, 0, t);
                }
                
                uiManager.DrawWorldText("calibri", new Vector2(0.5f, 1f), "Areatrigger " + areaTrigger.Id, 2.5f, Utilities.TRS(t.Position + Vectors.Up * height, Quaternion.Identity, Vector3.One), Vector4.One);
            }
        }
    }
}
