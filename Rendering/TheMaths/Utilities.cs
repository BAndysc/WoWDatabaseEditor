using System.Runtime.InteropServices;

namespace TheMaths
{
    public static class Vectors
    {
        /// <summary>
        /// The X unit <see cref="Vector3"/> (1, 0, 0).
        /// </summary>
        public static readonly Vector3 UnitX = new Vector3(1.0f, 0.0f, 0.0f);
    
        /// <summary>
        /// The Y unit <see cref="Vector3"/> (0, 1, 0).
        /// </summary>
        public static readonly Vector3 UnitY = new Vector3(0.0f, 1.0f, 0.0f);
    
        /// <summary>
        /// The Z unit <see cref="Vector3"/> (0, 0, 1).
        /// </summary>
        public static readonly Vector3 UnitZ = new Vector3(0.0f, 0.0f, 1.0f);
    
        /// <summary>
        /// A <see cref="Vector3"/> with all of its components set to one.
        /// </summary>
        public static readonly Vector3 One = new Vector3(1.0f, 1.0f, 1.0f);
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating up (0, 0, 1).
        /// </summary>
        public static readonly Vector3 Up = UnitZ;
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating down (0, 0, -1).
        /// </summary>
        public static readonly Vector3 Down = -UnitZ;
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating left (0, 1, 0).
        /// </summary>
        public static readonly Vector3 Left = UnitY;
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating right (0, -1, 0).
        /// </summary>
        public static readonly Vector3 Right = -UnitY;
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating forward in a right-handed coordinate system (1, 0, 0).
        /// </summary>
        public static readonly Vector3 Forward = UnitX;
    
        /// <summary>
        /// A unit <see cref="Vector3"/> designating backward in a right-handed coordinate system (1, 0, 0).
        /// </summary>
        public static readonly Vector3 Backward = -UnitX;

