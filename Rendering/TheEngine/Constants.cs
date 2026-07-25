namespace TheEngine
{
    public class Constants
    {
        public static int INSTANCES_BUFFER_INDEX = 1;
        
        public static int DEFAULT_SAMPLER = 15;

        public static int SCENE_BUFFER_INDEX = 0;

        public static int PIXEL_SCENE_BUFFER_INDEX = 2;

        // Forward+ tiled light culling (set 0 storage buffers)
        public const int LIGHT_BUFFER_BINDING = 2;
        public const int LIGHT_GRID_BINDING = 3;
        public const int LIGHT_INDEX_LIST_BINDING = 4;

        // Forward+ tiled decal culling (set 0 storage buffers)
        public const int DECAL_BUFFER_BINDING = 5;
        public const int DECAL_GRID_BINDING = 6;
        public const int DECAL_INDEX_LIST_BINDING = 7;

        // The editor's scene view (a second camera, different frustum/depth/resolution than the
        // main game view) needs its own tile-culled grid/index results live at the same time as
        // the main view's, since both views' Transparent passes read their grids later in the
        // frame, after both views' Opaque passes have run. Rather than add a second set of SSBO
        // bindings (which pushed this device's maxPerStageDescriptorStorageBuffers limit, 31 on
        // this MoltenVK device, past its cap), the LightGrid/LightIndexList/DecalGrid/
        // DecalIndexList buffers are simply allocated GRID_SLOTS times larger and indexed with a
        // `gridSet * <per-slot-tile-count>` offset (see SceneData.gridSet, theengine.cginc).
        public const int FORWARD_PLUS_GRID_SLOTS = 2;

        public const int FORWARD_PLUS_TILE_SIZE = 16;
        public const int FORWARD_PLUS_MAX_LIGHTS = 1024;
        public const int FORWARD_PLUS_MAX_LIGHTS_PER_TILE = 64;
        public const int FORWARD_PLUS_MAX_DECALS = 512;
        public const int FORWARD_PLUS_MAX_DECALS_PER_TILE = 64;
        // fixed upper bound on the tile grid (covers up to 4096x4096 at 16px tiles), so the
        // LightGrid/LightIndexList and DecalGrid/DecalIndexList SSBOs never need to be resized
        // when the game view does - tilesX/tilesY (actual, per-frame) just index into a
        // subrange of these buffers.
        public const int FORWARD_PLUS_MAX_TILES_X = 256;
        public const int FORWARD_PLUS_MAX_TILES_Y = 256;

        // Cascaded shadow maps: number of frustum-split cascades. Must stay in sync with NUM_CASCADES
        // in theengine.cginc (and the fixed-size cascade arrays in SceneBuffer). Per-cascade resolution
        // is configured per CascadeShadowMap entity. The cascade depth textures are shared across the
        // game and scene views, re-rendered before each view's opaque pass.
        public const int SHADOW_CASCADE_COUNT = 4;

        public static string SHADER_INCLUDE_DIR = "internalShaders";
    }
}
