using System.Collections;
using TheEngine.Coroutines;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MpqReader;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class MdxManager : System.IDisposable
    {
        public class MdxInstance
        {
            public IMesh mesh;
            public Material[] materials;

            public void Dispose(IGameContext context)
            {
                context.Engine.MeshManager.DisposeMesh(mesh);
            }
        }

        private Dictionary<string, MdxInstance?> meshes = new();

        //private Dictionary<string, Task<MdxInstance?>> meshesCurrentlyLoaded = new();
        private readonly IGameContext gameContext;

        public MdxManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
        }

        public IEnumerator LoadM2Mesh(string path, TaskCompletionSource<MdxInstance?> result)
        {
            if (meshes.ContainsKey(path))
            {
                result.SetResult(meshes[path]);
                yield break;
            }

            //if (meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            //    return await loadInProgress;

            //var completion = new TaskCompletionSource<MdxInstance?>();
            //meshesCurrentlyLoaded[path] = completion.Task;

            var file =
                gameContext.ReadFile(path.Replace("mdx", "M2", StringComparison.InvariantCultureIgnoreCase));

            yield return new WaitForTask(file);

            var skinFile =
                gameContext.ReadFile(path.Replace(".mdx", "00.skin", StringComparison.InvariantCultureIgnoreCase));
            
            yield return new WaitForTask(skinFile);
            
            if (file.Result == null || skinFile.Result == null)
            {
                Console.WriteLine("Cannot find model " + path);
                meshes[path] = null;
                //meshesCurrentlyLoaded.Remove(path);
                result.SetResult(null);
                yield break;
            }

            M2 m2 = null!;
            M2Skin skin = null!;
            Vector3[] vertices = null!;
            Vector3[] normals = null!;
            Vector2[] uv1 = null!;
            Vector2[] uv2 = null!;

            yield return new WaitForTask(Task.Run(() =>
            {
                m2 = M2.Read(new MemoryBinaryReader(file.Result));
                file.Result.Dispose();
                skin = M2Skin.Read(new MemoryBinaryReader(skinFile.Result));
                skinFile.Result.Dispose();
                var count = m2.vertices.Length;
                vertices = new Vector3[count];
                normals = new Vector3[count];
                uv1 = new Vector2[count];
                uv2 = new Vector2[count];

                for (int i = 0; i < count; ++i)
                {
                    var vert = m2.vertices[i];
                    vertices[i] = vert.pos;
                    normals[i] = vert.normal;
                    uv1[i] = vert.tex_coords[0];
                    uv2[i] = vert.tex_coords[1];
                }
            }));

            var md = new MeshData(vertices, normals, uv1, new int[] { });
            
            var mesh = gameContext.Engine.MeshManager.CreateMesh(md);
            mesh.SetSubmeshCount(skin.Batches.Length);

            Material[] materials = new Material[skin.Batches.Length];
            int j = 0;
            foreach (var batch in skin.Batches)
            {
                if (batch.skinSectionIndex == ushort.MaxValue ||
                    batch.materialIndex >= m2.materials.Length ||
                    batch.skinSectionIndex >= skin.SkinSections.Length)
                    continue;

                var section = skin.SkinSections[batch.skinSectionIndex];

                using var indices = new PooledArray<int>(section.indexCount);
                for (int i = 0; i < Math.Min(section.indexCount, skin.Indices.Length - section.indexStart); ++i)
                {
                    indices[i] = skin.Indices[section.indexStart + i];
                }

                mesh.SetIndices(indices.AsSpan(), j++);

                TextureHandle? th = null;
                if (batch.textureCount != ushort.MaxValue)
                {
                    for (int i = 0; i < batch.textureCount; ++i)
                    {
                        if (batch.textureComboIndex + i >= m2.textures.Length)
                            continue;
                        
                        var textureDef = m2.textures[batch.textureComboIndex + i];
                        var texFile = textureDef.filename.AsString();
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return gameContext.TextureManager.GetTexture(texFile, tcs);
                        th = tcs.Task.Result;
                        break;
                    }
                }

                var materialDef = m2.materials[batch.materialIndex];
                var m2ShaderHandle = gameContext.Engine.ShaderManager.LoadShader("data/m2.json");
                var material = gameContext.Engine.MaterialManager.CreateMaterial(m2ShaderHandle);

                if (th != null)
                    material.SetTexture("texture1", th.Value);

                if (materialDef.flags.HasFlag(M2MaterialFlags.TwoSided))
                    material.Culling = CullingMode.Off;

                material.SetUniform("highlight", 0);
                material.SetUniform("notSupported", 0);
                if (materialDef.blending_mode == M2Blend.M2BlendOpaque)
                {
                    material.BlendingEnabled = false;
                    material.SetUniform("alphaTest", -1);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAlphaKey)
                {
                    material.BlendingEnabled = false;
                    material.SetUniform("alphaTest", 224.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAlpha)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.SrcAlpha;
                    material.DestinationBlending = Blending.OneMinusSrcAlpha;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else
                    material.SetUniform("notSupported", 1);

                materials[j - 1] = material;
            }

            mesh.Rebuild();

            var mdx = new MdxInstance
            {
                mesh = mesh,
                materials = materials.AsSpan(0, j).ToArray()
            };
            meshes[path] = mdx;
            result.SetResult(mdx);
        }

        public void Dispose()
        {
            foreach (var mesh in meshes.Values)
                mesh?.Dispose(gameContext);
        }
    }
}