        public static Vector3 XYZ(this Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        
        public static Vector2 XY(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
        
        public static float Angle(this Vector2 that, Vector2 other)
         {
             return (float)Math.Acos(Vector2.Dot(other, that) / other.Length() / that.Length());
         }

         public static float SignedAngle(this Vector2 that,Vector2 other)
         {
             return Math.Sign(that.X * other.Y - that.Y * other.X) * that.Angle(other);
         }

         public static Vector3 Normalize(Vector3 a)
         {
             var norm = Vector3.Normalize(a);
             if (float.IsNaN(norm.X))
                 return Vector3.Zero;
             return norm;
         }
    }
    
    public static class Utilities
    {
        public static ref float Index(this ref Vector3 m, int index)
        {
            switch (index)
            {
                case 0:
                    return ref m.X;
                case 1:
                    return ref m.Y;
                case 2:
                    return ref m.Z;
            }
            throw new Exception("Index out of range");
        }
        
        public static ref float Index(this ref Vector4 m, int index)
        {
            switch (index)
            {
                case 0:
                    return ref m.X;
                case 1:
                    return ref m.Y;
                case 2:
                    return ref m.Z;
                case 3:
                    return ref m.W;
            }
            throw new Exception("Index out of range");
        }
    
        public static ref float Index(this ref Matrix m, int row, int col)
        {
            switch (row, col)
            {
                case (0, 0):
                    return ref m.M11;
                case (0, 1):
                    return ref m.M12;
                case (0, 2):
                    return ref m.M13;
                case (0, 3):
                    return ref m.M14;
                case (1, 0):
                    return ref m.M21;
                case (1, 1):
                    return ref m.M22;
                case (1, 2):
                    return ref m.M23;
                case (1, 3):
                    return ref m.M24;
                case (2, 0):
                    return ref m.M31;
                case (2, 1):
                    return ref m.M32;
                case (2, 2):
                    return ref m.M33;
                case (2, 3):
                    return ref m.M34;
                case (3, 0):
                    return ref m.M41;
                case (3, 1):
                    return ref m.M42;
                case (3, 2):
                    return ref m.M43;
                case (3, 3):
                    return ref m.M44;
            }

            throw new Exception($"Index ({row}, {col}) out of range");
        }
        
        /// A coordinate transform performs the transformation with the assumption that the w component
        /// is one. The four dimensional vector obtained from the transformation operation has each
        /// component in the vector divided by the w component. This forces the w component to be one and
        /// therefore makes the vector homogeneous. The homogeneous vector is often preferred when working
        /// with coordinates as the w component can safely be ignored.
        /// </remarks>
        public static void TransformCoordinate(in Vector3 coordinate, in Matrix transform, out Vector3 result)
        {
            Vector4 vector = new Vector4();
            vector.X = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + (coordinate.Z * transform.M31) + transform.M41;
            vector.Y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + (coordinate.Z * transform.M32) + transform.M42;
            vector.Z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + (coordinate.Z * transform.M33) + transform.M43;
            vector.W = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + (coordinate.Z * transform.M34) + transform.M44);
        
            result = new Vector3(vector.X * vector.W, vector.Y * vector.W, vector.Z * vector.W);
        }

        public static Vector3 TransformCoordinate(in Vector3 coordinate, in Matrix transform)
        {
            Vector3 result;
            TransformCoordinate(in coordinate, in transform, out result);
            return result;
        }
        
            /// <summary>
        /// Projects a 3D vector from screen space into object space. 
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="x">The X position of the viewport.</param>
        /// <param name="y">The Y position of the viewport.</param>
        /// <param name="width">The width of the viewport.</param>
        /// <param name="height">The height of the viewport.</param>
        /// <param name="minZ">The minimum depth of the viewport.</param>
        /// <param name="maxZ">The maximum depth of the viewport.</param>
        /// <param name="worldViewProjection">The combined world-view-projection matrix.</param>
        /// <param name="result">When the method completes, contains the vector in object space.</param>
        public static void Unproject(ref Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, ref Matrix worldViewProjection, out Vector3 result)
        {
            Vector3 v = new Vector3();
            Matrix matrix = new Matrix();
            Matrix.Invert( worldViewProjection, out matrix);
        
            v.X = (((vector.X - x) / width) * 2.0f) - 1.0f;
            v.Y = -((((vector.Y - y) / height) * 2.0f) - 1.0f);
            v.Z = (vector.Z - minZ) / (maxZ - minZ);
        
            TransformCoordinate(in v, in matrix, out result);
        }
            
                /// <summary>
        /// Performs a coordinate transformation on an array of vectors using the given <see cref="Matrix"/>.
        /// </summary>
        /// <param name="source">The array of coordinate vectors to transform.</param>
        /// <param name="transform">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        /// <remarks>
        /// A coordinate transform performs the transformation with the assumption that the w component
        /// is one. The four dimensional vector obtained from the transformation operation has each
        /// component in the vector divided by the w component. This forces the w component to be one and
        /// therefore makes the vector homogeneous. The homogeneous vector is often preferred when working
        /// with coordinates as the w component can safely be ignored.
        /// </remarks>
        public static void TransformCoordinate(Vector3[] source, ref Matrix transform, Vector3[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");
        
            for (int i = 0; i < source.Length; ++i)
            {
                TransformCoordinate(source[i], transform, out destination[i]);
            }
        }
                
                /// <summary>
        /// Creates a OldQuaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created OldQuaternion.</param>
        public static void RotationMatrix(ref Matrix3x3 matrix, out Quaternion result)
        {
            float sqrt;
            float half;
            float scale = matrix.M11 + matrix.M22 + matrix.M33;

            if (scale > 0.0f)
            {
                sqrt = (float)Math.Sqrt(scale + 1.0f);
                result.W = sqrt * 0.5f;
                sqrt = 0.5f / sqrt;

                result.X = (matrix.M23 - matrix.M32) * sqrt;
                result.Y = (matrix.M31 - matrix.M13) * sqrt;
                result.Z = (matrix.M12 - matrix.M21) * sqrt;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = 0.5f * sqrt;
                result.Y = (matrix.M12 + matrix.M21) * half;
                result.Z = (matrix.M13 + matrix.M31) * half;
                result.W = (matrix.M23 - matrix.M32) * half;
            }
            else if (matrix.M22 > matrix.M33)
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = (matrix.M21 + matrix.M12) * half;
                result.Y = 0.5f * sqrt;
                result.Z = (matrix.M32 + matrix.M23) * half;
                result.W = (matrix.M31 - matrix.M13) * half;
            }
            else
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                half = 0.5f / sqrt;

                result.X = (matrix.M31 + matrix.M13) * half;
                result.Y = (matrix.M32 + matrix.M23) * half;
                result.Z = 0.5f * sqrt;
                result.W = (matrix.M12 - matrix.M21) * half;
            }
        }
        
