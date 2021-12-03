using System;
using System.Collections.Generic;
using System.Collections;
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

namespace WDE.MapRenderer.Managers
{
    public class CreatureManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private IList<ICreature> CreatureData;
        private IList<ICreatureTemplate> CreatureTemplateData;
        private Transform transform = new Transform();
        private IMesh Mesh;
        private Material Material;
        // private TextureHandle Texture;
        // private readonly GameManager GameManager;
        // private readonly IDatabaseProvider database;

        public CreatureManager(IGameContext gameContext, IDatabaseProvider database)
        {
            this.gameContext = gameContext;
            //Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);
            Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            Material = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            Material.BlendingEnabled = true;
            Material.SourceBlending = Blending.SrcAlpha;
            Material.DestinationBlending = Blending.OneMinusSrcAlpha;
            Material.DepthTesting = DepthCompare.Lequal;
            Material.ZWrite = false;
            Material.SetUniform("objectColor", new Vector4(0.415f, 0.4f, 0.75f, 0.4f));            // this.database = database;
            CreatureData = database.GetCreatures().ToList();
            CreatureTemplateData = database.GetCreatureTemplates().ToList();

            gameContext.StartCoroutine(LoadCeatures());

        }

        // public bool OverrideLighting { get; set; }

        public void Dispose()
        {
            // gameContext.Engine.MeshManager.DisposeMesh(Mesh);
            // gameContext.Engine.TextureManager.DisposeTexture(Texture);
        }

        public IEnumerator LoadCeatures()
        {
            System.Diagnostics.Debug.WriteLine($"{CreatureData.Count} creature spawns");

            foreach (var creature in CreatureData)
            {
                // check map id
                if (creature.Map != gameContext.CurrentMap.Id)
                    continue;

                // check phasemask
                if ((creature.PhaseMask & 1) != 1)
                    continue;

                var creaturePosition = new Vector3(creature.X, creature.Y, creature.Z);

                transform.Position = creaturePosition.ToOpenGlPosition();
                transform.Rotation = Quaternion.FromEuler(0, MathUtil.RadiansToDegrees(-creature.O), 0.0f);
                float height = 0;

                ICreatureTemplate creaturetemplate = CreatureTemplateData.First(x => x.Entry == creature.Entry);

                if (gameContext.DbcManager.CreatureDisplayInfoStore.Contains(creaturetemplate.ModelId1))
                {
                    // System.Diagnostics.Debug.WriteLine($"Cr Y :  {creature.Y}");

                    // randomly select one of the display ids of the creature
                    var displayslist = new List<uint>{ creaturetemplate.ModelId1};

                    if (creaturetemplate.ModelId2 > 0)
                        displayslist.Add(creaturetemplate.ModelId2);

                    if (creaturetemplate.ModelId3 > 0)
                        displayslist.Add(creaturetemplate.ModelId3);

                    if (creaturetemplate.ModelId4 > 0)
                        displayslist.Add(creaturetemplate.ModelId4);

                    var randomid = new Random().Next(displayslist.Count);

                    CreatureDisplayInfo crdisplayinfo = gameContext.DbcManager.CreatureDisplayInfoStore[displayslist[randomid]];

                    string M2Path = gameContext.DbcManager.CreatureModelDataStore[crdisplayinfo.ModelId].ModelName;

                    transform.Scale = new Vector3(crdisplayinfo.CreatureModelScale * creaturetemplate.Scale);

                    TaskCompletionSource<MdxManager.MdxInstance?> mdx = new();
                    yield return gameContext.MdxManager.LoadM2Mesh(M2Path, mdx, crdisplayinfo.Id);
                    if (mdx.Task.Result == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Can't load {M2Path}"); //could not load mdx
                    }
                    else
                    {
                        int i = 0;
                        var instance = mdx.Task.Result;
                        height = instance.mesh.Bounds.Height / 2;
                        // position, rotation
                        foreach (var material in instance.materials)
                            gameContext.Engine.RenderManager.RegisterStaticRenderer(instance.mesh.Handle, material, i++, transform);

                        transform.Scale = instance.mesh.Bounds.Size / 2 ;
                        transform.Position  += instance.mesh.Bounds.Center;
                        gameContext.Engine.RenderManager.RegisterStaticRenderer(Mesh.Handle, Material, 0, transform);

                        // gameContext.Engine.Ui.DrawWorldText("calibri", new Vector2(0.5f, 1f), creaturetemplate.Name, 2.5f, Matrix.TRS(t.Position + Vector3.Up * height, in Quaternion.Identity, in Vector3.One));
                    }
                }
            }
        }

    }
}