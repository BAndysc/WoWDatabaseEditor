using TheEngine.ECS;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Components
{
    public struct LocalToWorld : IComponentData
    {
        private Matrix matrix;
        public Matrix Matrix
        {
            get
            {
                return matrix;
            }
            set
            {
                matrix = value;
                inv = matrix;
                inv.Invert();
            }
        }
        private Matrix inv;

        public Matrix Inverse => inv;

        private LocalToWorld(Matrix matrix)
        {
            this.matrix = matrix;
            inv = matrix;
            inv.Invert();
        }

        public Vector3 Position => matrix.TranslationVector;
        public Vector3 Scale => matrix.ScaleVector;
        
        public static implicit operator Matrix(LocalToWorld d) => d.matrix;
        public static explicit operator LocalToWorld(Matrix b) => new LocalToWorld(b);
    }

    public struct MeshRenderer : IComponentData
    {
        public MeshHandle MeshHandle;
        public MaterialHandle MaterialHandle;
        public int SubMeshId;
    }

    public struct WorldMeshBounds : IComponentData
    {
        public BoundingBox box;
        
        private WorldMeshBounds(BoundingBox box)
        {
            this.box = box;
        }

        public static implicit operator BoundingBox(WorldMeshBounds d) => d.box;
        public static explicit operator WorldMeshBounds(BoundingBox b) => new WorldMeshBounds(b);
    }

    public struct MeshBounds : IComponentData
    {
        public BoundingBox box;
        
        private MeshBounds(BoundingBox box)
        {
            this.box = box;
        }

        public static implicit operator BoundingBox(MeshBounds d) => d.box;
        public static explicit operator MeshBounds(BoundingBox b) => new MeshBounds(b);
    }

    public struct DirtyPosition : IComponentData
    {
        private byte enabled;

        private DirtyPosition(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public void Enable()
        {
            enabled = 1;
        }

        public void Disable()
        {
            enabled = 0;
        }

        public static implicit operator bool(DirtyPosition d) => d.enabled == 1;
        public static explicit operator DirtyPosition(bool b) => new DirtyPosition(b);
    }

    public struct RenderEnabledBit : IComponentData
    {
        private byte enabled;

        private RenderEnabledBit(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public void Enable()
        {
            enabled = 1;
        }

        public void Disable()
        {
            enabled = 0;
        }

        public static implicit operator bool(RenderEnabledBit d) => d.enabled == 1;
        public static explicit operator RenderEnabledBit(bool b) => new RenderEnabledBit(b);
    }
}