using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEngine.Handles;
using WDE.Common.Database;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;

namespace WDE.MapRenderer.Managers
{
    public class GameObjectManager : System.IDisposable
    {
        public class GameObjectChunkData
        {
            public List<GameObjectInstance> Objects = new();
        }
        
        private readonly IGameContext gameContext;

        private readonly IMeshManager meshManager;
        private readonly IMaterialManager materialManager;
        private readonly IRenderManager renderManager;
        private readonly IUIManager ui;
        private readonly MdxManager mdxManager;
        private readonly DbcManager dbcManager;
        private readonly CameraManager cameraManager;
        private readonly IDatabaseProvider database;

        private PerChunkHolder<List<IGameObject>> gameObjectDataPerChunk = new();
        private IMesh BoxMesh;
        private Material transcluentMaterial;

        public GameObjectManager(IGameContext gameContext, 
            IMeshManager meshManager,
            IMaterialManager materialManager,
            IRenderManager renderManager,
            IUIManager ui,
            MdxManager mdxManager,
            DbcManager dbcManager,
            CameraManager cameraManager,
            IDatabaseProvider database)
        {
            this.gameContext = gameContext;
            this.meshManager = meshManager;
            this.materialManager = materialManager;
            this.renderManager = renderManager;
            this.ui = ui;
            this.mdxManager = mdxManager;
            this.dbcManager = dbcManager;
            this.cameraManager = cameraManager;
            this.database = database;

            BoxMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            transcluentMaterial = materialManager.CreateMaterial("data/gizmo.json");
            transcluentMaterial.BlendingEnabled = true;
            transcluentMaterial.SourceBlending = Blending.SrcAlpha;
            transcluentMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            transcluentMaterial.DepthTesting = DepthCompare.Lequal;
            transcluentMaterial.ZWrite = false;
            transcluentMaterial.SetUniform("objectColor", new Vector4(0.2f, 0.8f, 0.2f, 0.2f));
        }
        
        public IEnumerator LoadEssentialData(CancellationToken cancel)
        {
            gameObjectDataPerChunk.Clear();

            var mapObjectsTask = database.GetGameObjectsByMapAsync((uint)gameContext.CurrentMap.Id);

            yield return mapObjectsTask;
            
            foreach (var gameObject in mapObjectsTask.Result)
            {
                if (cancel.IsCancellationRequested)
                    yield break;
                
                var chunk = new Vector3(gameObject.X, gameObject.Y, gameObject.Z).WoWPositionToChunk();
                if (chunk.Item1 <= 0 || chunk.Item2 <= 0 || chunk.Item1 >= 64 || chunk.Item2 >= 64)
                    continue;
                
                if (gameObjectDataPerChunk[chunk] == null)
                    gameObjectDataPerChunk[chunk] = new List<IGameObject>();
                
                gameObjectDataPerChunk[chunk]!.Add(gameObject);
            }
        }
        
        public IEnumerator UnloadChunk(GameObjectChunkData chunk)
        {
            foreach (var gameObject in chunk.Objects)
            {
                gameObject.Dispose();
            }
            
            chunk.Objects.Clear();

            yield break;
        }
        
        public IEnumerator LoadGameObjects(GameObjectChunkData chunk, int chunkX, int chunkY, CancellationToken cancel)
        {
            if (gameObjectDataPerChunk[chunkX, chunkY] == null)
                yield break;
            
            foreach (var gameobject in gameObjectDataPerChunk[chunkX, chunkY]!)
            {
                // check phasemask
                if (gameobject.PhaseMask != null && (gameobject.PhaseMask & 1) != 1)
                    continue;

                IGameObjectTemplate? gotemplate = database.GetGameObjectTemplate(gameobject.Entry);

                if (gotemplate == null)
                    continue;

                var gameObjectInstance = new GameObjectInstance(gameContext, gotemplate, null);

                yield return gameObjectInstance.Load();
                
                gameObjectInstance.Position = new Vector3(gameobject.X, gameobject.Y, gameobject.Z);

                gameObjectInstance.Rotation = new Quaternion(gameobject.Rotation0, gameobject.Rotation1, gameobject.Rotation2, gameobject.Rotation3);
            }
        }

        public void Dispose()
        {
            meshManager.DisposeMesh(BoxMesh);
        }
    }
}
