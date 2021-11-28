using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;
using WDE.Common.Database;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;

namespace WDE.MapRenderer.Managers
{
    public class CreatureManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private IList<ICreature> CreatureData;
        private IMesh Mesh;
        private Material Material;
        // private TextureHandle Texture;
        // private readonly GameManager GameManager;
        private readonly IDatabaseProvider database;

        public CreatureManager(IGameContext gameContext, IDatabaseProvider database)
        {
            this.gameContext = gameContext;
            Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere_wp.obj").MeshData);
            Material = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            // GameManager = new GameManager();
            this.database = database;
            CreatureData = database.GetCreatures().ToList();
            // CreatureTemplateData = database.getcre
            // gameContext.
            // CreatureData = gameContext.database.GetCreatures().ToList();


            // Texture = gameContext.Engine.TextureManager.LoadTexture("textures/noise_512.png");

            // skyMaterial.SetTexture("cloudsTex", noiseTexture);
        }

        // public bool OverrideLighting { get; set; }

        public void Dispose()
        {
            gameContext.Engine.MeshManager.DisposeMesh(Mesh);
            // gameContext.Engine.TextureManager.DisposeTexture(Texture);
        }

        public void Render()
        {

            if (CreatureData == null)
                return;

            // System.Diagnostics.Debug.WriteLine("Creatures entries count :  " + CreatureData.Count());
            // 
            // System.Diagnostics.Debug.WriteLine("map id :  " + gameContext.CurrentMap.Id);

            foreach (var creature in CreatureData)
            {
                // if (creature.Guid == 91596)
                //     System.Diagnostics.Debug.WriteLine("test creature map :  " + creature.Map + " " + creature.Entry + " " + creature.X);
                
                // check map id
                if (creature.Map != gameContext.CurrentMap.Id)
                    continue;

                // check phasemask
                if ( (creature.PhaseMask & 1) != 1)
                    continue;
                
                var creaturepos = new Vector3(creature.X, creature.Y, creature.Z);

                if ((gameContext.CameraManager.Position - creaturepos.ToOpenGlPosition()).LengthSquared() > 1000 * 1000)
                    continue;

                var t = new Transform
                {
                    Position = creaturepos.ToOpenGlPosition(),
                    Scale = new Vector3(1.0f),
                    Rotation = Quaternion.FromEuler(creature.O, 0.0f, 0.0f)
                };

                Material.SetUniform("objectColor", new Vector4(0.415f, 0.4f, 0.75f, 0.7f)); // values from 0 to 1
                Material.BlendingEnabled = true;
                Material.SourceBlending = Blending.SrcAlpha;
                Material.DestinationBlending = Blending.OneMinusSrcAlpha;
                // Material.DepthTesting = DepthCompare.Always;

                //////////////////////////////////////

                // var displayinfo = gameContext.DbcManager.CreatureDisplayInfoStore.First(x => x.Id == database.GetCreatureTemplate(creature.Entry).modelid1 );
                // 
                // string M2Path = gameContext.DbcManager.CreatureModelDataStore.First(x => x.Id == displayinfo.ModelId).ModelName;
                // 
                // t.Scale = Vector3.One * displayinfo.CreatureModelScale; // maybe multiply dbc scale * creature_template scale

                gameContext.Engine.RenderManager.Render(Mesh, Material, 0, t);

                // draw debug text
                // BoundingBox box = new BoundingBox(t.Position - new Vector3(1.0f), creaturepos.ToOpenGlPosition() + new Vector3(1.0f));
                // if (box.Contains(gameContext.CameraManager.Position) == ContainmentType.Contains)
                // {
                //     using var ui = gameContext.Engine.Ui.BeginImmediateDrawAbs(0, 0);
                //     ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.7f), 5);
                //     ui.Text("calibri", $"Inside areatrigger id: " + creature.Guid, 16, Vector4.One);
                //     ui.EndBox();
                // }

            }
           


        }

    }
}
