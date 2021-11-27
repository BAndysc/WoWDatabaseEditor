using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class AreaTriggerManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private IMesh Mesh;
        private Material Material;
        private TextureHandle Texture;
        private Material translucent;
        private Material opaque;
        private IMesh Mesh2;
        private Material translucent2;
        private Material opaque2;

        public AreaTriggerManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box_frame.obj").MeshData);
            Material = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            // Texture = gameContext.Engine.TextureManager.LoadTexture("textures/noise_512.png");

            translucent = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            opaque = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            Mesh2 = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            translucent2 = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            opaque2 = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            // skyMaterial.SetTexture("cloudsTex", noiseTexture);
        }

        // public bool OverrideLighting { get; set; }

        public void Dispose()
        {
            gameContext.Engine.MeshManager.DisposeMesh(Mesh);
            gameContext.Engine.TextureManager.DisposeTexture(Texture);
        }

        public void Update(float delta)
        {
            ////
        }

        public void Render()
        {
            // Time time = gameContext.TimeManager.Time;

            foreach (var areatrigger in gameContext.DbcManager.AreaTriggerStore)
            {
                if (areatrigger.ContinentID != gameContext.CurrentMap.Id)
                    continue;
                // System.Diagnostics.Debug.WriteLine(areatrigger.Id + " X:" + areatrigger.X + ", Y:" + areatrigger.Y);

                var areatriggerpos = new Vector3(areatrigger.X, areatrigger.Y, areatrigger.Z);

                if ((gameContext.CameraManager.Position - areatriggerpos.ToOpenGlPosition()).LengthSquared() > 800 * 1000)
                    continue;

                // if (areatrigger.Id == 2230)
                //     System.Diagnostics.Debug.WriteLine(" X:" + pos.X + ", Y:" + pos.Y + ", Z:" + pos.Z);

                var distance = areatrigger.Radius;

                var t = new Transform();
                t.Position = areatriggerpos.ToOpenGlPosition();

                if (areatrigger.Radius == 0) // if radius = 0, is box
                {
                    t.Scale = new Vector3( areatrigger.Box_Width / 2, areatrigger.Box_Height/2, areatrigger.Box_Length / 2);
                    t.Rotation = Quaternion.FromEuler(areatrigger.Box_Yaw, 0.0f, 0.0f);

                    Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);

                    
                    translucent.BlendingEnabled = true;
                    translucent.SourceBlending = Blending.SrcAlpha;
                    translucent.DestinationBlending = Blending.OneMinusSrcAlpha;
                    translucent.DepthTesting = false;
                    translucent.ZWrite = false;
                    translucent.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.5f));

                    
                    opaque.DepthTesting = true;
                    opaque.ZWrite = false;
                    opaque.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 1f));

                    // Render
                    gameContext.Engine.RenderManager.Render(Mesh, translucent, 0, t);
                    // gameContext.Engine.RenderManager.Render(Mesh, opaque, 0, t);

                    

                    // Material.SetUniform("objectColor", new Vector4(0.5f, 0.5f, 0.5f, 0.4f)); // values from 0 to 1
                    // Material.BlendingEnabled = true;
                    // Material.SourceBlending = Blending.SrcAlpha;
                    // Material.DestinationBlending = Blending.OneMinusSrcAlpha;
                    // Material.DepthTesting = true;

                    // gameContext.Engine.RenderManager.Render(Mesh2, Material, 0, t);

                    
                    translucent2.BlendingEnabled = true;
                    translucent2.SourceBlending = Blending.SrcAlpha;
                    translucent2.DestinationBlending = Blending.OneMinusSrcAlpha;
                    translucent2.DepthTesting = false;
                    translucent2.ZWrite = false;
                    translucent2.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.2f));

                    
                    opaque2.BlendingEnabled = true;
                    opaque2.SourceBlending = Blending.SrcAlpha;
                    opaque2.DestinationBlending = Blending.OneMinusSrcAlpha;
                    opaque2.DepthTesting = true;
                    opaque2.ZWrite = false;
                    opaque2.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.4f));

                    gameContext.Engine.RenderManager.Render(Mesh2, translucent2, 0, t);
                    gameContext.Engine.RenderManager.Render(Mesh2, opaque2, 0, t);
                        Material.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.4f)); // values from 0 to 1
                    // Material.BlendingEnabled = true;
                    // Material.SourceBlending = Blending.SrcAlpha;
                    // Material.DestinationBlending = Blending.OneMinusSrcAlpha;
                    // Material.DepthTesting = true;
                }

                else if (areatrigger.Radius > 0) // if radius > 0, is sphere
                {
                    t.Scale = new Vector3(distance);
                    Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/skysphere.obj").MeshData);

                    var translucent = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
                    translucent.BlendingEnabled = true;
                    translucent.SourceBlending = Blending.SrcAlpha;
                    translucent.DestinationBlending = Blending.OneMinusSrcAlpha;
                    translucent.DepthTesting = false;
                    translucent.ZWrite = false;
                    translucent.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.25f));

                    var opaque = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
                    opaque.BlendingEnabled = true;
                    opaque.SourceBlending = Blending.SrcAlpha;
                    opaque.DestinationBlending = Blending.OneMinusSrcAlpha;
                    opaque.DepthTesting = true;
                    opaque.ZWrite = false;
                    opaque.SetUniform("objectColor", new Vector4(0.2f, 0.4f, 1f, 0.5f));

                    // Render
                    gameContext.Engine.RenderManager.Render(Mesh, translucent, 0, t);
                    gameContext.Engine.RenderManager.Render(Mesh, opaque, 0, t);
                }


                // draw debug text
                // BoundingBox box = new BoundingBox(t.Position - new Vector3(distance), areatriggerpos.ToOpenGlPosition() + new Vector3(distance));
                // if (box.Contains(gameContext.CameraManager.Position) == ContainmentType.Contains)
                // {
                //     using var ui = gameContext.Engine.Ui.BeginImmediateDrawAbs(0, 0);
                //     ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.7f), 5);
                //     ui.Text("calibri", $"Inside areatrigger id: " + areatrigger.Id, 16, Vector4.One);
                //     ui.EndBox();
                // }
            }

        }

    }
}
