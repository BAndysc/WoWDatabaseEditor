using System.Runtime.CompilerServices;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct CopyParentTransform : IComponentData
    {
        public Entity Parent { get; set; }
        public Matrix? Local { get; set; }
    }

    public static class CopyParentTransformExtensions
    {
        public static void SetCopyParentTransform(this Entity entity, IEntityManager entityManager, Entity parent)
        {
            entityManager.GetComponent<CopyParentTransform>(entity).Parent = parent;
        }

        public static Entity GetRoot(this Entity entity, IEntityManager entityManager)
        {
            var copyTransformArchetype = entityManager.NewArchetype().WithComponentData<CopyParentTransform>();
            while (entityManager.Is(entity, copyTransformArchetype))
            {
                entity = entityManager.GetComponent<CopyParentTransform>(entity).Parent;
            }

            return entity;
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
                Matrix.Invert(matrix, out inv);
            }
        }
        private Matrix inv;

        public Matrix Inverse => inv;

        private LocalToWorld(Matrix matrix)
        {
            this.matrix = matrix;
            Matrix.Invert(matrix, out inv);
        }

        public Vector3 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.Translation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Utilities.TRS(value, Rotation, Scale);
        }

        public Vector3 Scale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.ScaleVector();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Utilities.TRS(Position, Rotation, value);
        }

        public Quaternion Rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => matrix.Rotation();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => matrix = Utilities.TRS(Position, value, Scale);
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
            entityManager.GetComponent<LocalToWorld>(entity).Matrix = Utilities.TRS(in position, in rotation, in scale);
        }
    }
}