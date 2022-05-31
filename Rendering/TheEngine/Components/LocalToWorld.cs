using System.Runtime.CompilerServices;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct CopyParentTransform : IComponentData
    {
        public Entity Parent { get; set; }
    }

    public static class CopyParentTransformExtensions
    {
        public static void SetCopyParentTransform(this Entity entity, IEntityManager entityManager, Entity parent)
        {
            entityManager.GetComponent<CopyParentTransform>(entity).Parent = parent;
        }
    }

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Matrix.TRS(value, Rotation, Scale);
        }

        public Vector3 Scale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.ScaleVector;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Matrix.TRS(Position, Rotation, value);
        }

        public Quaternion Rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.Rotation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Matrix.TRS(Position, value, Scale);
        }

        public static implicit operator Matrix(LocalToWorld d) => d.matrix;
        public static explicit operator LocalToWorld(Matrix b) => new LocalToWorld(b);
    }
    
    public static class LocalToWorldExtensions
    {
        public static void SetMatrix(this Entity entity, IEntityManager entityManager, in Matrix matrix)
        {
            entityManager.GetComponent<LocalToWorld>(entity).Matrix = matrix;
        }
        
        public static void SetTRS(this Entity entity, IEntityManager entityManager, in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            entityManager.GetComponent<LocalToWorld>(entity).Matrix = Matrix.TRS(in position, in rotation, in scale);
        }
    }
}