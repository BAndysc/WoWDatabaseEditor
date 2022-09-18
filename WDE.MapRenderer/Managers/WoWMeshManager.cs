using TheEngine.Entities;
using TheEngine.Interfaces;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class WoWMeshManager : System.IDisposable
    {
        private readonly IMeshManager meshManager;
        private readonly IRenderManager renderManager;
        private readonly LightingManager lightingManager;
        private readonly TimeManager timeManager;
        private IMesh chunkMesh;

        public WoWMeshManager(IMeshManager meshManager,
            IRenderManager renderManager,
            ITextureManager textureManager, 
            IMaterialManager materialManager,
            LightingManager lightingManager,
            TimeManager timeManager)
        {
            this.meshManager = meshManager;
            this.renderManager = renderManager;
            this.lightingManager = lightingManager;
            this.timeManager = timeManager;
            chunkMesh = meshManager.CreateMesh(ChunkMesh.Create());
            
            WaterMaterial = materialManager.CreateMaterial("data/water.json");
            WaterMaterial.Culling = CullingMode.Off;
            WaterMaterial.ZWrite = false;
            WaterMaterial.SourceBlending = Blending.SrcAlpha;
            WaterMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            WaterMaterial.BlendingEnabled = true;
            WaterMaterial.SetTexture("_WaterTexture", textureManager.LoadTexture("textures/water.png"));
            WaterMaterial.SetUniform("color", new Vector4(0.28f, 1, 0.95f, 0.4f));
        }

        public void Render()
        {
            WaterMaterial.SetTexture("_DepthTexture", renderManager.DepthTexture);
            WaterMaterial.SetTexture("_SceneColor", renderManager.OpaqueTexture);
            if (lightingManager.BestLight is { } light)
            {
                var time = timeManager.Time;
                var oceanShallow = light.NormalWeather.GetLightParameter(LightIntParamType.OceanShallow).GetColorAtTime(time);
                var oceanDeep = light.NormalWeather.GetLightParameter(LightIntParamType.OceanDeep).GetColorAtTime(time);
                //var riverShallow = light.NormalWeather.GetLightParameter(LightIntParamType.RiverShallow).GetColorAtTime(time);
                //var riverDeep = light.NormalWeather.GetLightParameter(LightIntParamType.RiverDeep).GetColorAtTime(time);
                WaterMaterial.SetUniform("deepColor", oceanDeep.ToRgbaVector() with {W=0.55f});
                WaterMaterial.SetUniform("shallowColor", oceanShallow.ToRgbaVector() with {W=0.85f});
            }
        }
        
        public Material WaterMaterial { get; }

        public IMesh MeshOfChunk => chunkMesh;

        public (Vector3[], ushort[]) GenerateGiganticWaterMesh(float height)
        {
            Vector3[] vertices;
            ushort[] indices;
            var offset = new Vector3(0, 0, height);
            vertices = new Vector3[4]
            {
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(-Constants.BlockSize, 0, 0),
                offset + new Vector3(-Constants.BlockSize, -Constants.BlockSize, 0),
                offset + new Vector3(0, -Constants.BlockSize, 0),
            };
            indices = new ushort[2 * 3]
            {
                0, 1, 2,
                0, 2, 3
            };
            return (vertices, indices);
        }
        
        public (Vector3[], ushort[]) GenerateWaterMesh(MH2OLiquidInstance mh2o)
        {
            Vector3[] vertices;
            ushort[] indices;
            // optimized path
            if (mh2o.LiquidVertexFormat == 2 || mh2o.IsSingleHeight)
            {
                var offset = new Vector3(-mh2o.Y_Offset * Constants.UnitSize, -mh2o.X_Offset * Constants.UnitSize, mh2o.MinHeightLevel);
                vertices = new Vector3[4]
                {
                    offset + new Vector3(0, 0, 0),
                    offset + new Vector3(-Constants.UnitSize * mh2o.Height, 0, 0),
                    offset + new Vector3(-Constants.UnitSize * mh2o.Height, -Constants.UnitSize * mh2o.Width, 0),
                    offset + new Vector3(0, -Constants.UnitSize * mh2o.Width, 0),
                };
                indices = new ushort[2 * 3]
                {
                    0, 1, 2,
                    0, 2, 3
                };
            }
            else
            {
                var tilecount = mh2o.Width * mh2o.Height;
                var vertCount = (mh2o.Height + 1) * (mh2o.Width + 1);
                vertices = new Vector3[vertCount];
                indices = new ushort[tilecount * 2 * 3];
                for (int y = 0; y < mh2o.Height + 1; ++y)
                {
                    for (int x = 0; x < mh2o.Width + 1; ++x)
                    {
                        var v_index = y * (mh2o.Width + 1) + x;
                        vertices[v_index] = new Vector3(-Constants.UnitSize * (y + mh2o.Y_Offset), -Constants.UnitSize * (x + mh2o.X_Offset), mh2o.HeightMap[v_index]);
                    }
                }
                int index = 0;
                for (int tileY = 0; tileY < mh2o.Height; ++tileY)
                {
                    for (int tileX = 0; tileX < mh2o.Width; ++tileX) // X
                    {
                        bool renderState = mh2o.RenderBitArray[(tileY) * mh2o.Width + (tileX)];

                        if (!renderState)
                            continue;

                        var vertexIndex = ((tileY)) * (mh2o.Width + 1) + (tileX);
                        int tl = vertexIndex;
                        int tr = tl + 1;
                        int bl = vertexIndex + (mh2o.Width + 1); // +mhwo width is a new row
                        int br = bl + 1;

                        // 1st triangle
                        indices[index++] = (ushort)tl;
                        indices[index++] = (ushort)bl;
                        indices[index++] = (ushort)tr;
                        // 2nd triangle
                        indices[index++] = (ushort)tr;
                        indices[index++] = (ushort)bl;
                        indices[index++] = (ushort)br;
                    }
                }
            }
            return (vertices, indices);
        }
        
        public void Dispose()
        {
            meshManager.DisposeMesh(chunkMesh);
            chunkMesh = null!;
        }
    }
}