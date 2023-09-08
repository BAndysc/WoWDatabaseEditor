using System.Collections;
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
        private readonly IGameFiles gameFiles;
        private readonly IMeshManager meshManager;
        private readonly WoWTextureManager textureManager;
        private readonly IMaterialManager materialManager;
        private readonly WoWMeshManager woWMeshManager;

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

        private Dictionary<FileId, WmoInstance?> meshes = new();
        private Dictionary<FileId, Task> meshesCurrentlyLoaded = new();

        public WmoManager(IGameFiles gameFiles,
            IMeshManager meshManager,
            WoWTextureManager textureManager,
            IMaterialManager materialManager,
            WoWMeshManager woWMeshManager)
        {
            this.gameFiles = gameFiles;
            this.meshManager = meshManager;
            this.textureManager = textureManager;
            this.materialManager = materialManager;
            this.woWMeshManager = woWMeshManager;
        }

        public IEnumerator LoadWorldMapObject(FileId path, TaskCompletionSource<WmoInstance?> result)
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

            var bytes = gameFiles.ReadFile(path);
            yield return bytes;
            if (bytes.Result == null)
            {
                meshes[path] = null;
                meshesCurrentlyLoaded.Remove(path);
                completion.SetResult(null);
                result.SetResult(null);
                yield break;
            }

            var wmo = WMO.Read(new MemoryBinaryReader(bytes.Result), gameFiles.WoWVersion);
            bytes.Result.Dispose();

            List<WorldMapObjectGroup> groups = new();
            for (int i = 0; i < wmo.Header.nGroups; ++i)
            {
                FileId groupFile;
                if (wmo.GroupFileDataIdsPerLods != null)
                    groupFile = wmo.GroupFileDataIdsPerLods[0, i];
                else
                    groupFile = path.Replace(".wmo", "_" + i.ToString().PadLeft(3, '0') + ".wmo", StringComparison.OrdinalIgnoreCase);
                
                var bytesGroup = gameFiles.ReadFile(groupFile);
                yield return bytesGroup;
                if (bytesGroup.Result == null)
                    continue;

                var group = new WorldMapObjectGroup(new MemoryBinaryReader(bytesGroup.Result), in wmo.Header);
                bytesGroup.Result.Dispose();
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
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return textureManager.GetTexture(tex1, tcs);
                        mat.SetTexture("texture1", tcs.Task.Result);
                    }
                    if (tex2 != null)
                    {
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return textureManager.GetTexture(tex2, tcs);
                        mat.SetTexture("texture2", tcs.Task.Result);
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

            meshes.Add(path, wmoInstance);
            completion.SetResult(wmoInstance);
            meshesCurrentlyLoaded.Remove(path);
            result.SetResult(wmoInstance);
        }

        private Material CreateMaterial(WMO wmo, WorldMapObjectGroup group, int materialId, out string? tex1, out string? tex2, out string? tex3)
        {
            ref readonly var materialDef = ref wmo.Materials[materialId];
            var mat = materialManager.CreateMaterial("data/wmo.json");

            mat.SetUniformInt("shader_id", (int)materialDef.shader);
            mat.SetUniformInt("translucent", 0);
            //mat.SetUniform("notSupported", 0.0f);
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
                //mat.SetUniform("notSupported", 1);
            }

            mat.ZWrite = !mat.BlendingEnabled;
            mat.SetUniform("alphaTest", alphaTest);
            mat.SetUniformInt("unlit", materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.unlit) ? 1 : 0);
            mat.SetUniformInt("brightAtNight",
                materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.brightAtNight) ? 1 : 0);
            mat.SetUniformInt("interior",
                group.Header.flags.HasFlagFast(WorldMapObjectGroupFlags.Interior) && group.VertexColors != null ? 1 : 0);

            if (materialDef.flags.HasFlagFast(WorldMapObjectMaterial.Flags.unculled))
                mat.Culling = CullingMode.Off;

            tex1 = materialDef.texture1Name;
            tex2 = materialDef.texture2Name;
            tex3 = materialDef.texture3Name;
            mat.SetTexture("texture1", textureManager.EmptyTexture);
            mat.SetTexture("texture2", textureManager.EmptyTexture);
            
            return mat;
        }

        public void Dispose()
        {
            foreach (var wmo in meshes.Values)
                wmo?.Dispose(meshManager);
        }
    }
}
