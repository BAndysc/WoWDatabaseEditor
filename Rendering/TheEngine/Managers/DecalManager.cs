using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Managers
{
    public partial class DecalManager : IDecalManager, IDisposable
    {
        private readonly Engine engine;
        Archetype decals;

        // High, fixed offset reserved for decal pick indices (see RenderManager.PickObject),
        // comfortably above any realistic ObjectDrawRenderStage.TotalToDraw for a frame - keeps
        // decal picking fully self-contained, with zero changes to ObjectDrawRenderStage's
        // existing 1-based mesh pick-index scheme.
        internal const uint PickIdBase = 1u << 24;

        private GpuDecal[] gpuDecals = new GpuDecal[Constants.FORWARD_PLUS_MAX_DECALS];
        // Reverse lookup from this frame's PickId back to the source Entity. Fully repopulated
        // by GatherDecals every frame (before that frame renders), and the GPU can only ever
        // echo back a PickId actually present in that same frame's uploaded DecalBuffer
        // (bounded by sceneData.DecalCount), so reusing the array across frames is safe.
        private readonly Entity[] decalEntitiesForPicking = new Entity[Constants.FORWARD_PLUS_MAX_DECALS];

        internal DecalManager(Engine engine)
        {
            this.engine = engine;
            decals = engine.entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Decal>();
        }

        internal Entity EntityAtPickIndex(int index) => decalEntitiesForPicking[index];

        /// <summary>
        /// Packs every entity matching (LocalToWorld, Decal) into <see cref="gpuDecals"/> for
        /// upload to the current frame's Decal SSBO, and returns the count actually written
        /// (capped at FORWARD_PLUS_MAX_DECALS). Decals without an Albedo texture are skipped,
        /// same as disabled decals.
        /// </summary>
        private partial struct GatherDecalsJob : IJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<LocalToWorld> transforms;
            private ComponentDataAccess<Decal> decalData;

            public TextureManager textureManager;
            public GpuDecal[] gpuDecals;
            public Entity[] decalEntitiesForPicking;
            public int count;

            public void Execute(int start, int end)
            {
                for (int i = start; i < end; i++)
                {
                    if (count >= gpuDecals.Length)
                        return;

                    ref readonly var transform = ref transforms[i];
                    ref readonly var decal = ref decalData[i];
                    if (decal.Disabled || decal.Albedo == null)
                        continue;

                    gpuDecals[count] = new GpuDecal
                    {
                        WorldToLocal = transform.Inverse,
                        PositionRadius = new Vector4(transform.Position, transform.Scale.Length()),
                        Color = decal.Color,
                        AlbedoTextureIndex = textureManager.GetBindlessIndex(decal.Albedo),
                        FadeAngleCos = decal.FadeAngleCos,
                        SortBias = 0,
                        PickId = PickIdBase + (uint)count,
                    };
                    decalEntitiesForPicking[count] = itr[i];
                    count++;
                }
            }
        }

        internal int GatherDecals(out GpuDecal[] decalsOut)
        {
            decalsOut = gpuDecals;
            var job = new GatherDecalsJob
            {
                textureManager = engine.textureManager,
                gpuDecals = gpuDecals,
                decalEntitiesForPicking = decalEntitiesForPicking,
            };
            job.Run(decals);
            return job.count;
        }

        public void Dispose()
        {

        }
    }
}
