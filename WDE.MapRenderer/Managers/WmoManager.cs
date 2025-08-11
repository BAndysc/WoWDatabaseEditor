using System.Collections;
using System.Runtime.InteropServices;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using Veldrid;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;
using Pipeline = TheEngine.Resources.Pipeline;

namespace WDE.MapRenderer.Managers
{
    public class WmoManager : System.IDisposable
    {
        private readonly IGameFiles gameFiles;
        private readonly IMeshManager meshManager;
        private readonly WoWTextureManager textureManager;
        private readonly IMaterialManager materialManager;
        private readonly WoWMeshManager woWMeshManager;
        private readonly IPipelineManager pipelineManager;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WmoMaterialData
        {
            public float alphaTest;
            public float notSupported;
            public int shader_id;
            public int unlit;
            public int brightAtNight;
            public int interior;
            public int translucent;
            public int padding;
        };

        public class WmoInstance
        {
            public List<(IMesh, Material[])> meshes = new();

            public IEnumerable<(IMesh, Material[])> Meshes => meshes;

            public void Dispose(IMeshManager meshManager)
            {
                foreach (var mesh in meshes)
                {
                    meshManager.DisposeMesh(mesh.Item1);
                }

                meshes.Clear();
            }
        }

        private Dictionary<FileId, WeakReference<WmoInstance>?> meshes = new();
        private Dictionary<FileId, Task<WmoInstance?>> meshesCurrentlyLoaded = new();

        private Dictionary<(GxBlendMode mode, bool unculled), Pipeline> pipelines = new Dictionary<(GxBlendMode mode, bool unculled), Pipeline>();

        private ShaderHandle wmoShader;

        public WmoManager(IGameFiles gameFiles,
            IMeshManager meshManager,
            WoWTextureManager textureManager,
            IMaterialManager materialManager,
            WoWMeshManager woWMeshManager,
            IShaderManager shaderManager,
            IPipelineManager pipelineManager)
        {
            this.gameFiles = gameFiles;
            this.meshManager = meshManager;
            this.textureManager = textureManager;
            this.materialManager = materialManager;
            this.woWMeshManager = woWMeshManager;
            this.pipelineManager = pipelineManager;

            wmoShader = shaderManager.LoadShader("data/wmo.json");

            Span<bool> trueFalse = stackalloc bool[2];
            trueFalse[0] = false;
            trueFalse[1] = true;
            foreach (var blendMode in Enum.GetValues<GxBlendMode>())
                foreach (bool unculled in trueFalse)
                    CreatePipeline(blendMode, unculled);
        }

        private void CreatePipeline(GxBlendMode blendMode, bool unculled)
        {
            var blending = new BlendAttachmentDescription();
            if (blendMode == GxBlendMode.GxBlend_Opaque)
            {
                //alphaTest = -1.0f;
                blending.BlendEnabled = false;
            }
            else if (blendMode == GxBlendMode.GxBlend_AlphaKey)
            {
                blending.BlendEnabled = false;
                //alphaTest = 0.878431372f; // 224/255
            }
            else if (blendMode == GxBlendMode.GxBlend_Alpha)
            {
                blending.BlendEnabled = true;
                blending.SourceColorFactor = BlendFactor.SourceAlpha;
                blending.DestinationColorFactor = BlendFactor.InverseSourceAlpha;
            }
            else if (blendMode == GxBlendMode.GxBlend_Add)
            {
                blending.BlendEnabled= true;
                blending.SourceColorFactor = BlendFactor.SourceAlpha;
                blending.DestinationColorFactor = BlendFactor.One;
            }
            else if (blendMode == GxBlendMode.GxBlend_Mod)
            {
                blending.BlendEnabled = true;
                blending.SourceColorFactor = BlendFactor.DestinationColor;
                blending.DestinationColorFactor = BlendFactor.Zero;
            }
            else if (blendMode == GxBlendMode.GxBlend_Mod2x)
            {
                blending.BlendEnabled = true;
                blending.SourceColorFactor = BlendFactor.DestinationColor;
                blending.DestinationColorFactor = BlendFactor.SourceColor;
            }
            else if (blendMode == GxBlendMode.GxBlend_ModAdd)
            {
                blending.BlendEnabled = true;
                blending.SourceColorFactor = BlendFactor.DestinationColor;
                blending.DestinationColorFactor = BlendFactor.One;
            }
            else
            {
                //alphaTest = -1.0f;
                blending.BlendEnabled = false;
                //mat.SetUniform("notSupported", 1);
            }

            pipelines[(blendMode, unculled)] = pipelineManager.CreatePipeline(wmoShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual with {DepthWriteEnabled = !blending.BlendEnabled },
                RasterizerState = new RasterizerStateDescription()
                {
                    CullMode = unculled ? FaceCullMode.None : FaceCullMode.Front,
                    FillMode = PolygonFillMode.Solid,
                    FrontFace = FrontFace.Clockwise,
                    DepthClipEnabled = true,
                    ScissorTestEnabled = false,
                },
                BlendState = new BlendStateDescription()
                {
                    AttachmentStates = new BlendAttachmentDescription[]
                    {
                        blending
                    }
                }
            }, false);
        }

