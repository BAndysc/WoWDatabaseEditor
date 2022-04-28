using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;
using WDE.Common.Database;
using WDE.MapRenderer;
using WDE.MapRenderer.Utils;

namespace WDE.MapRenderer.Managers
{
    public class CreatureManager : System.IDisposable
    {
        public class CreatureChunkData
        {
            public List<StaticRenderHandle> registeredEntities = new();
        }
        
        private Random rng = new Random();
        private readonly IGameContext gameContext;
        private readonly IMeshManager meshManager;
        private readonly IRenderManager renderManager;
        private readonly DbcManager dbcManager;
        private readonly MdxManager mdxManager;
        private readonly IUIManager uiManager;
        private readonly IDatabaseProvider database;
        private PerChunkHolder<IList<ICreature>> CreatureDataPerChunk = new();
        private Transform transform = new Transform();
        private IMesh BoxMesh;
        private Material BoxMaterial;
        // private TextureHandle Texture;
        // private readonly GameManager GameManager;
        // private readonly IDatabaseProvider database;

        public CreatureManager(IGameContext gameContext, 
            IMeshManager meshManager, 
            IMaterialManager materialManager,
            IRenderManager renderManager,
            DbcManager dbcManager,
            MdxManager mdxManager,
            IUIManager uiManager,
            IDatabaseProvider database)
        {
            this.gameContext = gameContext;
            this.meshManager = meshManager;
            this.renderManager = renderManager;
            this.dbcManager = dbcManager;
            this.mdxManager = mdxManager;
            this.uiManager = uiManager;
            this.database = database;
            //Mesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);
            BoxMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            BoxMaterial = materialManager.CreateMaterial("data/gizmo.json");
            BoxMaterial.BlendingEnabled = true;
            BoxMaterial.SourceBlending = Blending.SrcAlpha;
            BoxMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            BoxMaterial.DepthTesting = DepthCompare.Lequal;
            BoxMaterial.ZWrite = false;
            BoxMaterial.SetUniform("objectColor", new Vector4(0.415f, 0.4f, 0.75f, 0.4f));
        }

        public IEnumerator LoadEssentialData(CancellationToken cancel)
        {
            CreatureDataPerChunk.Clear();

            var mapCreaturesTask = database.GetCreaturesByMapAsync((uint)gameContext.CurrentMap.Id);

            yield return mapCreaturesTask;
            
            foreach (var creature in mapCreaturesTask.Result)
            {
                if (cancel.IsCancellationRequested)
                    yield break;
                
                var chunk = new Vector3(creature.X, creature.Y, creature.Z).WoWPositionToChunk();
                if (chunk.Item1 <= 0 || chunk.Item2 <= 0 || chunk.Item1 >= 64 || chunk.Item2 >= 64)
                    continue;
                
                if (CreatureDataPerChunk[chunk.Item1, chunk.Item2] == null)
                    CreatureDataPerChunk[chunk.Item1, chunk.Item2] = new List<ICreature>();
                
                CreatureDataPerChunk[chunk.Item1, chunk.Item2]!.Add(creature);
            }
        }

        // public bool OverrideLighting { get; set; }

        public void Dispose()
        {
            meshManager.DisposeMesh(BoxMesh);
        }

        public IEnumerator LoadCreatures(CreatureChunkData chunk, int chunkX, int chunkY, CancellationToken cancel)
        {
            if (CreatureDataPerChunk[chunkX, chunkY] == null)
                yield break;
            
            foreach (var creature in CreatureDataPerChunk[chunkX, chunkY]!)
            {
                if (cancel.IsCancellationRequested)
                    yield break;
                
                // check phasemask
                if (creature.PhaseMask != null && (creature.PhaseMask & 1) != 1)
                    continue;

                ICreatureTemplate? creatureTemplate = database.GetCreatureTemplate(creature.Entry);;

                if (creatureTemplate == null)
                    continue;
                
                if (dbcManager.CreatureDisplayInfoStore.Contains(creatureTemplate.GetModel(0)))
                {
                    var randomModel = creatureTemplate.GetRandomModel();
                    
                    TaskCompletionSource<MdxManager.MdxInstance?> mdx = new();
                    yield return mdxManager.LoadCreatureModel(randomModel, mdx);
                    if (mdx.Task.Result == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Can't load {randomModel}"); //could not load mdx
                    }
                    else
                    {
                        CreatureDisplayInfo creatureDisplayInfo = dbcManager.CreatureDisplayInfoStore[randomModel];
                        
                        int i = 0;
                        var instance = mdx.Task.Result;
                        transform.Position = new Vector3(creature.X, creature.Y, creature.Z);
                        transform.Rotation = Quaternion.FromEuler(0, MathUtil.RadiansToDegrees(creature.O), 0.0f);
                        transform.Scale = new Vector3(creatureDisplayInfo.CreatureModelScale * creatureTemplate.Scale);
                        foreach (var material in instance.materials)
                            chunk.registeredEntities.Add(renderManager.RegisterStaticRenderer(instance.mesh.Handle, material, i++, transform));

                        //uiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 1f),
                        //    creaturetemplate.Name + "\n" + creaturetemplate.Entry + "\n" + randomModel, 1f,
                        //    Matrix.TRS(transform.Position, in Quaternion.Identity, in Vector3.One));
                    }
                }
            }
        }

        public IEnumerator UnloadChunk(CreatureChunkData chunk)
        {
            foreach (var creature in chunk.registeredEntities)
            {
                renderManager.UnregisterStaticRenderer(creature);
            }

            yield break;
        }
    }
}