        /// <summary>
        /// Projects a 3D vector from screen space into object space. 
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="x">The X position of the viewport.</param>
        /// <param name="y">The Y position of the viewport.</param>
        /// <param name="width">The width of the viewport.</param>
        /// <param name="height">The height of the viewport.</param>
        /// <param name="minZ">The minimum depth of the viewport.</param>
        /// <param name="maxZ">The maximum depth of the viewport.</param>
        /// <param name="worldViewProjection">The combined world-view-projection matrix.</param>
        /// <returns>The vector in object space.</returns>
        public static Vector3 Unproject(Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, Matrix worldViewProjection)
        {
            Vector3 result;
            Unproject(ref vector, x, y, width, height, minZ, maxZ, ref worldViewProjection, out result);
            return result;
        }

        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }

        public static void MinMax(this float f, ref float min, ref float max)
        {
            if (f < min)
                min = f;
            if (f > max)
                max = f;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        
                 /// <summary>
         /// Creates a transformation matrix.
         /// </summary>
         /// <param name="scalingCenter">Center point of the scaling operation.</param>
         /// <param name="scalingRotation">Scaling rotation amount.</param>
         /// <param name="scaling">Scaling factor.</param>
         /// <param name="rotationCenter">The center of the rotation.</param>
         /// <param name="rotation">The rotation of the transformation.</param>
         /// <param name="translation">The translation factor of the transformation.</param>
         /// <param name="result">When the method completes, contains the created transformation matrix.</param>
         public static void Transformation(in Vector3 scaling, in Quaternion rotation, in Vector3 translation, out Matrix result)
         {
             result = Matrix.CreateScale(scaling) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);       
         }

         /// <summary>
         /// Creates a transformation matrix.
         /// </summary>
         /// <param name="scalingCenter">Center point of the scaling operation.</param>
         /// <param name="scalingRotation">Scaling rotation amount.</param>
         /// <param name="scaling">Scaling factor.</param>
         /// <param name="rotationCenter">The center of the rotation.</param>
         /// <param name="rotation">The rotation of the transformation.</param>
         /// <param name="translation">The translation factor of the transformation.</param>
         /// <returns>The created transformation matrix.</returns>
         public static Matrix Transformation(in Vector3 scalingCenter, in Quaternion scalingRotation, in Vector3 scaling, in Vector3 rotationCenter, in Quaternion rotation, in Vector3 translation)
         {
             Matrix result;
             Transformation(in scaling, in rotation, in translation, out result);
             return result;
         }

         public static Matrix TRS(in Vector3 position, in Quaternion rotation, in Vector3 scale)
         {
             return Transformation(Vector3.Zero, Quaternion.Identity, in scale, Vector3.Zero, in rotation, in position);
         }

         public static Vector3 Multiply(this Vector3 point, Quaternion rotation)
         {
             return rotation.Multiply(point);
         }

         public static Vector3 Multiply(this Quaternion rotation, Vector3 point)
         {
             return Vector3.Transform(point, rotation);
             // float num1 = rotation.X * 2f;
             // float num2 = rotation.Y * 2f;
             // float num3 = rotation.Z * 2f;
             // float xx = rotation.X * num1;
             // float yy = rotation.Y * num2;
             // float zz = rotation.Z * num3;
             // float xy = rotation.X * num2;
             // float xz = rotation.X * num3;
             // float yz = rotation.Y * num3;
             // float wx = rotation.W * num1;
             // float wy = rotation.W * num2;
             // float wz = rotation.W * num3;
             // Vector3 vector3;
             // vector3.X = (float) ((1.0 - (yy + (double) zz)) * point.X + (xy - (double) wz) * point.Y + (xz + (double) wy) * point.Z);
             // vector3.Y = (float) ((xy + (double) wz) * point.X + (1.0 - (xx + (double) zz)) * point.Y + (yz - (double) wx) * point.Z);
             // vector3.Z = (float) ((xz - (double) wy) * point.X + (yz + (double) wx) * point.Y + (1.0 - (xx + (double) yy)) * point.Z);
             // return vector3;
         }
         
