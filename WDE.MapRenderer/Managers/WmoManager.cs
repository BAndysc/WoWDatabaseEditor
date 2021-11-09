using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class WmoManager : System.IDisposable
    {
        private readonly IGameContext gameContext;

        public class WmoInstance
        {
            public List<(IMesh, Material[])> meshes = new();

            public IEnumerable<(IMesh, Material[])> Meshes => meshes;

            public void Dispose(IGameContext context)
            {
                foreach (var mesh in meshes)
                {
                    context.Engine.MeshManager.DisposeMesh(mesh.Item1);
                }

                meshes.Clear();
            }
        }

        private Dictionary<string, WmoInstance?> meshes = new();
        private Dictionary<string, Task> meshesCurrentlyLoaded = new();

        public WmoManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
        }

        public IEnumerator LoadWorldMapObject(string path, TaskCompletionSource<WmoInstance?> result)
        {
            if (meshes.ContainsKey(path))
            {
                result.SetResult(meshes[path]);
                yield break;
            }

            if (meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            {
                yield return loadInProgress;
                result.SetResult(meshes[path]);
                yield break;
            }

            var completion = new TaskCompletionSource<WmoInstance?>();
            meshesCurrentlyLoaded[path] = completion.Task;

            var bytes = gameContext.ReadFile(path);
            yield return bytes;
            if (bytes.Result == null)
            {
                meshes[path] = null;
                meshesCurrentlyLoaded.Remove(path);
                completion.SetResult(null);
                result.SetResult(null);
                yield break;
            }

            var wmo = WMO.Read(new MemoryBinaryReader(bytes.Result));
            bytes.Result.Dispose();

            List<WorldMapObjectGroup> groups = new();
            for (int i = 0; i < wmo.Header.nGroups; ++i)
            {
                var groupFile = path.Replace(".wmo", "_" + i.ToString().PadLeft(3, '0') + ".wmo",
                    StringComparison.OrdinalIgnoreCase);
                var bytesGroup = gameContext.ReadFile(groupFile);
                yield return bytesGroup;
                if (bytesGroup.Result == null)
                    continue;

                var group = new WorldMapObjectGroup(new MemoryBinaryReader(bytesGroup.Result), true);
                bytesGroup.Result.Dispose();
                groups.Add(group);
            }

            var wmoInstance = new WmoInstance();

            foreach (var group in groups)
            {
                var wmoMeshData = new MeshData(group.Vertices.AsArray(), group.Normals.AsArray(), group.UVs.AsArray(),
                    group.Indices.AsArray(), group.Vertices.Length, group.Indices.Length);

                var wmoMesh = gameContext.Engine.MeshManager.CreateMesh(wmoMeshData);
                wmoMesh.SetSubmeshCount(group.Batches.Length);
                int j = 0;
                Material[] materials = new Material[group.Batches.Length];
                foreach (var batch in group.Batches)
                {
                    wmoMesh.SetSubmeshIndicesRange(j++, (int)batch.startIndex, batch.count);

                    var materialDef = wmo.Materials[batch.material_id];
                    var m2ShaderHandle = gameContext.Engine.ShaderManager.LoadShader("data/m2.json");
                    var mat = gameContext.Engine.MaterialManager.CreateMaterial(m2ShaderHandle);

                    mat.SetUniform("highlight", 0);
                    mat.SetUniform("notSupported", 0);
                    float alphaTest = 0.003921568f; // 1/255
                    if (materialDef.blendMode == GxBlendMode.GxBlend_Opaque)
                    {
                        alphaTest = -1.0f;
                        mat.BlendingEnabled = false;
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_AlphaKey)
                    {
                        mat.BlendingEnabled = false;
                        alphaTest = 0.878431372f; // 224/255
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_Alpha)
                    {
                        mat.BlendingEnabled = true;
                        mat.SourceBlending = Blending.SrcAlpha;
                        mat.DestinationBlending = Blending.OneMinusSrcAlpha;
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_Add)
                    {
                        mat.BlendingEnabled = true;
                        mat.SourceBlending = Blending.SrcAlpha;
                        mat.DestinationBlending = Blending.One;
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_Mod)
                    {
                        mat.BlendingEnabled = true;
                        mat.SourceBlending = Blending.DstColor;
                        mat.DestinationBlending = Blending.Zero;
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_Mod2x)
                    {
                        mat.BlendingEnabled = true;
                        mat.SourceBlending = Blending.DstColor;
                        mat.DestinationBlending = Blending.SrcColor;
                    }
                    else if (materialDef.blendMode == GxBlendMode.GxBlend_ModAdd)
                    {
                        mat.BlendingEnabled = true;
                        mat.SourceBlending = Blending.DstColor;
                        mat.DestinationBlending = Blending.One;
                    }
                    else
                    {
                        alphaTest = -1.0f;
                        mat.BlendingEnabled = false;
                        mat.SetUniform("notSupported", 1);
                    }

                    mat.SetUniform("alphaTest", alphaTest);

                    if (materialDef.flags.HasFlag(WorldMapObjectMaterial.Flags.unculled))
                        mat.Culling = CullingMode.Off;

                    if (materialDef.texture1Name != null)
                    {
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return gameContext.TextureManager.GetTexture(materialDef.texture1Name, tcs);
                        mat.SetTexture("texture1", tcs.Task.Result);
                    }

                    materials[j - 1] = mat;
                }

                wmoMesh.Rebuild();
                wmoInstance.meshes.Add((wmoMesh, materials));
                group.Dispose();
            }

            meshes.Add(path, wmoInstance);
            completion.SetResult(wmoInstance);
            meshesCurrentlyLoaded.Remove(path);
            result.SetResult(wmoInstance);
        }

        public void Dispose()
        {
            foreach (var wmo in meshes.Values)
                wmo?.Dispose(gameContext);
        }
    }
}