        private Pipeline GetPipeline(GxBlendMode blendMode, bool unculled)
        {
            if (pipelines.TryGetValue((blendMode, unculled), out var pipeline))
                return pipeline;

            throw new Exception($"Pipeline for {blendMode}, {unculled} not found");
        }

        public async ValueTask<WmoInstance?> LoadWorldMapObject(FileId path)
        {
            if (meshes.TryGetValue(path, out var mesh))
            {
                if (mesh.TryGetTarget(out var target))
                    return target;
                meshes.Remove(path);
            }

            if (meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            {
                return await loadInProgress;
            }

            var completion = new TaskCompletionSource<WmoInstance?>();
            meshesCurrentlyLoaded[path] = completion.Task;

            var bytes = await gameFiles.ReadFile(path);
            if (bytes == null)
            {
                meshes[path] = null;
                meshesCurrentlyLoaded.Remove(path);
                completion.SetResult(null);
                return null;
            }

            var wmo = WMO.Read(new MemoryBinaryReader(bytes), gameFiles.WoWVersion);
            bytes.Dispose();

            List<WorldMapObjectGroup> groups = new();
            for (int i = 0; i < wmo.Header.nGroups; ++i)
            {
                FileId groupFile;
                if (wmo.GroupFileDataIdsPerLods != null)
                    groupFile = wmo.GroupFileDataIdsPerLods[0, i];
                else
                    groupFile = path.Replace(".wmo", "_" + i.ToString().PadLeft(3, '0') + ".wmo", StringComparison.OrdinalIgnoreCase);
                
                var bytesGroup = await gameFiles.ReadFile(groupFile);
                if (bytesGroup == null)
                    continue;

                var group = new WorldMapObjectGroup(new MemoryBinaryReader(bytesGroup), in wmo.Header);
                bytesGroup.Dispose();
                // bazaarfacade03 and cathy_facade01 - LODs for stormwind used by portal culling,
                // but gives poor results without portal culling
                if (group.Header.uniqueID is 2625 or 2624)
                    continue;
                groups.Add(group);
            }

            var wmoInstance = new WmoInstance();

            foreach (var group in groups)
            {
                if (group.Batches == null)
                {
                    group.Dispose();
                    continue;
                }
                
                ushort[] indices = new ushort[group.Indices.Length + group.CollisionOnlyIndices.Length];
                Array.Copy(group.Indices.AsArray(), indices, group.Indices.Length);
                Array.Copy(group.CollisionOnlyIndices, 0, indices, group.Indices.Length, group.CollisionOnlyIndices.Length);
                var wmoMeshData = new MeshData(group.Vertices.AsArray(), group.Normals.AsArray(), group.UVs.Count >= 1 ? group.UVs[0].AsArray() : null,
                    indices, group.Vertices.Length, group.Indices.Length,
                    group.UVs.Count >= 2 ? group.UVs[1].AsArray() : null, group.VertexColors?.AsArray());
                
                var wmoMesh = meshManager.CreateMesh(wmoMeshData);
                
                wmoMesh.SetSubmeshCount(group.Batches.Length + 1); // + 1 for collision only submesh
                int j = 0;
                Material[] materials = new Material[group.Batches.Length];
                foreach (var batch in group.Batches)
                {
                    wmoMesh.SetSubmeshIndicesRange(j++, (int)batch.startIndex, batch.count);
                    var mat = CreateMaterial(wmo, group, batch.material_id, out var tex1, out var tex2, out var tex3);
                    
                    if (tex1 != null)
                    {
                        mat.SetTexture("texture1", await textureManager.GetTexture(tex1));
                    }
                    if (tex2 != null)
                    {
                        mat.SetTexture("texture2", await textureManager.GetTexture(tex2));
                    }

                    materials[j - 1] = mat;
                }

                if (group.CollisionOnlyIndices.Length > 0)
                    wmoMesh.SetSubmeshIndicesRange(j++, group.Indices.Length, group.CollisionOnlyIndices.Length);

                wmoMesh.RebuildIndices();
                wmoInstance.meshes.Add((wmoMesh, materials));

                if (group.Header.flags.HasFlagFast(WorldMapObjectGroupFlags.HasWater))
                {
                    var (liquidVertices, liquidIndices) = woWMeshManager.GenerateWmoWaterMesh(in group.Liquid);
                    var waterMesh = meshManager.CreateMesh(liquidVertices, liquidIndices);
                    Material[] liquidMaterials = new Material[1];
                    liquidMaterials[0] = woWMeshManager.WaterMaterial;
                    wmoInstance.meshes.Add((waterMesh, liquidMaterials));
                }

                group.Dispose();
            }

            meshes.Add(path, new WeakReference<WmoInstance>(wmoInstance));
            completion.SetResult(wmoInstance);
            meshesCurrentlyLoaded.Remove(path);
            return wmoInstance;
        }

        private Material<WmoMaterialData> CreateMaterial(WMO wmo, WorldMapObjectGroup group, int materialId, out string? tex1, out string? tex2, out string? tex3)
        {
            ref readonly var materialDef = ref wmo.Materials[materialId];
            var pipeline = GetPipeline(materialDef.blendMode,materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.unculled));
            var mat = materialManager.CreateMaterial<WmoMaterialData>(pipeline);

            WmoMaterialData data = new WmoMaterialData()
            {
                shader_id = (int)materialDef.shader,
                translucent = 0,
            };
            //mat.SetUniform("notSupported", 0.0f);
            float alphaTest = 0.003921568f; // 1/255

            if (materialDef.blendMode == GxBlendMode.GxBlend_Opaque)
            {
                alphaTest = -1.0f;
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_AlphaKey)
            {
                alphaTest = 0.878431372f; // 224/255
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_Alpha)
            {
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_Add)
            {
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_Mod)
            {
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_Mod2x)
            {
            }
            else if (materialDef.blendMode == GxBlendMode.GxBlend_ModAdd)
            {
            }
            else
            {
                alphaTest = -1.0f;
                //mat.SetUniform("notSupported", 1);
            }

            data.alphaTest = alphaTest;
            data.unlit = materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.unlit) ? 1 : 0;
            data.brightAtNight =
                materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.brightAtNight) ? 1 : 0;
            data.interior =
                group.Header.flags.HasFlagFast(WorldMapObjectGroupFlags.Interior) && group.VertexColors != null ? 1 : 0;

            tex1 = materialDef.texture1Name;
            tex2 = materialDef.texture2Name;
            tex3 = materialDef.texture3Name;
            mat.SetTexture("texture1", textureManager.EmptyTexture);
            mat.SetTexture("texture2", textureManager.EmptyTexture);

            mat.SetMaterialData(ref data);
            return mat;
        }

        public void Dispose()
        {
            foreach (var wmo in meshes.Values)
            {
                if (wmo.TryGetTarget(out var target))
                    target.Dispose(meshManager);
            }
            meshes.Clear();
        }
    }
}
