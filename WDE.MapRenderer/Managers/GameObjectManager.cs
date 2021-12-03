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

namespace WDE.MapRenderer.Managers
{
    public class GameObjectManager
    {
        private readonly IGameContext gameContext;
        // private readonly IDatabaseProvider database;
        private IList<IGameObject> gameobjects;
        private IList<IGameObjectTemplate> gameobjectstemplates;
        private Transform t = new Transform();
        private IMesh BoxMesh;
        private Material transcluentMaterial;

        public static float GameObjectVisibilityDistanceSquare = 900 * 900;


        public GameObjectManager(IGameContext gameContext, IDatabaseProvider db)
        {
            this.gameContext = gameContext;
            gameobjects = db.GetGameObjects().ToList();
            gameobjectstemplates = db.GetGameObjectTemplates().ToList();

            BoxMesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/box.obj").MeshData);
            transcluentMaterial = gameContext.Engine.MaterialManager.CreateMaterial("data/gizmo.json");
            transcluentMaterial.BlendingEnabled = true;
            transcluentMaterial.SourceBlending = Blending.SrcAlpha;
            transcluentMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            transcluentMaterial.DepthTesting = DepthCompare.Lequal;
            transcluentMaterial.ZWrite = false;
            transcluentMaterial.SetUniform("objectColor", new Vector4(0.2f, 0.8f, 0.2f, 0.2f));

            gameContext.StartCoroutine(LoadGameObjects());
        }

        public void Render()
        {
            foreach (var gameobject in gameobjects)
            {
                // check map id
                if (gameobject.Map != gameContext.CurrentMap.Id)
                    continue;

                // check phasemask
                if ((gameobject.PhaseMask & 1) != 1)
                    continue;

                var gameObjectPosition = new Vector3(gameobject.X, gameobject.Y, gameobject.Z);

                t.Position = gameObjectPosition.ToOpenGlPosition();

                if ((gameContext.CameraManager.Position - t.Position).LengthSquared() > GameObjectVisibilityDistanceSquare)
                    continue;

                IGameObjectTemplate gotemplate = gameobjectstemplates.First(x => x.Entry == gameobject.Entry);

                System.Diagnostics.Debug.WriteLine($"Can't load {gotemplate.Name}");

                t.Scale = new Vector3(gotemplate.Size);

                string M2Path = "";

                if ( gameContext.DbcManager.GameObjectDisplayInfoStore.Contains((int)gotemplate.DisplayId) )
                { 
                    // M2Path = gameContext.DbcManager.GameObjectDisplayInfoStore.First(x => x.Id == gotemplate.DisplayId).ModelName;
                    M2Path = gameContext.DbcManager.GameObjectDisplayInfoStore[(int)gotemplate.DisplayId].ModelName;
                    gameContext.Engine.Ui.DrawWorldText("calibri", new Vector2(0.5f, 1f), M2Path, 2.5f, Matrix.TRS(t.Position + Vector3.Up * 5.0f, in Quaternion.Identity, in Vector3.One));
                }
                // 
                // if (M2Path == null)
                // {
                //     System.Diagnostics.Debug.WriteLine($"Can't load {M2Path}"); //could not load mdx
                // }
                // else
                //     
            }
        }

        public IEnumerator LoadGameObjects()
        {

            // if (gameobjects == null)
            //     return;

            foreach (var gameobject in gameobjects)
            {
                // check map id
                if (gameobject.Map != gameContext.CurrentMap.Id)
                    continue;

                // check phasemask
                if ((gameobject.PhaseMask & 1) != 1)
                    continue;

                var gameObjectPosition = new Vector3(gameobject.X, gameobject.Y, gameobject.Z);

                t.Position = gameObjectPosition.ToOpenGlPosition();
                float height = 0;

                // if ((gameContext.CameraManager.Position - t.Position).LengthSquared() > GameObjectVisibilityDistanceSquare)
                //     continue;

                IGameObjectTemplate gotemplate = gameobjectstemplates.First(x => x.Entry == gameobject.Entry);

                t.Scale = new Vector3(gotemplate.Size);
                // TODO : apply rotation +  orientation
                t.Rotation = Quaternion.FromEuler(0, MathUtil.RadiansToDegrees(-gameobject.Orientation), 0.0f);

                // t.Rotation = new Quaternion(gameobject.Rotation0, gameobject.Rotation1, gameobject.Rotation2, gameobject.Rotation3);
                
                string M2Path = "";

                if (gameContext.DbcManager.GameObjectDisplayInfoStore.Contains((int)gotemplate.DisplayId))
                {
                    M2Path = gameContext.DbcManager.GameObjectDisplayInfoStore[(int)gotemplate.DisplayId].ModelName;

                    TaskCompletionSource<MdxManager.MdxInstance?> mdx = new();
                    yield return gameContext.MdxManager.LoadM2Mesh(M2Path, mdx);
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
                            gameContext.Engine.RenderManager.RegisterStaticRenderer(instance.mesh.Handle, material, i++, t);

                        t.Scale = instance.mesh.Bounds.Size /2 ;
                        t.Position += instance.mesh.Bounds.Center;
                        // t.Scale = new Vector3(instance.mesh.Bounds.Width, instance.mesh.Bounds.Depth, instance.mesh.Bounds.Height); 
                        gameContext.Engine.RenderManager.RegisterStaticRenderer(BoxMesh.Handle, transcluentMaterial, 0, t);

                        // gameContext.Engine.Ui.DrawWorldText("calibri", new Vector2(0.5f, 1f), gotemplate.Name, 2.5f, Matrix.TRS(t.Position + Vector3.Up * height, in Quaternion.Identity, in Vector3.One));
                    }
                }
            }
        }

    }
}
