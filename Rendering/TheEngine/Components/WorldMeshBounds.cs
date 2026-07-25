using System;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct WorldMeshBounds : IComponentData
    {
        public BoundingBox box;

        private WorldMeshBounds(BoundingBox box)
        {
            this.box = box;
        }

        public static WorldMeshBounds FromLocal(in MeshBounds local, in LocalToWorld localToWorld)
        {
            Span<Vector3> corners = stackalloc Vector3[8];
            return FromLocal(in local, in localToWorld, ref corners);
        }

        public static WorldMeshBounds FromLocal(in MeshBounds local, in LocalToWorld localToWorld, ref Span<Vector3> corners)
        {
            var matrix = localToWorld.Matrix;
            var min = local.box.Minimum;
            var max = local.box.Maximum;
            corners[0] = new Vector3(min.X, max.Y, max.Z);
            corners[1] = new Vector3(max.X, max.Y, max.Z);
            corners[2] = new Vector3(max.X, min.Y, max.Z);
            corners[3] = new Vector3(min.X, min.Y, max.Z);
            corners[4] = new Vector3(min.X, max.Y, min.Z);
            corners[5] = new Vector3(max.X, max.Y, min.Z);
            corners[6] = new Vector3(max.X, min.Y, min.Z);
            corners[7] = new Vector3(min.X, min.Y, min.Z);
            for (int j = 0; j < 8; ++j)
            {
                var vec4 = new Vector4(corners[j], 1);
                var worldspace = Vector4.Transform(vec4, matrix);
                corners[j] = worldspace.XYZ();
            }

            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int j = 0; j < 8; ++j)
            {
                min.X = Math.Min(min.X, corners[j].X);
                min.Y = Math.Min(min.Y, corners[j].Y);
                min.Z = Math.Min(min.Z, corners[j].Z);

                max.X = Math.Max(max.X, corners[j].X);
                max.Y = Math.Max(max.Y, corners[j].Y);
                max.Z = Math.Max(max.Z, corners[j].Z);
            }

            return (WorldMeshBounds)new BoundingBox(min, max);
        }

        public static implicit operator BoundingBox(WorldMeshBounds d) => d.box;
        public static explicit operator WorldMeshBounds(BoundingBox b) => new WorldMeshBounds(b);
    }
}
