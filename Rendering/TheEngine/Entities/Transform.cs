using Avalonia;
using TheMaths;

namespace TheEngine.Entities
{
    public class Transform
    {
        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateMatrix();
            }
        }

        private Vector3 _scale;
        public Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                UpdateMatrix();
            }
        }

        private Quaternion _rotation;
        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                UpdateMatrix();
            }
        }

        public Vector3 Forward => Vector3.Transform(Vectors.Forward, Rotation);

        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public Matrix LocalToWorldMatrix;
        public Matrix WorldToLocalMatrix;

        private void UpdateMatrix()
        {
            LocalToWorldMatrix = Utilities.Transformation(Vector3.Zero, Quaternion.Identity, Scale, Vector3.Zero, Rotation, Position);
            Matrix.Invert(LocalToWorldMatrix, out WorldToLocalMatrix);
        }
    }
}
