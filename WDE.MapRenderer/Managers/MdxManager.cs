using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum ModelPixelShader
    {
        Combiners_Opaque = 0,
        Combiners_Decal,
        Combiners_Add,
        Combiners_Mod2x,
        Combiners_Fade,
        Combiners_Mod,
        Combiners_Opaque_Opaque,
        Combiners_Opaque_Add,
        Combiners_Opaque_Mod2x,
        Combiners_Opaque_Mod2xNA,
        Combiners_Opaque_AddNA,
        Combiners_Opaque_Mod,
        Combiners_Mod_Opaque,
        Combiners_Mod_Add,
        Combiners_Mod_Mod2x,
        Combiners_Mod_Mod2xNA,
        Combiners_Mod_AddNA,
        Combiners_Mod_Mod,
        Combiners_Add_Mod,
        Combiners_Mod2x_Mod2x,
        Combiners_Opaque_Mod2xNA_Alpha,
        Combiners_Opaque_AddAlpha,
        Combiners_Opaque_AddAlpha_Alpha,
    };

    
    public class MdxManager : System.IDisposable
    {
        public class MdxInstance
        {
            public IMesh mesh;
            public Material[] materials;

            public void Dispose(IMeshManager meshManager)
            {
                meshManager.DisposeMesh(mesh);
            }
        }

        private Dictionary<string, MdxInstance?> meshes = new();
        private Dictionary<string, Task<MdxInstance?>> meshesCurrentlyLoaded = new();
        private readonly IGameFiles gameFiles;
        private readonly IMeshManager meshManager;
        private readonly IMaterialManager materialManager;
        private readonly WoWTextureManager textureManager;

        public MdxManager(IGameFiles gameFiles, IMeshManager meshManager, IMaterialManager materialManager, WoWTextureManager textureManager)
        {
            this.gameFiles = gameFiles;
            this.meshManager = meshManager;
            this.materialManager = materialManager;
            this.textureManager = textureManager;
        }

        public IEnumerator LoadM2Mesh(string path, TaskCompletionSource<MdxInstance?> result)
        {
            if (meshes.ContainsKey(path))
            {
                result.SetResult(meshes[path]);
                yield break;
            }

            if (meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            {
                yield return new WaitForTask(loadInProgress);
                result.SetResult(meshes[path]);
                yield break;
            }

            var completion = new TaskCompletionSource<MdxInstance?>();
            meshesCurrentlyLoaded[path] = completion.Task;

            var m2FilePath = path.Replace("mdx", "M2", StringComparison.InvariantCultureIgnoreCase);
            var skinFilePath = m2FilePath.Replace(".m2", "00.skin", StringComparison.InvariantCultureIgnoreCase);
            var file = gameFiles.ReadFile(m2FilePath);

            yield return new WaitForTask(file);

            var skinFile = gameFiles.ReadFile(skinFilePath);
            
            yield return new WaitForTask(skinFile);
            
            if (file.Result == null || skinFile.Result == null)
            {
                Console.WriteLine("Cannot find model " + path);
                meshes[path] = null;
                completion.SetResult(null);
                meshesCurrentlyLoaded.Remove(path);
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

            var md = new MeshData(vertices, normals, uv1, new int[] { }, null, null, uv2);
            
            var mesh = meshManager.CreateMesh(md);
            mesh.SetSubmeshCount(skin.Batches.Length);

            Material[] materials = new Material[skin.Batches.Length];
            int j = 0;
            foreach (var batch in skin.Batches)
            {
                if (batch.skinSectionIndex == ushort.MaxValue ||
                    batch.materialIndex >= m2.materials.Length ||
                    batch.skinSectionIndex >= skin.SkinSections.Length)
                {
                    Console.WriteLine("Sth wrong with batch " + j + " in model " + path);
                    continue;
                }

                var section = skin.SkinSections[batch.skinSectionIndex];
                

                using var indices = new PooledArray<int>(section.indexCount);
                for (int i = 0; i < Math.Min(section.indexCount, skin.Indices.Length - section.indexStart); ++i)
                {
                    indices[i] = skin.Indices[section.indexStart + i];
                }

                mesh.SetIndices(indices.AsSpan(), j++);

                TextureHandle? th = null;
                TextureHandle? th2 = null;
                for (int i = 0; i < (batch.textureCount >= 5 ? 1 : batch.textureCount); ++i)
                {
                    if (batch.textureComboIndex + i >= m2.textureCombos.Length)
                    {
                        if (th.HasValue)
                            th2 = textureManager.EmptyTexture;
                        else
                            th = textureManager.EmptyTexture;
                        Console.WriteLine("File " + path + " batch " + j + " tex " + i + " out of range");
                        continue;
                    }
                    var texId = m2.textureCombos[batch.textureComboIndex + i];
                    if (texId == -1)
                    {
                        if (th.HasValue)
                            th2 = textureManager.EmptyTexture;
                        else
                            th = textureManager.EmptyTexture;
                    }
                    else
                    {
                        var textureDef = m2.textures[texId];
                        var texFile = textureDef.filename.AsString();
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return textureManager.GetTexture(texFile, tcs);
                        var resTex = tcs.Task.Result;
                        if (th.HasValue)
                            th2 = resTex;
                        else
                            th = resTex;
                    }
                }

                var materialDef = m2.materials[batch.materialIndex];
                var material = materialManager.CreateMaterial("data/m2.json");

                material.SetTexture("texture1", th ?? textureManager.EmptyTexture);
                material.SetTexture("texture2", th2 ?? textureManager.EmptyTexture);

                var trans = 1.0f;
                if (batch.colorIndex != -1 && m2.colors.Length < batch.colorIndex)
                {
                    if (m2.colors[batch.colorIndex].alpha.values.Length == 0 || m2.colors[batch.colorIndex].alpha.values[0].Length == 0)
                        trans = 1;
                    else
                        trans = m2.colors[batch.colorIndex].alpha.values[0][0].Value;
                }

                if (batch.transparencyIndex != -1 && m2.texture_weights.Length < batch.transparencyIndex)
                {
                    if (m2.texture_weights[batch.transparencyIndex].weight.values.Length > 0 && m2.texture_weights[batch.transparencyIndex].weight.values[0].Length > 0)
                        trans *= m2.texture_weights[batch.transparencyIndex].weight.values[0][0].Value;
                }
                
                Vector4 mesh_color = new Vector4(1.0f, 1.0f, 1.0f, trans);
    
                material.SetUniform("mesh_color", mesh_color);
                material.SetUniformInt("pixel_shader", (int)(M2GetPixelShaderID(batch.textureCount, batch.shader_id) ?? ModelPixelShader.Combiners_Opaque));
                //Console.WriteLine(path + " INDEX: " + j + " Pixel shader: " + M2GetPixelShaderID(batch.textureCount, batch.shader_id) + " tex count: " + batch.textureCount + " shader id: " + batch.shader_id + " blend: " + materialDef.blending_mode + " priority " + batch.priorityPlane + " start ");
                
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
                    //material.SourceBlending = Blending.One;
                    //material.DestinationBlending = Blending.Zero;
                    material.SetUniform("alphaTest", 224.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAlpha)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.SrcAlpha;
                    material.DestinationBlending = Blending.OneMinusSrcAlpha;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendNoAlphaAdd)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.One;
                    material.DestinationBlending = Blending.One;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAdd)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.SrcAlpha;
                    material.DestinationBlending = Blending.One;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendMod)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.DstColor;
                    material.DestinationBlending = Blending.Zero;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendMod2X)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.DstColor;
                    material.DestinationBlending = Blending.SrcColor;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else
                    material.SetUniform("notSupported", 1);

                material.ZWrite = !material.BlendingEnabled;
                //material.DepthTesting = materialDef.flags.HasFlag(M2MaterialFlags.DepthTest); // produces wrong results :thonk:

                if (materialDef.flags.HasFlag(M2MaterialFlags.TwoSided))
                    material.Culling = CullingMode.Off;
                material.SetUniformInt("unlit", materialDef.flags.HasFlag(M2MaterialFlags.Unlit) ? 1 : 0);

                materials[j - 1] = material;
            }

            mesh.Rebuild();

            var mdx = new MdxInstance
            {
                mesh = mesh,
                materials = materials.AsSpan(0, j).ToArray()
            };
            meshes.Add(path, mdx);
            completion.SetResult(null);
            meshesCurrentlyLoaded.Remove(path);
            result.SetResult(mdx);
        }

        // https://wowdev.wiki/M2/.skin/WotLK_shader_selection
        ModelPixelShader? GetPixelShader(ushort texture_count, ushort shader_id)
        {
            ushort texture1_fragment_mode = (ushort)((shader_id >> 4) & 7);
              ushort texture2_fragment_mode = (ushort)(shader_id & 7);
          // uint16_t texture1_env_map = (shader_id >> 4) & 8;
          // uint16_t texture2_env_map = shader_id & 8;

          ModelPixelShader? pixel_shader = null;

          if (texture_count == 1)
          {
            switch (texture1_fragment_mode)
            {
            case 0:
              pixel_shader = ModelPixelShader.Combiners_Opaque;
              break;
            case 2:
              pixel_shader = ModelPixelShader.Combiners_Decal;
              break;
            case 3:
              pixel_shader = ModelPixelShader.Combiners_Add;
              break;
            case 4:
              pixel_shader = ModelPixelShader.Combiners_Mod2x;
              break;
            case 5:
              pixel_shader = ModelPixelShader.Combiners_Fade;
              break;
            default:
              pixel_shader = ModelPixelShader.Combiners_Mod;
              break;
            }
          }
          else
          {
            if (texture1_fragment_mode == 0)
            {
              switch (texture2_fragment_mode)
              {
              case 0:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Opaque;
                break;
              case 3:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Add;
                break;
              case 4:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2x;
                break;
              case 6:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2xNA;
                break;
              case 7:
                pixel_shader = ModelPixelShader.Combiners_Opaque_AddNA;
                break;
              default:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod;
                break;
              }
            }
            else if (texture1_fragment_mode == 1)
            {
              switch (texture2_fragment_mode)
              {
              case 0:
                pixel_shader = ModelPixelShader.Combiners_Mod_Opaque;
                break;
              case 3:
                pixel_shader = ModelPixelShader.Combiners_Mod_Add;
                break;
              case 4:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod2x;
                break;
              case 6:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod2xNA;
                break;
              case 7:
                pixel_shader = ModelPixelShader.Combiners_Mod_AddNA;
                break;
              default:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod;
                break;
              }
            }
            else if (texture1_fragment_mode == 3)
            {
              if (texture2_fragment_mode == 1)
              {
                pixel_shader = ModelPixelShader.Combiners_Add_Mod;
              }
            }
            else if (texture1_fragment_mode == 4 && texture2_fragment_mode == 4)
            {
              pixel_shader = ModelPixelShader.Combiners_Mod2x_Mod2x;
            }
            else if (texture2_fragment_mode == 1)
            {
              pixel_shader = ModelPixelShader.Combiners_Mod_Mod2x;
            }
          }
         

          return pixel_shader;
        }
        
        ModelPixelShader? M2GetPixelShaderID(ushort texture_count, ushort shader_id)
        {
            ModelPixelShader? pixel_shader = null;

            if ((shader_id & 0x8000) == 0)
            {
                pixel_shader = GetPixelShader(texture_count, shader_id);

                if (!pixel_shader.HasValue)
                {
                    pixel_shader = GetPixelShader(texture_count, 0x11);
                }
            }
            else
            {
                switch (shader_id & 0x7FFF)
                {
                    case 1:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2xNA_Alpha;
                        break;
                    case 2:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_AddAlpha;
                        break;
                    case 3:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_AddAlpha_Alpha;
                        break;
                }  
            }

            return pixel_shader;
        }
        
        public void Dispose()
        {
            foreach (var mesh in meshes.Values)
                mesh?.Dispose(meshManager);
        }
    }
}