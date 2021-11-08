using System.Runtime.CompilerServices;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct LocalToWorld : IComponentData
    {
        private Matrix matrix;
        public Matrix Matrix
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix;
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

        public Vector3 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.TranslationVector;
        }

        public Vector3 Scale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.ScaleVector;
        }

        public static implicit operator Matrix(LocalToWorld d) => d.matrix;
        public static explicit operator LocalToWorld(Matrix b) => new LocalToWorld(b);
    }
}