         public static Quaternion Rotation(this Matrix m)
         {
             Vector3 scale;
             //Scaling is the length of the rows.
             scale.X = (float)Math.Sqrt((m.M11 * m.M11) + (m.M12 * m.M12) + (m.M13 * m.M13));
             scale.Y = (float)Math.Sqrt((m.M21 * m.M21) + (m.M22 * m.M22) + (m.M23 * m.M23));
             scale.Z = (float)Math.Sqrt((m.M31 * m.M31) + (m.M32 * m.M32) + (m.M33 * m.M33));

             //If any of the scaling factors are zero, than the rotation matrix can not exist.
             if (MathUtil.IsZero(scale.X) ||
                 MathUtil.IsZero(scale.Y) ||
                 MathUtil.IsZero(scale.Z))
             {
                 return Quaternion.Identity;
             }

             //The rotation is the left over matrix after dividing out the scaling.
             Matrix rotationmatrix = new Matrix();
             rotationmatrix.M11 = m.M11 / scale.X;
             rotationmatrix.M12 = m.M12 / scale.X;
             rotationmatrix.M13 = m.M13 / scale.X;

             rotationmatrix.M21 = m.M21 / scale.Y;
             rotationmatrix.M22 = m.M22 / scale.Y;
             rotationmatrix.M23 = m.M23 / scale.Y;

             rotationmatrix.M31 = m.M31 / scale.Z;
             rotationmatrix.M32 = m.M32 / scale.Z;
             rotationmatrix.M33 = m.M33 / scale.Z;

             rotationmatrix.M44 = 1f;

             return Quaternion.CreateFromRotationMatrix(rotationmatrix);
         }
         
        public static Vector3 ScaleVector(this Matrix m)
         {
             return new Vector3((float)Math.Sqrt((m.M11 * m.M11) + (m.M12 * m.M12) + (m.M13 * m.M13)),
                 (float)Math.Sqrt((m.M21 * m.M21) + (m.M22 * m.M22) + (m.M23 * m.M23)),
                 (float)Math.Sqrt((m.M31 * m.M31) + (m.M32 * m.M32) + (m.M33 * m.M33)));
         }

        
        public static Vector4 WithW(this Vector4 v, float w)
        {
            return new Vector4(v.X, v.Y, v.Z, w);
        }
        
        public static Vector3 WithZ(this Vector3 w, float z)
        {
            return new Vector3(w.X, w.Y, z);
        }
        
        /// <summary>
        /// Gets the angle of the OldQuaternion.
        /// </summary>
        /// <value>The OldQuaternion's angle.</value>
        public static float Angle(this Quaternion q)
        {
            float length = (q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z);
            if (MathUtil.IsZero(length))
                return 0.0f;

            return (float)(2.0 * Math.Acos(MathUtil.Clamp(q.W, -1f, 1f)));
        }

        /// <summary>
        /// Gets the axis components of the OldQuaternion.
        /// </summary>
        /// <value>The axis components of the OldQuaternion.</value>
        public static Vector3 Axis(this Quaternion q)
        {
            float length = (q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z);
            if (MathUtil.IsZero(length))
                return Vector3.UnitX;

            float inv = 1.0f / (float)Math.Sqrt(length);
            return new Vector3(q.X * inv, q.Y * inv, q.Z * inv);
        }
        
        public static Quaternion FromEuler(double yaw, double roll, double pitch)
        {
            roll = roll * Math.PI / 180;
            yaw = yaw * Math.PI / 180;
            pitch = pitch * Math.PI / 180;
            double cy = Math.Cos(roll * 0.5);
            double sy = Math.Sin(roll * 0.5);
            double cp = Math.Cos(yaw * 0.5);
            double sp = Math.Sin(yaw * 0.5);
            double cr = Math.Cos(pitch * 0.5);
            double sr = Math.Sin(pitch * 0.5);

            Quaternion q = new Quaternion((float)(sr * cp * cy - cr * sp * sy),
                (float)(cr * sp * cy + sr * cp * sy),
                (float)(cr * cp * sy - sr * sp * cy),
                (float)(cr * cp * cy + sr * sp * sy));
            

            return q;
        }
        
        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The direction of looking.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            if (Vector3.Dot(forward, up) > 0.9999f)
                return FromEuler(270, 0, 0);
            if (Vector3.Dot(forward, up) < -0.9999f)
                return FromEuler(90, 0, 0);
            forward = Vectors.Normalize(forward);
            Vector3 right = Vectors.Normalize(Vector3.Cross(forward, up));
            up = Vector3.Cross(right, forward);
            
            var rightX = -right.Y;
            var rightY = right.Z;
            var rightZ = right.X;
            var upX = -up.Y;
            var upY = up.Z;
            var upZ = up.X;
            var forwardX = -forward.Y;
            var forwardY = forward.Z;
            var forwardZ = forward.X;
            
            float diagonal = rightX + upY + forwardZ;
            var OldQuaternion = new Quaternion();
            if (diagonal > 0f)
            {
                var num = (float)Math.Sqrt(diagonal + 1f);
                OldQuaternion.W = num * 0.5f;
                num = 0.5f / num;
                OldQuaternion.X = -(rightY - upX) * num;
                OldQuaternion.Y = (upZ - forwardY) * num;
                OldQuaternion.Z = -(forwardX - rightZ) * num;
                return OldQuaternion;
            }
            if ((rightX >= upY) && (rightX >= forwardZ))
            {
                var num7 = (float)Math.Sqrt(((1f + rightX) - upY) - forwardZ);
                var num4 = 0.5f / num7;
                OldQuaternion.X = -(rightZ + forwardX) * num4;
                OldQuaternion.Y = 0.5f * num7;
                OldQuaternion.Z = -(rightY + upX) * num4;
                OldQuaternion.W = (upZ - forwardY) * num4;
                return OldQuaternion;
            }
            if (upY > forwardZ)
            {
                var num6 = (float)Math.Sqrt(((1f + upY) - rightX) - forwardZ);
                var num3 = 0.5f / num6;
                OldQuaternion.X = -(forwardY + upZ) * num3;
                OldQuaternion.Y = (upX + rightY) * num3;
                OldQuaternion.Z = -0.5f * num6;
                OldQuaternion.W = (forwardX - rightZ) * num3;
                return OldQuaternion; 
            }
            var num5 = (float)Math.Sqrt(((1f + forwardZ) - rightX) - upY);
            var num2 = 0.5f / num5;
            OldQuaternion.X = -0.5f * num5;
            OldQuaternion.Y = (forwardX + rightZ) * num2;
            OldQuaternion.Z = -(forwardY + upZ) * num2;
            OldQuaternion.W = (rightY - upX) * num2;
            return OldQuaternion;
        }
        
        public static Vector3 ToEulerRad(this Quaternion q) => q.ToEulerDeg() * MathUtil.Deg2Rad;
        
        public static Vector3 ToEulerDeg(this Quaternion that)
        {
            float sqw = that.W * that.W;
            float sqx = that.X * that.X;
            float sqy = that.Y * that.Y;
            float sqz = that.Z * that.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = that.X * that.W - that.Y * that.Z;
            Vector3 v;

            if (test > 0.4995f * unit)
            { // singularity at north pole
                v.Y = 2f * (float)Math.Atan2(that.Y, that.X);
                v.X = MathUtil.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * MathUtil.Rad2Deg);
            }
            if (test < -0.4995f * unit)
            { // singularity at south pole
                v.Y = -2f * (float)Math.Atan2(that.Y, that.X);
                v.X = -MathUtil.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * MathUtil.Rad2Deg);
            }
            Quaternion q = new Quaternion(that.W, that.Z, that.X, that.Y);
            v.Y = (float)System.Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (q.Z * q.Z + q.W * q.W));     // Yaw
            v.X = (float)System.Math.Asin(2f * (q.X * q.Z - q.W * q.Y));                             // Pitch
            v.Z = (float)System.Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (q.Y * q.Y + q.Z * q.Z));      // Roll
            return NormalizeAngles(v * MathUtil.Rad2Deg);
        }
        
        private static Vector3 NormalizeAngles(Vector3 angles)
        {
            angles.X = NormalizeAngle(angles.X);
            angles.Y = NormalizeAngle(angles.Y);
            angles.Z = NormalizeAngle(angles.Z);
            return angles;
        }
        
        private static float NormalizeAngle(float angle)
        {
            while (angle > 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;
        }

    }
}