// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheMaths
{
    /// <summary>
    /// Represents a 4x4 mathematical OldMatrix.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OldMatrix : IEquatable<OldMatrix>, IFormattable
    {
        /// <summary>
        /// The size of the <see cref="OldMatrix"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = 4 * 4 * sizeof(float);

        /// <summary>
        /// A <see cref="OldMatrix"/> with all of its components set to zero.
        /// </summary>
        public static readonly OldMatrix Zero = new OldMatrix();

        /// <summary>
        /// The identity <see cref="OldMatrix"/>.
        /// </summary>
        public static readonly OldMatrix Identity = new OldMatrix() { M11 = 1.0f, M22 = 1.0f, M33 = 1.0f, M44 = 1.0f };

        /// <summary>
        /// Value at row 1 column 1 of the OldMatrix.
        /// </summary>
        public float M11;

        /// <summary>
        /// Value at row 1 column 2 of the OldMatrix.
        /// </summary>
        public float M12;

        /// <summary>
        /// Value at row 1 column 3 of the OldMatrix.
        /// </summary>
        public float M13;

        /// <summary>
        /// Value at row 1 column 4 of the OldMatrix.
        /// </summary>
        public float M14;

        /// <summary>
        /// Value at row 2 column 1 of the OldMatrix.
        /// </summary>
        public float M21;

        /// <summary>
        /// Value at row 2 column 2 of the OldMatrix.
        /// </summary>
        public float M22;

        /// <summary>
        /// Value at row 2 column 3 of the OldMatrix.
        /// </summary>
        public float M23;

        /// <summary>
        /// Value at row 2 column 4 of the OldMatrix.
        /// </summary>
        public float M24;

        /// <summary>
        /// Value at row 3 column 1 of the OldMatrix.
        /// </summary>
        public float M31;

        /// <summary>
        /// Value at row 3 column 2 of the OldMatrix.
        /// </summary>
        public float M32;

        /// <summary>
        /// Value at row 3 column 3 of the OldMatrix.
        /// </summary>
        public float M33;

        /// <summary>
        /// Value at row 3 column 4 of the OldMatrix.
        /// </summary>
        public float M34;

        /// <summary>
        /// Value at row 4 column 1 of the OldMatrix.
        /// </summary>
        public float M41;

        /// <summary>
        /// Value at row 4 column 2 of the OldMatrix.
        /// </summary>
        public float M42;

        /// <summary>
        /// Value at row 4 column 3 of the OldMatrix.
        /// </summary>
        public float M43;

        /// <summary>
        /// Value at row 4 column 4 of the OldMatrix.
        /// </summary>
        public float M44;
     
        /// <summary>
        /// Gets or sets the up <see cref="OldVector3"/> of the OldMatrix; that is M21, M22, and M23.
        /// </summary>
        public OldVector3 Up
        {
          get
          {
            OldVector3 vector3;
            vector3.X = this.M21;
            vector3.Y = this.M22;
            vector3.Z = this.M23;
            return vector3;
          }
          set
          {
            this.M21 = value.X;
            this.M22 = value.Y;
            this.M23 = value.Z;
          }
        }
    
        /// <summary>
        /// Gets or sets the down <see cref="OldVector3"/> of the OldMatrix; that is -M21, -M22, and -M23.
        /// </summary>
        public OldVector3 Down
        {
          get
          {
            OldVector3 vector3;
            vector3.X = -this.M21;
            vector3.Y = -this.M22;
            vector3.Z = -this.M23;
            return vector3;
          }
          set
          {
            this.M21 = -value.X;
            this.M22 = -value.Y;
            this.M23 = -value.Z;
          }
        }
    
        /// <summary>
        /// Gets or sets the right <see cref="OldVector3"/> of the OldMatrix; that is M11, M12, and M13.
        /// </summary>
        public OldVector3 Right
        {
          get
          {
            OldVector3 vector3;
            vector3.X = this.M11;
            vector3.Y = this.M12;
            vector3.Z = this.M13;
            return vector3;
          }
          set
          {
            this.M11 = value.X;
            this.M12 = value.Y;
            this.M13 = value.Z;
          }
        }
    
        /// <summary>
        /// Gets or sets the left <see cref="OldVector3"/> of the OldMatrix; that is -M11, -M12, and -M13.
        /// </summary>
        public OldVector3 Left
        {
          get
          {
            OldVector3 vector3;
            vector3.X = -this.M11;
            vector3.Y = -this.M12;
            vector3.Z = -this.M13;
            return vector3;
          }
          set
          {
            this.M11 = -value.X;
            this.M12 = -value.Y;
            this.M13 = -value.Z;
          }
        }
        
        /// <summary>
        /// Gets or sets the forward <see cref="OldVector3"/> of the OldMatrix; that is -M31, -M32, and -M33.
        /// </summary>
        public OldVector3 Forward
        {
          get
          {
            OldVector3 vector3;
            vector3.X = -this.M31;
            vector3.Y = -this.M32;
            vector3.Z = -this.M33;
            return vector3;
          }
          set
          {
            this.M31 = -value.X;
            this.M32 = -value.Y;
            this.M33 = -value.Z;
          }
        }
        
        /// <summary>
        /// Gets or sets the backward <see cref="OldVector3"/> of the OldMatrix; that is M31, M32, and M33.
        /// </summary>
        public OldVector3 Backward
        {
          get
          {
            OldVector3 vector3;
            vector3.X = this.M31;
            vector3.Y = this.M32;
            vector3.Z = this.M33;
            return vector3;
          }
          set
          {
            this.M31 = value.X;
            this.M32 = value.Y;
            this.M33 = value.Z;
          }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OldMatrix"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public OldMatrix(float value)
        {
            M11 = M12 = M13 = M14 =
            M21 = M22 = M23 = M24 =
            M31 = M32 = M33 = M34 =
            M41 = M42 = M43 = M44 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldMatrix"/> struct.
        /// </summary>
        /// <param name="M11">The value to assign at row 1 column 1 of the OldMatrix.</param>
        /// <param name="M12">The value to assign at row 1 column 2 of the OldMatrix.</param>
        /// <param name="M13">The value to assign at row 1 column 3 of the OldMatrix.</param>
        /// <param name="M14">The value to assign at row 1 column 4 of the OldMatrix.</param>
        /// <param name="M21">The value to assign at row 2 column 1 of the OldMatrix.</param>
        /// <param name="M22">The value to assign at row 2 column 2 of the OldMatrix.</param>
        /// <param name="M23">The value to assign at row 2 column 3 of the OldMatrix.</param>
        /// <param name="M24">The value to assign at row 2 column 4 of the OldMatrix.</param>
        /// <param name="M31">The value to assign at row 3 column 1 of the OldMatrix.</param>
        /// <param name="M32">The value to assign at row 3 column 2 of the OldMatrix.</param>
        /// <param name="M33">The value to assign at row 3 column 3 of the OldMatrix.</param>
        /// <param name="M34">The value to assign at row 3 column 4 of the OldMatrix.</param>
        /// <param name="M41">The value to assign at row 4 column 1 of the OldMatrix.</param>
        /// <param name="M42">The value to assign at row 4 column 2 of the OldMatrix.</param>
        /// <param name="M43">The value to assign at row 4 column 3 of the OldMatrix.</param>
        /// <param name="M44">The value to assign at row 4 column 4 of the OldMatrix.</param>
        public OldMatrix(float M11, float M12, float M13, float M14,
            float M21, float M22, float M23, float M24,
            float M31, float M32, float M33, float M34,
            float M41, float M42, float M43, float M44)
        {
            this.M11 = M11; this.M12 = M12; this.M13 = M13; this.M14 = M14;
            this.M21 = M21; this.M22 = M22; this.M23 = M23; this.M24 = M24;
            this.M31 = M31; this.M32 = M32; this.M33 = M33; this.M34 = M34;
            this.M41 = M41; this.M42 = M42; this.M43 = M43; this.M44 = M44;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldMatrix"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the components of the OldMatrix. This must be an array with sixteen elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than sixteen elements.</exception>
        public OldMatrix(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 16)
                throw new ArgumentOutOfRangeException("values", "There must be sixteen and only sixteen input values for OldMatrix.");

            M11 = values[0];
            M12 = values[1];
            M13 = values[2];
            M14 = values[3];

            M21 = values[4];
            M22 = values[5];
            M23 = values[6];
            M24 = values[7];

            M31 = values[8];
            M32 = values[9];
            M33 = values[10];
            M34 = values[11];

            M41 = values[12];
            M42 = values[13];
            M43 = values[14];
            M44 = values[15];
        }

        /// <summary>
        /// Gets or sets the first row in the OldMatrix; that is M11, M12, M13, and M14.
        /// </summary>
        public OldVector4 Row1
        {
            get { return new OldVector4(M11, M12, M13, M14); }
            set { M11 = value.X; M12 = value.Y; M13 = value.Z; M14 = value.W; }
        }

        /// <summary>
        /// Gets or sets the second row in the OldMatrix; that is M21, M22, M23, and M24.
        /// </summary>
        public OldVector4 Row2
        {
            get { return new OldVector4(M21, M22, M23, M24); }
            set { M21 = value.X; M22 = value.Y; M23 = value.Z; M24 = value.W; }
        }

        /// <summary>
        /// Gets or sets the third row in the OldMatrix; that is M31, M32, M33, and M34.
        /// </summary>
        public OldVector4 Row3
        {
            get { return new OldVector4(M31, M32, M33, M34); }
            set { M31 = value.X; M32 = value.Y; M33 = value.Z; M34 = value.W; }
        }

        /// <summary>
        /// Gets or sets the fourth row in the OldMatrix; that is M41, M42, M43, and M44.
        /// </summary>
        public OldVector4 Row4
        {
            get { return new OldVector4(M41, M42, M43, M44); }
            set { M41 = value.X; M42 = value.Y; M43 = value.Z; M44 = value.W; }
        }

        /// <summary>
        /// Gets or sets the first column in the OldMatrix; that is M11, M21, M31, and M41.
        /// </summary>
        public OldVector4 Column1
        {
            get { return new OldVector4(M11, M21, M31, M41); }
            set { M11 = value.X; M21 = value.Y; M31 = value.Z; M41 = value.W; }
        }

        /// <summary>
        /// Gets or sets the second column in the OldMatrix; that is M12, M22, M32, and M42.
        /// </summary>
        public OldVector4 Column2
        {
            get { return new OldVector4(M12, M22, M32, M42); }
            set { M12 = value.X; M22 = value.Y; M32 = value.Z; M42 = value.W; }
        }

        /// <summary>
        /// Gets or sets the third column in the OldMatrix; that is M13, M23, M33, and M43.
        /// </summary>
        public OldVector4 Column3
        {
            get { return new OldVector4(M13, M23, M33, M43); }
            set { M13 = value.X; M23 = value.Y; M33 = value.Z; M43 = value.W; }
        }

        /// <summary>
        /// Gets or sets the fourth column in the OldMatrix; that is M14, M24, M34, and M44.
        /// </summary>
        public OldVector4 Column4
        {
            get { return new OldVector4(M14, M24, M34, M44); }
            set { M14 = value.X; M24 = value.Y; M34 = value.Z; M44 = value.W; }
        }

        /// <summary>
        /// Gets or sets the translation of the OldMatrix; that is M41, M42, and M43.
        /// </summary>
        public OldVector3 TranslationVector
        {
            get { return new OldVector3(M41, M42, M43); }
            set { M41 = value.X; M42 = value.Y; M43 = value.Z; }
        }

        /// <summary>
        /// Gets or sets the scale of the OldMatrix; that is M11, M22, and M33.
        /// </summary>
        public OldVector3 ScaleVector
        {
            get
            {
                return new OldVector3((float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13)),
                    (float)Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23)),
                    (float)Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33)));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is an identity OldMatrix.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is an identity OldMatrix; otherwise, <c>false</c>.
        /// </value>
        public bool IsIdentity
        {
            get { return this.Equals(Identity); }
        }

        public OldQuaternion Rotation
        {
            get
            {
                OldVector3 scale;
                //Scaling is the length of the rows.
                scale.X = (float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
                scale.Y = (float)Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23));
                scale.Z = (float)Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33));

                //If any of the scaling factors are zero, than the rotation OldMatrix can not exist.
                if (MathUtil.IsZero(scale.X) ||
                    MathUtil.IsZero(scale.Y) ||
                    MathUtil.IsZero(scale.Z))
                {
                    return OldQuaternion.Identity;
                }

                //The rotation is the left over OldMatrix after dividing out the scaling.
                OldMatrix rotationOldMatrix = new OldMatrix();
                rotationOldMatrix.M11 = M11 / scale.X;
                rotationOldMatrix.M12 = M12 / scale.X;
                rotationOldMatrix.M13 = M13 / scale.X;

                rotationOldMatrix.M21 = M21 / scale.Y;
                rotationOldMatrix.M22 = M22 / scale.Y;
                rotationOldMatrix.M23 = M23 / scale.Y;

                rotationOldMatrix.M31 = M31 / scale.Z;
                rotationOldMatrix.M32 = M32 / scale.Z;
                rotationOldMatrix.M33 = M33 / scale.Z;

                rotationOldMatrix.M44 = 1f;

                OldQuaternion.RotationMatrix(ref rotationOldMatrix, out var rotation);
                return rotation;
            }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the OldMatrix component, depending on the index.</value>
        /// <param name="index">The zero-based index of the component to access.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 15].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:  return M11;
                    case 1:  return M12;
                    case 2:  return M13;
                    case 3:  return M14;
                    case 4:  return M21;
                    case 5:  return M22;
                    case 6:  return M23;
                    case 7:  return M24;
                    case 8:  return M31;
                    case 9:  return M32;
                    case 10: return M33;
                    case 11: return M34;
                    case 12: return M41;
                    case 13: return M42;
                    case 14: return M43;
                    case 15: return M44;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for OldMatrix run from 0 to 15, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: M11 = value; break;
                    case 1: M12 = value; break;
                    case 2: M13 = value; break;
                    case 3: M14 = value; break;
                    case 4: M21 = value; break;
                    case 5: M22 = value; break;
                    case 6: M23 = value; break;
                    case 7: M24 = value; break;
                    case 8: M31 = value; break;
                    case 9: M32 = value; break;
                    case 10: M33 = value; break;
                    case 11: M34 = value; break;
                    case 12: M41 = value; break;
                    case 13: M42 = value; break;
                    case 14: M43 = value; break;
                    case 15: M44 = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for OldMatrix run from 0 to 15, inclusive.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the OldMatrix component, depending on the index.</value>
        /// <param name="row">The row of the OldMatrix to access.</param>
        /// <param name="column">The column of the OldMatrix to access.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="row"/> or <paramref name="column"/>is out of the range [0, 3].</exception>
        public float this[int row, int column]
        {
            get
            {
                if (row < 0 || row > 3)
                    throw new ArgumentOutOfRangeException("row", "Rows and columns for matrices run from 0 to 3, inclusive.");
                if (column < 0 || column > 3)
                    throw new ArgumentOutOfRangeException("column", "Rows and columns for matrices run from 0 to 3, inclusive.");

                return this[(row * 4) + column];
            }

            set
            {
                if (row < 0 || row > 3)
                    throw new ArgumentOutOfRangeException("row", "Rows and columns for matrices run from 0 to 3, inclusive.");
                if (column < 0 || column > 3)
                    throw new ArgumentOutOfRangeException("column", "Rows and columns for matrices run from 0 to 3, inclusive.");

                this[(row * 4) + column] = value;
            }
        }

        /// <summary>
        /// Calculates the determinant of the OldMatrix.
        /// </summary>
        /// <returns>The determinant of the OldMatrix.</returns>
        public float Determinant()
        {
            float temp1 = (M33 * M44) - (M34 * M43);
            float temp2 = (M32 * M44) - (M34 * M42);
            float temp3 = (M32 * M43) - (M33 * M42);
            float temp4 = (M31 * M44) - (M34 * M41);
            float temp5 = (M31 * M43) - (M33 * M41);
            float temp6 = (M31 * M42) - (M32 * M41);

            return ((((M11 * (((M22 * temp1) - (M23 * temp2)) + (M24 * temp3))) - (M12 * (((M21 * temp1) -
                (M23 * temp4)) + (M24 * temp5)))) + (M13 * (((M21 * temp2) - (M22 * temp4)) + (M24 * temp6)))) -
                (M14 * (((M21 * temp3) - (M22 * temp5)) + (M23 * temp6))));
        }

        /// <summary>
        /// Inverts the OldMatrix.
        /// </summary>
        public void Invert()
        {
            Invert(ref this, out this);
        }

        /// <summary>
        /// Transposes the OldMatrix.
        /// </summary>
        public void Transpose()
        {
            Transpose(ref this, out this);
        }

        /// <summary>
        /// Orthogonalizes the specified OldMatrix.
        /// </summary>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the OldMatrix will be orthogonal to any other given row in the
        /// OldMatrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public void Orthogonalize()
        {
            Orthogonalize(ref this, out this);
        }

        /// <summary>
        /// Orthonormalizes the specified OldMatrix.
        /// </summary>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public void Orthonormalize()
        {
            Orthonormalize(ref this, out this);
        }

        /// <summary>
        /// Decomposes a OldMatrix into an orthonormalized OldMatrix Q and a right triangular OldMatrix R.
        /// </summary>
        /// <param name="Q">When the method completes, contains the orthonormalized OldMatrix of the decomposition.</param>
        /// <param name="R">When the method completes, contains the right triangular OldMatrix of the decomposition.</param>
        public void DecomposeQR(out OldMatrix Q, out OldMatrix R)
        {
            OldMatrix temp = this;
            temp.Transpose();
            Orthonormalize(ref temp, out Q);
            Q.Transpose();

            R = new OldMatrix();
            R.M11 = OldVector4.Dot(Q.Column1, Column1);
            R.M12 = OldVector4.Dot(Q.Column1, Column2);
            R.M13 = OldVector4.Dot(Q.Column1, Column3);
            R.M14 = OldVector4.Dot(Q.Column1, Column4);

            R.M22 = OldVector4.Dot(Q.Column2, Column2);
            R.M23 = OldVector4.Dot(Q.Column2, Column3);
            R.M24 = OldVector4.Dot(Q.Column2, Column4);

            R.M33 = OldVector4.Dot(Q.Column3, Column3);
            R.M34 = OldVector4.Dot(Q.Column3, Column4);

            R.M44 = OldVector4.Dot(Q.Column4, Column4);
        }

        /// <summary>
        /// Decomposes a OldMatrix into a lower triangular OldMatrix L and an orthonormalized OldMatrix Q.
        /// </summary>
        /// <param name="L">When the method completes, contains the lower triangular OldMatrix of the decomposition.</param>
        /// <param name="Q">When the method completes, contains the orthonormalized OldMatrix of the decomposition.</param>
        public void DecomposeLQ(out OldMatrix L, out OldMatrix Q)
        {
            Orthonormalize(ref this, out Q);

            L = new OldMatrix();
            L.M11 = OldVector4.Dot(Q.Row1, Row1);
            
            L.M21 = OldVector4.Dot(Q.Row1, Row2);
            L.M22 = OldVector4.Dot(Q.Row2, Row2);
            
            L.M31 = OldVector4.Dot(Q.Row1, Row3);
            L.M32 = OldVector4.Dot(Q.Row2, Row3);
            L.M33 = OldVector4.Dot(Q.Row3, Row3);
            
            L.M41 = OldVector4.Dot(Q.Row1, Row4);
            L.M42 = OldVector4.Dot(Q.Row2, Row4);
            L.M43 = OldVector4.Dot(Q.Row3, Row4);
            L.M44 = OldVector4.Dot(Q.Row4, Row4);
        }

        /// <summary>
        /// Decomposes a OldMatrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed OldMatrix.</param>
        /// <param name="rotation">When the method completes, contains the rotation component of the decomposed OldMatrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed OldMatrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation OldMatrix only.
        /// </remarks>
        public bool Decompose(out OldVector3 scale, out OldQuaternion rotation, out OldVector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = this.M41;
            translation.Y = this.M42;
            translation.Z = this.M43;

            //Scaling is the length of the rows.
            scale.X = (float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
            scale.Y = (float)Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23));
            scale.Z = (float)Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33));

            //If any of the scaling factors are zero, than the rotation OldMatrix can not exist.
            if (MathUtil.IsZero(scale.X) ||
                MathUtil.IsZero(scale.Y) ||
                MathUtil.IsZero(scale.Z))
            {
                rotation = OldQuaternion.Identity;
                return false;
            }

            //The rotation is the left over OldMatrix after dividing out the scaling.
            OldMatrix rotationOldMatrix = new OldMatrix();
            rotationOldMatrix.M11 = M11 / scale.X;
            rotationOldMatrix.M12 = M12 / scale.X;
            rotationOldMatrix.M13 = M13 / scale.X;

            rotationOldMatrix.M21 = M21 / scale.Y;
            rotationOldMatrix.M22 = M22 / scale.Y;
            rotationOldMatrix.M23 = M23 / scale.Y;

            rotationOldMatrix.M31 = M31 / scale.Z;
            rotationOldMatrix.M32 = M32 / scale.Z;
            rotationOldMatrix.M33 = M33 / scale.Z;

            rotationOldMatrix.M44 = 1f;

            OldQuaternion.RotationMatrix(ref rotationOldMatrix, out rotation);
            return true;
        }

        /// <summary>
        /// Decomposes a uniform scale OldMatrix into a scale, rotation, and translation.
        /// A uniform scale OldMatrix has the same scale in every axis.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed OldMatrix.</param>
        /// <param name="rotation">When the method completes, contains the rotation component of the decomposed OldMatrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed OldMatrix.</param>
        /// <remarks>
        /// This method is designed to decompose only an SRT transformation OldMatrix that has the same scale in every axis.
        /// </remarks>
        public bool DecomposeUniformScale(out float scale, out OldQuaternion rotation, out OldVector3 translation)
        {
            //Get the translation.
            translation.X = this.M41;
            translation.Y = this.M42;
            translation.Z = this.M43;

            //Scaling is the length of the rows. ( just take one row since this is a uniform OldMatrix)
            scale = (float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
            var inv_scale = 1f / scale;

            //If any of the scaling factors are zero, then the rotation OldMatrix can not exist.
            if (Math.Abs(scale) < MathUtil.ZeroTolerance)
            {
                rotation = OldQuaternion.Identity;
                return false;
            }

            //The rotation is the left over OldMatrix after dividing out the scaling.
            OldMatrix rotationOldMatrix = new OldMatrix();
            rotationOldMatrix.M11 = M11 * inv_scale;
            rotationOldMatrix.M12 = M12 * inv_scale;
            rotationOldMatrix.M13 = M13 * inv_scale;

            rotationOldMatrix.M21 = M21 * inv_scale;
            rotationOldMatrix.M22 = M22 * inv_scale;
            rotationOldMatrix.M23 = M23 * inv_scale;

            rotationOldMatrix.M31 = M31 * inv_scale;
            rotationOldMatrix.M32 = M32 * inv_scale;
            rotationOldMatrix.M33 = M33 * inv_scale;

            rotationOldMatrix.M44 = 1f;

            OldQuaternion.RotationMatrix(ref rotationOldMatrix, out rotation);
            return true;
        }

        /// <summary>
        /// Exchanges two rows in the OldMatrix.
        /// </summary>
        /// <param name="firstRow">The first row to exchange. This is an index of the row starting at zero.</param>
        /// <param name="secondRow">The second row to exchange. This is an index of the row starting at zero.</param>
        public void ExchangeRows(int firstRow, int secondRow)
        {
            if (firstRow < 0)
                throw new ArgumentOutOfRangeException("firstRow", "The parameter firstRow must be greater than or equal to zero.");
            if (firstRow > 3)
                throw new ArgumentOutOfRangeException("firstRow", "The parameter firstRow must be less than or equal to three.");
            if (secondRow < 0)
                throw new ArgumentOutOfRangeException("secondRow", "The parameter secondRow must be greater than or equal to zero.");
            if (secondRow > 3)
                throw new ArgumentOutOfRangeException("secondRow", "The parameter secondRow must be less than or equal to three.");

            if (firstRow == secondRow)
                return;

            float temp0 = this[secondRow, 0];
            float temp1 = this[secondRow, 1];
            float temp2 = this[secondRow, 2];
            float temp3 = this[secondRow, 3];

            this[secondRow, 0] = this[firstRow, 0];
            this[secondRow, 1] = this[firstRow, 1];
            this[secondRow, 2] = this[firstRow, 2];
            this[secondRow, 3] = this[firstRow, 3];

            this[firstRow, 0] = temp0;
            this[firstRow, 1] = temp1;
            this[firstRow, 2] = temp2;
            this[firstRow, 3] = temp3;
        }

        /// <summary>
        /// Exchanges two columns in the OldMatrix.
        /// </summary>
        /// <param name="firstColumn">The first column to exchange. This is an index of the column starting at zero.</param>
        /// <param name="secondColumn">The second column to exchange. This is an index of the column starting at zero.</param>
        public void ExchangeColumns(int firstColumn, int secondColumn)
        {
            if (firstColumn < 0)
                throw new ArgumentOutOfRangeException("firstColumn", "The parameter firstColumn must be greater than or equal to zero.");
            if (firstColumn > 3)
                throw new ArgumentOutOfRangeException("firstColumn", "The parameter firstColumn must be less than or equal to three.");
            if (secondColumn < 0)
                throw new ArgumentOutOfRangeException("secondColumn", "The parameter secondColumn must be greater than or equal to zero.");
            if (secondColumn > 3)
                throw new ArgumentOutOfRangeException("secondColumn", "The parameter secondColumn must be less than or equal to three.");

            if (firstColumn == secondColumn)
                return;

            float temp0 = this[0, secondColumn];
            float temp1 = this[1, secondColumn];
            float temp2 = this[2, secondColumn];
            float temp3 = this[3, secondColumn];

            this[0, secondColumn] = this[0, firstColumn];
            this[1, secondColumn] = this[1, firstColumn];
            this[2, secondColumn] = this[2, firstColumn];
            this[3, secondColumn] = this[3, firstColumn];

            this[0, firstColumn] = temp0;
            this[1, firstColumn] = temp1;
            this[2, firstColumn] = temp2;
            this[3, firstColumn] = temp3;
        }

        /// <summary>
        /// Creates an array containing the elements of the OldMatrix.
        /// </summary>
        /// <returns>A sixteen-element array containing the components of the OldMatrix.</returns>
        public float[] ToArray()
        {
            return new[] { M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44 };
        }

        /// <summary>
        /// Determines the sum of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to add.</param>
        /// <param name="right">The second OldMatrix to add.</param>
        /// <param name="result">When the method completes, contains the sum of the two matrices.</param>
        public static void Add(ref OldMatrix left, ref OldMatrix right, out OldMatrix result)
        {
            result.M11 = left.M11 + right.M11;
            result.M12 = left.M12 + right.M12;
            result.M13 = left.M13 + right.M13;
            result.M14 = left.M14 + right.M14;
            result.M21 = left.M21 + right.M21;
            result.M22 = left.M22 + right.M22;
            result.M23 = left.M23 + right.M23;
            result.M24 = left.M24 + right.M24;
            result.M31 = left.M31 + right.M31;
            result.M32 = left.M32 + right.M32;
            result.M33 = left.M33 + right.M33;
            result.M34 = left.M34 + right.M34;
            result.M41 = left.M41 + right.M41;
            result.M42 = left.M42 + right.M42;
            result.M43 = left.M43 + right.M43;
            result.M44 = left.M44 + right.M44;
        }

        /// <summary>
        /// Determines the sum of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to add.</param>
        /// <param name="right">The second OldMatrix to add.</param>
        /// <returns>The sum of the two matrices.</returns>
        public static OldMatrix Add(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Determines the difference between two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to subtract.</param>
        /// <param name="right">The second OldMatrix to subtract.</param>
        /// <param name="result">When the method completes, contains the difference between the two matrices.</param>
        public static void Subtract(ref OldMatrix left, ref OldMatrix right, out OldMatrix result)
        {
            result.M11 = left.M11 - right.M11;
            result.M12 = left.M12 - right.M12;
            result.M13 = left.M13 - right.M13;
            result.M14 = left.M14 - right.M14;
            result.M21 = left.M21 - right.M21;
            result.M22 = left.M22 - right.M22;
            result.M23 = left.M23 - right.M23;
            result.M24 = left.M24 - right.M24;
            result.M31 = left.M31 - right.M31;
            result.M32 = left.M32 - right.M32;
            result.M33 = left.M33 - right.M33;
            result.M34 = left.M34 - right.M34;
            result.M41 = left.M41 - right.M41;
            result.M42 = left.M42 - right.M42;
            result.M43 = left.M43 - right.M43;
            result.M44 = left.M44 - right.M44;
        }

        /// <summary>
        /// Determines the difference between two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to subtract.</param>
        /// <param name="right">The second OldMatrix to subtract.</param>
        /// <returns>The difference between the two matrices.</returns>
        public static OldMatrix Subtract(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldMatrix by the given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled OldMatrix.</param>
        public static void Multiply(ref OldMatrix left, float right, out OldMatrix result)
        {
            result.M11 = left.M11 * right;
            result.M12 = left.M12 * right;
            result.M13 = left.M13 * right;
            result.M14 = left.M14 * right;
            result.M21 = left.M21 * right;
            result.M22 = left.M22 * right;
            result.M23 = left.M23 * right;
            result.M24 = left.M24 * right;
            result.M31 = left.M31 * right;
            result.M32 = left.M32 * right;
            result.M33 = left.M33 * right;
            result.M34 = left.M34 * right;
            result.M41 = left.M41 * right;
            result.M42 = left.M42 * right;
            result.M43 = left.M43 * right;
            result.M44 = left.M44 * right;
        }

        /// <summary>
        /// Scales a OldMatrix by the given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled OldMatrix.</returns>
        public static OldMatrix Multiply(OldMatrix left, float right)
        {
            OldMatrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to multiply.</param>
        /// <param name="right">The second OldMatrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void Multiply(ref OldMatrix left, ref OldMatrix right, out OldMatrix result)
        {
            OldMatrix temp = new OldMatrix();
            temp.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
            temp.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
            temp.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
            temp.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);
            temp.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
            temp.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
            temp.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
            temp.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);
            temp.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
            temp.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
            temp.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
            temp.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);
            temp.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
            temp.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
            temp.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
            temp.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);
            result = temp;
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to multiply.</param>
        /// <param name="right">The second OldMatrix to multiply.</param>
        /// <returns>The product of the two matrices.</returns>
        public static OldMatrix Multiply(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldMatrix by the given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled OldMatrix.</param>
        public static void Divide(ref OldMatrix left, float right, out OldMatrix result)
        {
            float inv = 1.0f / right;

            result.M11 = left.M11 * inv;
            result.M12 = left.M12 * inv;
            result.M13 = left.M13 * inv;
            result.M14 = left.M14 * inv;
            result.M21 = left.M21 * inv;
            result.M22 = left.M22 * inv;
            result.M23 = left.M23 * inv;
            result.M24 = left.M24 * inv;
            result.M31 = left.M31 * inv;
            result.M32 = left.M32 * inv;
            result.M33 = left.M33 * inv;
            result.M34 = left.M34 * inv;
            result.M41 = left.M41 * inv;
            result.M42 = left.M42 * inv;
            result.M43 = left.M43 * inv;
            result.M44 = left.M44 * inv;
        }

        /// <summary>
        /// Scales a OldMatrix by the given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled OldMatrix.</returns>
        public static OldMatrix Divide(OldMatrix left, float right)
        {
            OldMatrix result;
            Divide(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Determines the quotient of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to divide.</param>
        /// <param name="right">The second OldMatrix to divide.</param>
        /// <param name="result">When the method completes, contains the quotient of the two matrices.</param>
        public static void Divide(ref OldMatrix left, ref OldMatrix right, out OldMatrix result)
        {
            result.M11 = left.M11 / right.M11;
            result.M12 = left.M12 / right.M12;
            result.M13 = left.M13 / right.M13;
            result.M14 = left.M14 / right.M14;
            result.M21 = left.M21 / right.M21;
            result.M22 = left.M22 / right.M22;
            result.M23 = left.M23 / right.M23;
            result.M24 = left.M24 / right.M24;
            result.M31 = left.M31 / right.M31;
            result.M32 = left.M32 / right.M32;
            result.M33 = left.M33 / right.M33;
            result.M34 = left.M34 / right.M34;
            result.M41 = left.M41 / right.M41;
            result.M42 = left.M42 / right.M42;
            result.M43 = left.M43 / right.M43;
            result.M44 = left.M44 / right.M44;
        }

        /// <summary>
        /// Determines the quotient of two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to divide.</param>
        /// <param name="right">The second OldMatrix to divide.</param>
        /// <returns>The quotient of the two matrices.</returns>
        public static OldMatrix Divide(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Performs the exponential operation on a OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to perform the operation on.</param>
        /// <param name="exponent">The exponent to raise the OldMatrix to.</param>
        /// <param name="result">When the method completes, contains the exponential OldMatrix.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="exponent"/> is negative.</exception>
        public static void Exponent(ref OldMatrix value, int exponent, out OldMatrix result)
        {
            //Source: http://rosettacode.org
            //Reference: http://rosettacode.org/wiki/OldMatrix-exponentiation_operator

            if (exponent < 0)
                throw new ArgumentOutOfRangeException("exponent", "The exponent can not be negative.");

            if (exponent == 0)
            {
                result = OldMatrix.Identity;
                return;
            }

            if (exponent == 1)
            {
                result = value;
                return;
            }

            OldMatrix identity = OldMatrix.Identity;
            OldMatrix temp = value;

            while (true)
            {
                if ((exponent & 1) != 0)
                    identity = identity * temp;

                exponent /= 2;

                if (exponent > 0)
                    temp *= temp;
                else
                    break;
            }

            result = identity;
        }

        /// <summary>
        /// Performs the exponential operation on a OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to perform the operation on.</param>
        /// <param name="exponent">The exponent to raise the OldMatrix to.</param>
        /// <returns>The exponential OldMatrix.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="exponent"/> is negative.</exception>
        public static OldMatrix Exponent(OldMatrix value, int exponent)
        {
            OldMatrix result;
            Exponent(ref value, exponent, out result);
            return result;
        }

        /// <summary>
        /// Negates a OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to be negated.</param>
        /// <param name="result">When the method completes, contains the negated OldMatrix.</param>
        public static void Negate(ref OldMatrix value, out OldMatrix result)
        {
            result.M11 = -value.M11;
            result.M12 = -value.M12;
            result.M13 = -value.M13;
            result.M14 = -value.M14;
            result.M21 = -value.M21;
            result.M22 = -value.M22;
            result.M23 = -value.M23;
            result.M24 = -value.M24;
            result.M31 = -value.M31;
            result.M32 = -value.M32;
            result.M33 = -value.M33;
            result.M34 = -value.M34;
            result.M41 = -value.M41;
            result.M42 = -value.M42;
            result.M43 = -value.M43;
            result.M44 = -value.M44;
        }

        /// <summary>
        /// Negates a OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to be negated.</param>
        /// <returns>The negated OldMatrix.</returns>
        public static OldMatrix Negate(OldMatrix value)
        {
            OldMatrix result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start OldMatrix.</param>
        /// <param name="end">End OldMatrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two matrices.</param>
        /// <remarks>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref OldMatrix start, ref OldMatrix end, float amount, out OldMatrix result)
        {
            result.M11 = MathUtil.Lerp(start.M11, end.M11, amount);
            result.M12 = MathUtil.Lerp(start.M12, end.M12, amount);
            result.M13 = MathUtil.Lerp(start.M13, end.M13, amount);
            result.M14 = MathUtil.Lerp(start.M14, end.M14, amount);
            result.M21 = MathUtil.Lerp(start.M21, end.M21, amount);
            result.M22 = MathUtil.Lerp(start.M22, end.M22, amount);
            result.M23 = MathUtil.Lerp(start.M23, end.M23, amount);
            result.M24 = MathUtil.Lerp(start.M24, end.M24, amount);
            result.M31 = MathUtil.Lerp(start.M31, end.M31, amount);
            result.M32 = MathUtil.Lerp(start.M32, end.M32, amount);
            result.M33 = MathUtil.Lerp(start.M33, end.M33, amount);
            result.M34 = MathUtil.Lerp(start.M34, end.M34, amount);
            result.M41 = MathUtil.Lerp(start.M41, end.M41, amount);
            result.M42 = MathUtil.Lerp(start.M42, end.M42, amount);
            result.M43 = MathUtil.Lerp(start.M43, end.M43, amount);
            result.M44 = MathUtil.Lerp(start.M44, end.M44, amount);
        }

        /// <summary>
        /// Performs a linear interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start OldMatrix.</param>
        /// <param name="end">End OldMatrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two matrices.</returns>
        /// <remarks>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static OldMatrix Lerp(OldMatrix start, OldMatrix end, float amount)
        {
            OldMatrix result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs a cubic interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start OldMatrix.</param>
        /// <param name="end">End OldMatrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the cubic interpolation of the two matrices.</param>
        public static void SmoothStep(ref OldMatrix start, ref OldMatrix end, float amount, out OldMatrix result)
        {
            amount = MathUtil.SmoothStep(amount);
            Lerp(ref start, ref end, amount, out result);
        }

        /// <summary>
        /// Performs a cubic interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start OldMatrix.</param>
        /// <param name="end">End OldMatrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two matrices.</returns>
        public static OldMatrix SmoothStep(OldMatrix start, OldMatrix end, float amount)
        {
            OldMatrix result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Calculates the transpose of the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix whose transpose is to be calculated.</param>
        /// <param name="result">When the method completes, contains the transpose of the specified OldMatrix.</param>
        public static void Transpose(ref OldMatrix value, out OldMatrix result)
        {
            OldMatrix temp = new OldMatrix();
            temp.M11 = value.M11;
            temp.M12 = value.M21;
            temp.M13 = value.M31;
            temp.M14 = value.M41;
            temp.M21 = value.M12;
            temp.M22 = value.M22;
            temp.M23 = value.M32;
            temp.M24 = value.M42;
            temp.M31 = value.M13;
            temp.M32 = value.M23;
            temp.M33 = value.M33;
            temp.M34 = value.M43;
            temp.M41 = value.M14;
            temp.M42 = value.M24;
            temp.M43 = value.M34;
            temp.M44 = value.M44;

            result = temp;
        }

        /// <summary>
        /// Calculates the transpose of the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix whose transpose is to be calculated.</param>
        /// <param name="result">When the method completes, contains the transpose of the specified OldMatrix.</param>
        public static void TransposeByRef(ref OldMatrix value, ref OldMatrix result)
        {
            result.M11 = value.M11;
            result.M12 = value.M21;
            result.M13 = value.M31;
            result.M14 = value.M41;
            result.M21 = value.M12;
            result.M22 = value.M22;
            result.M23 = value.M32;
            result.M24 = value.M42;
            result.M31 = value.M13;
            result.M32 = value.M23;
            result.M33 = value.M33;
            result.M34 = value.M43;
            result.M41 = value.M14;
            result.M42 = value.M24;
            result.M43 = value.M34;
            result.M44 = value.M44;
        }

        /// <summary>
        /// Calculates the transpose of the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix whose transpose is to be calculated.</param>
        /// <returns>The transpose of the specified OldMatrix.</returns>
        public static OldMatrix Transpose(OldMatrix value)
        {
            OldMatrix result;
            Transpose(ref value, out result);
            return result;
        }

        /// <summary>
        /// Calculates the inverse of the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix whose inverse is to be calculated.</param>
        /// <param name="result">When the method completes, contains the inverse of the specified OldMatrix.</param>
        public static void Invert(ref OldMatrix value, out OldMatrix result)
        {
            float b0 = (value.M31 * value.M42) - (value.M32 * value.M41);
            float b1 = (value.M31 * value.M43) - (value.M33 * value.M41);
            float b2 = (value.M34 * value.M41) - (value.M31 * value.M44);
            float b3 = (value.M32 * value.M43) - (value.M33 * value.M42);
            float b4 = (value.M34 * value.M42) - (value.M32 * value.M44);
            float b5 = (value.M33 * value.M44) - (value.M34 * value.M43);

            float d11 = value.M22 * b5 + value.M23 * b4 + value.M24 * b3;
            float d12 = value.M21 * b5 + value.M23 * b2 + value.M24 * b1;
            float d13 = value.M21 * -b4 + value.M22 * b2 + value.M24 * b0;
            float d14 = value.M21 * b3 + value.M22 * -b1 + value.M23 * b0;

            float det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13 - value.M14 * d14;
            if (Math.Abs(det) == 0.0f)
            {
                result = OldMatrix.Zero;
                return;
            }

            det = 1f / det;

            float a0 = (value.M11 * value.M22) - (value.M12 * value.M21);
            float a1 = (value.M11 * value.M23) - (value.M13 * value.M21);
            float a2 = (value.M14 * value.M21) - (value.M11 * value.M24);
            float a3 = (value.M12 * value.M23) - (value.M13 * value.M22);
            float a4 = (value.M14 * value.M22) - (value.M12 * value.M24);
            float a5 = (value.M13 * value.M24) - (value.M14 * value.M23);

            float d21 = value.M12 * b5 + value.M13 * b4 + value.M14 * b3;
            float d22 = value.M11 * b5 + value.M13 * b2 + value.M14 * b1;
            float d23 = value.M11 * -b4 + value.M12 * b2 + value.M14 * b0;
            float d24 = value.M11 * b3 + value.M12 * -b1 + value.M13 * b0;

            float d31 = value.M42 * a5 + value.M43 * a4 + value.M44 * a3;
            float d32 = value.M41 * a5 + value.M43 * a2 + value.M44 * a1;
            float d33 = value.M41 * -a4 + value.M42 * a2 + value.M44 * a0;
            float d34 = value.M41 * a3 + value.M42 * -a1 + value.M43 * a0;

            float d41 = value.M32 * a5 + value.M33 * a4 + value.M34 * a3;
            float d42 = value.M31 * a5 + value.M33 * a2 + value.M34 * a1;
            float d43 = value.M31 * -a4 + value.M32 * a2 + value.M34 * a0;
            float d44 = value.M31 * a3 + value.M32 * -a1 + value.M33 * a0;

            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det; result.M14 = -d41 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det; result.M24 = +d42 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det; result.M34 = -d43 * det;
            result.M41 = -d14 * det; result.M42 = +d24 * det; result.M43 = -d34 * det; result.M44 = +d44 * det;
        }

        /// <summary>
        /// Calculates the inverse of the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix whose inverse is to be calculated.</param>
        /// <returns>The inverse of the specified OldMatrix.</returns>
        public static OldMatrix Invert(OldMatrix value)
        {
            value.Invert();
            return value;
        }

        /// <summary>
        /// Orthogonalizes the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to orthogonalize.</param>
        /// <param name="result">When the method completes, contains the orthogonalized OldMatrix.</param>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the OldMatrix will be orthogonal to any other given row in the
        /// OldMatrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static void Orthogonalize(ref OldMatrix value, out OldMatrix result)
        {
            //Uses the modified Gram-Schmidt process.
            //q1 = m1
            //q2 = m2 - ((q1 ⋅ m2) / (q1 ⋅ q1)) * q1
            //q3 = m3 - ((q1 ⋅ m3) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m3) / (q2 ⋅ q2)) * q2
            //q4 = m4 - ((q1 ⋅ m4) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m4) / (q2 ⋅ q2)) * q2 - ((q3 ⋅ m4) / (q3 ⋅ q3)) * q3

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            result = value;

            result.Row2 = result.Row2 - (OldVector4.Dot(result.Row1, result.Row2) / OldVector4.Dot(result.Row1, result.Row1)) * result.Row1;

            result.Row3 = result.Row3 - (OldVector4.Dot(result.Row1, result.Row3) / OldVector4.Dot(result.Row1, result.Row1)) * result.Row1;
            result.Row3 = result.Row3 - (OldVector4.Dot(result.Row2, result.Row3) / OldVector4.Dot(result.Row2, result.Row2)) * result.Row2;

            result.Row4 = result.Row4 - (OldVector4.Dot(result.Row1, result.Row4) / OldVector4.Dot(result.Row1, result.Row1)) * result.Row1;
            result.Row4 = result.Row4 - (OldVector4.Dot(result.Row2, result.Row4) / OldVector4.Dot(result.Row2, result.Row2)) * result.Row2;
            result.Row4 = result.Row4 - (OldVector4.Dot(result.Row3, result.Row4) / OldVector4.Dot(result.Row3, result.Row3)) * result.Row3;
        }

        /// <summary>
        /// Orthogonalizes the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to orthogonalize.</param>
        /// <returns>The orthogonalized OldMatrix.</returns>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the OldMatrix will be orthogonal to any other given row in the
        /// OldMatrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static OldMatrix Orthogonalize(OldMatrix value)
        {
            OldMatrix result;
            Orthogonalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Orthonormalizes the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to orthonormalize.</param>
        /// <param name="result">When the method completes, contains the orthonormalized OldMatrix.</param>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static void Orthonormalize(ref OldMatrix value, out OldMatrix result)
        {
            //Uses the modified Gram-Schmidt process.
            //Because we are making unit vectors, we can optimize the math for orthonormalization
            //and simplify the projection operation to remove the division.
            //q1 = m1 / |m1|
            //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|
            //q3 = (m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2) / |m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2|
            //q4 = (m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3) / |m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3|

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            result = value;

            result.Row1 = OldVector4.Normalize(result.Row1);

            result.Row2 = result.Row2 - OldVector4.Dot(result.Row1, result.Row2) * result.Row1;
            result.Row2 = OldVector4.Normalize(result.Row2);

            result.Row3 = result.Row3 - OldVector4.Dot(result.Row1, result.Row3) * result.Row1;
            result.Row3 = result.Row3 - OldVector4.Dot(result.Row2, result.Row3) * result.Row2;
            result.Row3 = OldVector4.Normalize(result.Row3);

            result.Row4 = result.Row4 - OldVector4.Dot(result.Row1, result.Row4) * result.Row1;
            result.Row4 = result.Row4 - OldVector4.Dot(result.Row2, result.Row4) * result.Row2;
            result.Row4 = result.Row4 - OldVector4.Dot(result.Row3, result.Row4) * result.Row3;
            result.Row4 = OldVector4.Normalize(result.Row4);
        }

        /// <summary>
        /// Orthonormalizes the specified OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to orthonormalize.</param>
        /// <returns>The orthonormalized OldMatrix.</returns>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting OldMatrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the OldMatrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static OldMatrix Orthonormalize(OldMatrix value)
        {
            OldMatrix result;
            Orthonormalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the OldMatrix into upper triangular form using elementary row operations.
        /// </summary>
        /// <param name="value">The OldMatrix to put into upper triangular form.</param>
        /// <param name="result">When the method completes, contains the upper triangular OldMatrix.</param>
        /// <remarks>
        /// If the OldMatrix is not invertible (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the OldMatrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static void UpperTriangularForm(ref OldMatrix value, out OldMatrix result)
        {
            //Adapted from the row echelon code.
            result = value;
            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (MathUtil.IsZero(result[i, lead]))
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }
        }

        /// <summary>
        /// Brings the OldMatrix into upper triangular form using elementary row operations.
        /// </summary>
        /// <param name="value">The OldMatrix to put into upper triangular form.</param>
        /// <returns>The upper triangular OldMatrix.</returns>
        /// <remarks>
        /// If the OldMatrix is not invertible (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the OldMatrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static OldMatrix UpperTriangularForm(OldMatrix value)
        {
            OldMatrix result;
            UpperTriangularForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the OldMatrix into lower triangular form using elementary row operations.
        /// </summary>
        /// <param name="value">The OldMatrix to put into lower triangular form.</param>
        /// <param name="result">When the method completes, contains the lower triangular OldMatrix.</param>
        /// <remarks>
        /// If the OldMatrix is not invertible (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the OldMatrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static void LowerTriangularForm(ref OldMatrix value, out OldMatrix result)
        {
            //Adapted from the row echelon code.
            OldMatrix temp = value;
            OldMatrix.Transpose(ref temp, out result);

            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (MathUtil.IsZero(result[i, lead]))
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }

            OldMatrix.Transpose(ref result, out result);
        }

        /// <summary>
        /// Brings the OldMatrix into lower triangular form using elementary row operations.
        /// </summary>
        /// <param name="value">The OldMatrix to put into lower triangular form.</param>
        /// <returns>The lower triangular OldMatrix.</returns>
        /// <remarks>
        /// If the OldMatrix is not invertible (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the OldMatrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static OldMatrix LowerTriangularForm(OldMatrix value)
        {
            OldMatrix result;
            LowerTriangularForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the OldMatrix into row echelon form using elementary row operations;
        /// </summary>
        /// <param name="value">The OldMatrix to put into row echelon form.</param>
        /// <param name="result">When the method completes, contains the row echelon form of the OldMatrix.</param>
        public static void RowEchelonForm(ref OldMatrix value, out OldMatrix result)
        {
            //Source: Wikipedia pseudo code
            //Reference: http://en.wikipedia.org/wiki/Row_echelon_form#Pseudocode

            result = value;
            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (MathUtil.IsZero(result[i, lead]))
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];
                result[r, 0] *= multiplier;
                result[r, 1] *= multiplier;
                result[r, 2] *= multiplier;
                result[r, 3] *= multiplier;

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * result[i, lead];
                        result[i, 1] -= result[r, 1] * result[i, lead];
                        result[i, 2] -= result[r, 2] * result[i, lead];
                        result[i, 3] -= result[r, 3] * result[i, lead];
                    }
                }

                lead++;
            }
        }

        /// <summary>
        /// Brings the OldMatrix into row echelon form using elementary row operations;
        /// </summary>
        /// <param name="value">The OldMatrix to put into row echelon form.</param>
        /// <returns>When the method completes, contains the row echelon form of the OldMatrix.</returns>
        public static OldMatrix RowEchelonForm(OldMatrix value)
        {
            OldMatrix result;
            RowEchelonForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the OldMatrix into reduced row echelon form using elementary row operations.
        /// </summary>
        /// <param name="value">The OldMatrix to put into reduced row echelon form.</param>
        /// <param name="augment">The fifth column of the OldMatrix.</param>
        /// <param name="result">When the method completes, contains the resultant OldMatrix after the operation.</param>
        /// <param name="augmentResult">When the method completes, contains the resultant fifth column of the OldMatrix.</param>
        /// <remarks>
        /// <para>The fifth column is often called the augmented part of the OldMatrix. This is because the fifth
        /// column is really just an extension of the OldMatrix so that there is a place to put all of the
        /// non-zero components after the operation is complete.</para>
        /// <para>Often times the resultant OldMatrix will the identity OldMatrix or a OldMatrix similar to the identity
        /// OldMatrix. Sometimes, however, that is not possible and numbers other than zero and one may appear.</para>
        /// <para>This method can be used to solve systems of linear equations. Upon completion of this method,
        /// the <paramref name="augmentResult"/> will contain the solution for the system. It is up to the user
        /// to analyze both the input and the result to determine if a solution really exists.</para>
        /// </remarks>
        public static void ReducedRowEchelonForm(ref OldMatrix value, ref OldVector4 augment, out OldMatrix result, out OldVector4 augmentResult)
        {
            //Source: http://rosettacode.org
            //Reference: http://rosettacode.org/wiki/Reduced_row_echelon_form

            float[,] OldMatrix = new float[4, 5];

            OldMatrix[0, 0] = value[0, 0];
            OldMatrix[0, 1] = value[0, 1];
            OldMatrix[0, 2] = value[0, 2];
            OldMatrix[0, 3] = value[0, 3];
            OldMatrix[0, 4] = augment[0];

            OldMatrix[1, 0] = value[1, 0];
            OldMatrix[1, 1] = value[1, 1];
            OldMatrix[1, 2] = value[1, 2];
            OldMatrix[1, 3] = value[1, 3];
            OldMatrix[1, 4] = augment[1];

            OldMatrix[2, 0] = value[2, 0];
            OldMatrix[2, 1] = value[2, 1];
            OldMatrix[2, 2] = value[2, 2];
            OldMatrix[2, 3] = value[2, 3];
            OldMatrix[2, 4] = augment[2];

            OldMatrix[3, 0] = value[3, 0];
            OldMatrix[3, 1] = value[3, 1];
            OldMatrix[3, 2] = value[3, 2];
            OldMatrix[3, 3] = value[3, 3];
            OldMatrix[3, 4] = augment[3];

            int lead = 0;
            int rowcount = 4;
            int columncount = 5;

            for (int r = 0; r < rowcount; r++)
            {
                if (columncount <= lead)
                    break;

                int i = r;

                while (OldMatrix[i, lead] == 0)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (columncount == lead)
                            break;
                    }
                }

                for (int j = 0; j < columncount; j++)
                {
                    float temp = OldMatrix[r, j];
                    OldMatrix[r, j] = OldMatrix[i, j];
                    OldMatrix[i, j] = temp;
                }

                float div = OldMatrix[r, lead];

                for (int j = 0; j < columncount; j++)
                {
                    OldMatrix[r, j] /= div;
                }

                for (int j = 0; j < rowcount; j++)
                {
                    if (j != r)
                    {
                        float sub = OldMatrix[j, lead];
                        for (int k = 0; k < columncount; k++) OldMatrix[j, k] -= (sub * OldMatrix[r, k]);
                    }
                }

                lead++;
            }

            result.M11 = OldMatrix[0, 0];
            result.M12 = OldMatrix[0, 1];
            result.M13 = OldMatrix[0, 2];
            result.M14 = OldMatrix[0, 3];

            result.M21 = OldMatrix[1, 0];
            result.M22 = OldMatrix[1, 1];
            result.M23 = OldMatrix[1, 2];
            result.M24 = OldMatrix[1, 3];

            result.M31 = OldMatrix[2, 0];
            result.M32 = OldMatrix[2, 1];
            result.M33 = OldMatrix[2, 2];
            result.M34 = OldMatrix[2, 3];

            result.M41 = OldMatrix[3, 0];
            result.M42 = OldMatrix[3, 1];
            result.M43 = OldMatrix[3, 2];
            result.M44 = OldMatrix[3, 3];

            augmentResult.X = OldMatrix[0, 4];
            augmentResult.Y = OldMatrix[1, 4];
            augmentResult.Z = OldMatrix[2, 4];
            augmentResult.W = OldMatrix[3, 4];
        }

        /// <summary>
        /// Creates a left-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="result">When the method completes, contains the created billboard OldMatrix.</param>
        public static void BillboardLH(ref OldVector3 objectPosition, ref OldVector3 cameraPosition, ref OldVector3 cameraUpVector, ref OldVector3 cameraForwardVector, out OldMatrix result)
        {
            OldVector3 crossed;
            OldVector3 final;
            OldVector3 difference = cameraPosition - objectPosition;

            float lengthSq = difference.LengthSquared();
            if (MathUtil.IsZero(lengthSq))
                difference = -cameraForwardVector;
            else
                difference *= (float)(1.0 / Math.Sqrt(lengthSq));

            OldVector3.Cross(ref cameraUpVector, ref difference, out crossed);
            crossed.Normalize();
            OldVector3.Cross(ref difference, ref crossed, out final);

            result.M11 = crossed.X;
            result.M12 = crossed.Y;
            result.M13 = crossed.Z;
            result.M14 = 0.0f;
            result.M21 = final.X;
            result.M22 = final.Y;
            result.M23 = final.Z;
            result.M24 = 0.0f;
            result.M31 = difference.X;
            result.M32 = difference.Y;
            result.M33 = difference.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Creates a left-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard OldMatrix.</returns>
        public static OldMatrix BillboardLH(OldVector3 objectPosition, OldVector3 cameraPosition, OldVector3 cameraUpVector, OldVector3 cameraForwardVector)
        {
            OldMatrix result;
            BillboardLH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="result">When the method completes, contains the created billboard OldMatrix.</param>
        public static void BillboardRH(ref OldVector3 objectPosition, ref OldVector3 cameraPosition, ref OldVector3 cameraUpVector, ref OldVector3 cameraForwardVector, out OldMatrix result)
        {
            OldVector3 crossed;
            OldVector3 final;
            OldVector3 difference = objectPosition - cameraPosition;

            float lengthSq = difference.LengthSquared();
            if (MathUtil.IsZero(lengthSq))
                difference = -cameraForwardVector;
            else
                difference *= (float)(1.0 / Math.Sqrt(lengthSq));

            OldVector3.Cross(ref cameraUpVector, ref difference, out crossed);
            crossed.Normalize();
            OldVector3.Cross(ref difference, ref crossed, out final);

            result.M11 = crossed.X;
            result.M12 = crossed.Y;
            result.M13 = crossed.Z;
            result.M14 = 0.0f;
            result.M21 = final.X;
            result.M22 = final.Y;
            result.M23 = final.Z;
            result.M24 = 0.0f;
            result.M31 = difference.X;
            result.M32 = difference.Y;
            result.M33 = difference.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Creates a right-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard OldMatrix.</returns>
        public static OldMatrix BillboardRH(OldVector3 objectPosition, OldVector3 cameraPosition, OldVector3 cameraUpVector, OldVector3 cameraForwardVector) {
            OldMatrix result;
            BillboardRH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, look-at OldMatrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldMatrix.</param>
        public static void LookAtLH(ref OldVector3 eye, ref OldVector3 target, ref OldVector3 up, out OldMatrix result)
        {
            OldVector3 xaxis, yaxis, zaxis;
            OldVector3.Subtract(ref target, ref eye, out zaxis); zaxis.Normalize();
            OldVector3.Cross(ref up, ref zaxis, out xaxis); xaxis.Normalize();
            OldVector3.Cross(ref zaxis, ref xaxis, out yaxis);

            result = OldMatrix.Identity;
            result.M11 = xaxis.X; result.M21 = xaxis.Y; result.M31 = xaxis.Z;
            result.M12 = yaxis.X; result.M22 = yaxis.Y; result.M32 = yaxis.Z;
            result.M13 = zaxis.X; result.M23 = zaxis.Y; result.M33 = zaxis.Z;

            OldVector3.Dot(ref xaxis, ref eye, out result.M41);
            OldVector3.Dot(ref yaxis, ref eye, out result.M42);
            OldVector3.Dot(ref zaxis, ref eye, out result.M43);

            result.M41 = -result.M41;
            result.M42 = -result.M42;
            result.M43 = -result.M43;
        }

        /// <summary>
        /// Creates a left-handed, look-at OldMatrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldMatrix.</returns>
        public static OldMatrix LookAtLH(OldVector3 eye, OldVector3 target, OldVector3 up)
        {
            OldMatrix result;
            LookAtLH(ref eye, ref target, ref up, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, look-at OldMatrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldMatrix.</param>
        public static void LookAtRH(ref OldVector3 eye, ref OldVector3 target, ref OldVector3 up, out OldMatrix result)
        {
            OldVector3 xaxis, yaxis, zaxis;
            OldVector3.Subtract(ref eye, ref target, out zaxis); zaxis.Normalize();
            OldVector3.Cross(ref up, ref zaxis, out xaxis); xaxis.Normalize();
            OldVector3.Cross(ref zaxis, ref xaxis, out yaxis);

            result = OldMatrix.Identity;
            result.M11 = xaxis.X; result.M21 = xaxis.Y; result.M31 = xaxis.Z;
            result.M12 = yaxis.X; result.M22 = yaxis.Y; result.M32 = yaxis.Z;
            result.M13 = zaxis.X; result.M23 = zaxis.Y; result.M33 = zaxis.Z;

            OldVector3.Dot(ref xaxis, ref eye, out result.M41);
            OldVector3.Dot(ref yaxis, ref eye, out result.M42);
            OldVector3.Dot(ref zaxis, ref eye, out result.M43);

            result.M41 = -result.M41;
            result.M42 = -result.M42;
            result.M43 = -result.M43;
        }

        /// <summary>
        /// Creates a right-handed, look-at OldMatrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldMatrix.</returns>
        public static OldMatrix LookAtRH(OldVector3 eye, OldVector3 target, OldVector3 up)
        {
            OldMatrix result;
            LookAtRH(ref eye, ref target, ref up, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, orthographic projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void OrthoLH(float width, float height, float znear, float zfar, out OldMatrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            OrthoOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a left-handed, orthographic projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix OrthoLH(float width, float height, float znear, float zfar)
        {
            OldMatrix result;
            OrthoLH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, orthographic projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void OrthoRH(float width, float height, float znear, float zfar, out OldMatrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            OrthoOffCenterRH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a right-handed, orthographic projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix OrthoRH(float width, float height, float znear, float zfar)
        {
            OldMatrix result;
            OrthoRH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, customized orthographic projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void OrthoOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar, out OldMatrix result)
        {
            float zRange = 1.0f / (zfar - znear);

            result = OldMatrix.Identity;
            result.M11 = 2.0f / (right - left);
            result.M22 = 2.0f / (top - bottom);
            result.M33 = zRange;
            result.M41 = (left + right) / (left - right);
            result.M42 = (top + bottom) / (bottom - top);
            result.M43 = -znear * zRange;
        }

        /// <summary>
        /// Creates a left-handed, customized orthographic projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix OrthoOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            OldMatrix result;
            OrthoOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, customized orthographic projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void OrthoOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar, out OldMatrix result)
        {
            OrthoOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            result.M33 *= -1.0f;
        }

        /// <summary>
        /// Creates a right-handed, customized orthographic projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix OrthoOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            OldMatrix result;
            OrthoOffCenterRH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, perspective projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveLH(float width, float height, float znear, float zfar, out OldMatrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            PerspectiveOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a left-handed, perspective projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveLH(float width, float height, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveLH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, perspective projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveRH(float width, float height, float znear, float zfar, out OldMatrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            PerspectiveOffCenterRH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a right-handed, perspective projection OldMatrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveRH(float width, float height, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveRH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, perspective projection OldMatrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveFovLH(float fov, float aspect, float znear, float zfar, out OldMatrix result)
        {
            float yScale = (float)(1.0f / Math.Tan(fov * 0.5f));
            float q = zfar / (zfar - znear);

            result = new OldMatrix();
            result.M11 = yScale / aspect;
            result.M22 = yScale;
            result.M33 = q;
            result.M34 = 1.0f;
            result.M43 = -q * znear;
        }

        /// <summary>
        /// Creates a left-handed, perspective projection OldMatrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveFovLH(float fov, float aspect, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveFovLH(fov, aspect, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, perspective projection OldMatrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveFovRH(float fov, float aspect, float znear, float zfar, out OldMatrix result)
        {
            float yScale = (float)(1.0f / Math.Tan(fov * 0.5f));
            float q = zfar / (znear - zfar);

            result = new OldMatrix();
            result.M11 = yScale / aspect;
            result.M22 = yScale;
            result.M33 = q;
            result.M34 = -1.0f;
            result.M43 = q * znear;
        }

        /// <summary>
        /// Creates a right-handed, perspective projection OldMatrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveFovRH(float fov, float aspect, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveFovRH(fov, aspect, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, customized perspective projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar, out OldMatrix result)
        {
            float zRange = zfar / (zfar - znear);

            result = new OldMatrix();
            result.M11 = 2.0f * znear / (right - left);
            result.M22 = 2.0f * znear / (top - bottom);
            result.M31 = (left + right) / (left - right);
            result.M32 = (top + bottom) / (bottom - top);
            result.M33 = zRange;
            result.M34 = 1.0f;
            result.M43 = -znear * zRange;
        }

        /// <summary>
        /// Creates a left-handed, customized perspective projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, customized perspective projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection OldMatrix.</param>
        public static void PerspectiveOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar, out OldMatrix result)
        {
            PerspectiveOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            result.M31 *= -1.0f;
            result.M32 *= -1.0f;
            result.M33 *= -1.0f;
            result.M34 *= -1.0f;
        }

        /// <summary>
        /// Creates a right-handed, customized perspective projection OldMatrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection OldMatrix.</returns>
        public static OldMatrix PerspectiveOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            OldMatrix result;
            PerspectiveOffCenterRH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }


        /// <summary>
        /// Creates a OldMatrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="scale">Scaling factor for all three axes.</param>
        /// <param name="result">When the method completes, contains the created scaling OldMatrix.</param>
        public static void Scaling(in OldVector3 scale, out OldMatrix result)
        {
            Scaling(scale.X, scale.Y, scale.Z, out result);
        }

        /// <summary>
        /// Creates a OldMatrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="scale">Scaling factor for all three axes.</param>
        /// <returns>The created scaling OldMatrix.</returns>
        public static OldMatrix Scaling(in OldVector3 scale)
        {
            OldMatrix result;
            Scaling(in scale, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="x">Scaling factor that is applied along the x-axis.</param>
        /// <param name="y">Scaling factor that is applied along the y-axis.</param>
        /// <param name="z">Scaling factor that is applied along the z-axis.</param>
        /// <param name="result">When the method completes, contains the created scaling OldMatrix.</param>
        public static void Scaling(float x, float y, float z, out OldMatrix result)
        {
            result = OldMatrix.Identity;
            result.M11 = x;
            result.M22 = y;
            result.M33 = z;
        }

        /// <summary>
        /// Creates a OldMatrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="x">Scaling factor that is applied along the x-axis.</param>
        /// <param name="y">Scaling factor that is applied along the y-axis.</param>
        /// <param name="z">Scaling factor that is applied along the z-axis.</param>
        /// <returns>The created scaling OldMatrix.</returns>
        public static OldMatrix Scaling(float x, float y, float z)
        {
            OldMatrix result;
            Scaling(x, y, z, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that uniformly scales along all three axis.
        /// </summary>
        /// <param name="scale">The uniform scale that is applied along all axis.</param>
        /// <param name="result">When the method completes, contains the created scaling OldMatrix.</param>
        public static void Scaling(float scale, out OldMatrix result)
        {
            result = OldMatrix.Identity;
            result.M11 = result.M22 = result.M33 = scale;
        }

        /// <summary>
        /// Creates a OldMatrix that uniformly scales along all three axis.
        /// </summary>
        /// <param name="scale">The uniform scale that is applied along all axis.</param>
        /// <returns>The created scaling OldMatrix.</returns>
        public static OldMatrix Scaling(float scale)
        {
            OldMatrix result;
            Scaling(scale, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation OldMatrix.</param>
        public static void RotationX(float angle, out OldMatrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = OldMatrix.Identity;
            result.M22 = cos;
            result.M23 = sin;
            result.M32 = -sin;
            result.M33 = cos;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationX(float angle)
        {
            OldMatrix result;
            RotationX(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation OldMatrix.</param>
        public static void RotationY(float angle, out OldMatrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = OldMatrix.Identity;
            result.M11 = cos;
            result.M13 = -sin;
            result.M31 = sin;
            result.M33 = cos;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationY(float angle)
        {
            OldMatrix result;
            RotationY(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation OldMatrix.</param>
        public static void RotationZ(float angle, out OldMatrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = OldMatrix.Identity;
            result.M11 = cos;
            result.M12 = sin;
            result.M21 = -sin;
            result.M22 = cos;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationZ(float angle)
        {
            OldMatrix result;
            RotationZ(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around an arbitrary axis.
        /// </summary>
        /// <param name="axis">The axis around which to rotate. This parameter is assumed to be normalized.</param>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation OldMatrix.</param>
        public static void RotationAxis(ref OldVector3 axis, float angle, out OldMatrix result)
        {
            float x = axis.X;
            float y = axis.Y;
            float z = axis.Z;
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float xz = x * z;
            float yz = y * z;

            result = OldMatrix.Identity;
            result.M11 = xx + (cos * (1.0f - xx));
            result.M12 = (xy - (cos * xy)) + (sin * z);
            result.M13 = (xz - (cos * xz)) - (sin * y);
            result.M21 = (xy - (cos * xy)) - (sin * z);
            result.M22 = yy + (cos * (1.0f - yy));
            result.M23 = (yz - (cos * yz)) + (sin * x);
            result.M31 = (xz - (cos * xz)) + (sin * y);
            result.M32 = (yz - (cos * yz)) - (sin * x);
            result.M33 = zz + (cos * (1.0f - zz));
        }

        /// <summary>
        /// Creates a OldMatrix that rotates around an arbitrary axis.
        /// </summary>
        /// <param name="axis">The axis around which to rotate. This parameter is assumed to be normalized.</param>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationAxis(OldVector3 axis, float angle)
        {
            OldMatrix result;
            RotationAxis(ref axis, angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a rotation OldMatrix from a quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion to use to build the OldMatrix.</param>
        /// <param name="result">The created rotation OldMatrix.</param>
        public static void RotationQuaternion(in OldQuaternion rotation, out OldMatrix result)
        {
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            float xy = rotation.X * rotation.Y;
            float zw = rotation.Z * rotation.W;
            float zx = rotation.Z * rotation.X;
            float yw = rotation.Y * rotation.W;
            float yz = rotation.Y * rotation.Z;
            float xw = rotation.X * rotation.W;

            result = OldMatrix.Identity;
            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));
        }

        /// <summary>
        /// Creates a rotation OldMatrix from a quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion to use to build the OldMatrix.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationQuaternion(in OldQuaternion rotation)
        {
            OldMatrix result;
            RotationQuaternion(in rotation, out result);
            return result;
        }

        /// <summary>
        /// Creates a rotation OldMatrix with a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw around the y-axis, in radians.</param>
        /// <param name="pitch">Pitch around the x-axis, in radians.</param>
        /// <param name="roll">Roll around the z-axis, in radians.</param>
        /// <param name="result">When the method completes, contains the created rotation OldMatrix.</param>
        public static void RotationYawPitchRoll(float yaw, float pitch, float roll, out OldMatrix result)
        {
            OldQuaternion quaternion = new OldQuaternion();
            OldQuaternion.RotationYawPitchRoll(yaw, pitch, roll, out quaternion);
            RotationQuaternion(in quaternion, out result);
        }

        /// <summary>
        /// Creates a rotation OldMatrix with a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw around the y-axis, in radians.</param>
        /// <param name="pitch">Pitch around the x-axis, in radians.</param>
        /// <param name="roll">Roll around the z-axis, in radians.</param>
        /// <returns>The created rotation OldMatrix.</returns>
        public static OldMatrix RotationYawPitchRoll(float yaw, float pitch, float roll)
        {
            OldMatrix result;
            RotationYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        /// <summary>
        /// Creates a translation OldMatrix using the specified offsets.
        /// </summary>
        /// <param name="value">The offset for all three coordinate planes.</param>
        /// <param name="result">When the method completes, contains the created translation OldMatrix.</param>
        public static void Translation(in OldVector3 value, out OldMatrix result)
        {
            Translation(value.X, value.Y, value.Z, out result);
        }

        /// <summary>
        /// Creates a translation OldMatrix using the specified offsets.
        /// </summary>
        /// <param name="value">The offset for all three coordinate planes.</param>
        /// <returns>The created translation OldMatrix.</returns>
        public static OldMatrix Translation(in OldVector3 value)
        {
            OldMatrix result;
            Translation(in value, out result);
            return result;
        }

        /// <summary>
        /// Creates a translation OldMatrix using the specified offsets.
        /// </summary>
        /// <param name="x">X-coordinate offset.</param>
        /// <param name="y">Y-coordinate offset.</param>
        /// <param name="z">Z-coordinate offset.</param>
        /// <param name="result">When the method completes, contains the created translation OldMatrix.</param>
        public static void Translation(float x, float y, float z, out OldMatrix result)
        {
            result = OldMatrix.Identity;
            result.M41 = x;
            result.M42 = y;
            result.M43 = z;
        }

        /// <summary>
        /// Creates a translation OldMatrix using the specified offsets.
        /// </summary>
        /// <param name="x">X-coordinate offset.</param>
        /// <param name="y">Y-coordinate offset.</param>
        /// <param name="z">Z-coordinate offset.</param>
        /// <returns>The created translation OldMatrix.</returns>
        public static OldMatrix Translation(float x, float y, float z)
        {
            OldMatrix result;
            Translation(x, y, z, out result);
            return result;
        }

        /// <summary>
        /// Creates a skew/shear OldMatrix by means of a translation vector, a rotation vector, and a rotation angle.
        /// shearing is performed in the direction of translation vector, where translation vector and rotation vector define the shearing plane.
        /// The effect is such that the skewed rotation vector has the specified angle with rotation itself.
        /// </summary>
        /// <param name="angle">The rotation angle.</param>
        /// <param name="rotationVec">The rotation vector</param>
        /// <param name="transVec">The translation vector</param>
        /// <param name="OldMatrix">Contains the created skew/shear OldMatrix. </param>
        public static void Skew(float angle, ref OldVector3 rotationVec, ref OldVector3 transVec, out OldMatrix OldMatrix)
        {
            //http://elckerlyc.ewi.utwente.nl/browser/Elckerlyc/Hmi/HmiMath/src/hmi/math/Mat3f.java
            float MINIMAL_SKEW_ANGLE = 0.000001f;

            OldVector3 e0 = rotationVec;
            OldVector3 e1 = OldVector3.Normalize(transVec);

            float rv1;
            OldVector3.Dot(ref rotationVec, ref  e1, out rv1);
            e0 += rv1 * e1;
            float rv0;
            OldVector3.Dot(ref rotationVec, ref e0, out rv0);
            float cosa = (float)Math.Cos(angle);
            float sina = (float)Math.Sin(angle);
            float rr0 = rv0 * cosa - rv1 * sina;
            float rr1 = rv0 * sina + rv1 * cosa;

            if (rr0 < MINIMAL_SKEW_ANGLE)
                throw new ArgumentException("illegal skew angle");

            float d = (rr1 / rr0) - (rv1 / rv0);

            OldMatrix = OldMatrix.Identity;
            OldMatrix.M11 = d * e1[0] * e0[0] + 1.0f;
            OldMatrix.M12 = d * e1[0] * e0[1];
            OldMatrix.M13 = d * e1[0] * e0[2];
            OldMatrix.M21 = d * e1[1] * e0[0];
            OldMatrix.M22 = d * e1[1] * e0[1] + 1.0f;
            OldMatrix.M23 = d * e1[1] * e0[2];
            OldMatrix.M31 = d * e1[2] * e0[0];
            OldMatrix.M32 = d * e1[2] * e0[1];
            OldMatrix.M33 = d * e1[2] * e0[2] + 1.0f;
        }

        /// <summary>
        /// Creates a 3D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation OldMatrix.</param>
        public static void AffineTransformation(float scaling, ref OldQuaternion rotation, ref OldVector3 translation, out OldMatrix result)
        {
            result = Scaling(scaling) * RotationQuaternion(rotation) * Translation(translation);
        }

        /// <summary>
        /// Creates a 3D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation OldMatrix.</returns>
        public static OldMatrix AffineTransformation(float scaling, OldQuaternion rotation, OldVector3 translation)
        {
            OldMatrix result;
            AffineTransformation(scaling, ref rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 3D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation OldMatrix.</param>
        public static void AffineTransformation(float scaling, ref OldVector3 rotationCenter, ref OldQuaternion rotation, ref OldVector3 translation, out OldMatrix result)
        {
            result = Scaling(scaling) * Translation(-rotationCenter) * RotationQuaternion(rotation) *
                Translation(rotationCenter) * Translation(translation);
        }

        /// <summary>
        /// Creates a 3D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation OldMatrix.</returns>
        public static OldMatrix AffineTransformation(float scaling, OldVector3 rotationCenter, OldQuaternion rotation, OldVector3 translation)
        {
            OldMatrix result;
            AffineTransformation(scaling, ref rotationCenter, ref rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 2D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation OldMatrix.</param>
        public static void AffineTransformation2D(float scaling, float rotation, ref OldVector2 translation, out OldMatrix result)
        {
            result = Scaling(scaling, scaling, 1.0f) * RotationZ(rotation) * Translation((OldVector3)translation);
        }

        /// <summary>
        /// Creates a 2D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation OldMatrix.</returns>
        public static OldMatrix AffineTransformation2D(float scaling, float rotation, OldVector2 translation)
        {
            OldMatrix result;
            AffineTransformation2D(scaling, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 2D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation OldMatrix.</param>
        public static void AffineTransformation2D(float scaling, ref OldVector2 rotationCenter, float rotation, ref OldVector2 translation, out OldMatrix result)
        {
            result = Scaling(scaling, scaling, 1.0f) * Translation((OldVector3)(-rotationCenter)) * RotationZ(rotation) *
                Translation((OldVector3)rotationCenter) * Translation((OldVector3)translation);
        }

        /// <summary>
        /// Creates a 2D affine transformation OldMatrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation OldMatrix.</returns>
        public static OldMatrix AffineTransformation2D(float scaling, OldVector2 rotationCenter, float rotation, OldVector2 translation)
        {
            OldMatrix result;
            AffineTransformation2D(scaling, ref rotationCenter, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a transformation OldMatrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created transformation OldMatrix.</param>
        public static void Transformation(in OldVector3 scalingCenter, in OldQuaternion scalingRotation, in OldVector3 scaling, in OldVector3 rotationCenter, in OldQuaternion rotation, in OldVector3 translation, out OldMatrix result)
        {
            OldMatrix sr = RotationQuaternion(scalingRotation);

            result = Translation(-scalingCenter) * Transpose(sr) * Scaling(scaling) * sr * Translation(scalingCenter) * Translation(-rotationCenter) *
                RotationQuaternion(rotation) * Translation(rotationCenter) * Translation(translation);       
        }

        /// <summary>
        /// Creates a transformation OldMatrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation OldMatrix.</returns>
        public static OldMatrix Transformation(in OldVector3 scalingCenter, in OldQuaternion scalingRotation, in OldVector3 scaling, in OldVector3 rotationCenter, in OldQuaternion rotation, in OldVector3 translation)
        {
            OldMatrix result;
            Transformation(in scalingCenter, in scalingRotation, in scaling, in rotationCenter, in rotation, in translation, out result);
            return result;
        }

        public static OldMatrix TRS(in OldVector3 position, in OldQuaternion rotation, in OldVector3 scale)
        {
            return Transformation(in OldVector3.Zero, in OldQuaternion.Identity, in scale, in OldVector3.Zero, in rotation, in position);
        }

        /// <summary>
        /// Creates a 2D transformation OldMatrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created transformation OldMatrix.</param>
        public static void Transformation2D(ref OldVector2 scalingCenter, float scalingRotation, ref OldVector2 scaling, ref OldVector2 rotationCenter, float rotation, ref OldVector2 translation, out OldMatrix result)
        {
            result = Translation((OldVector3)(-scalingCenter)) * RotationZ(-scalingRotation) * Scaling((OldVector3)scaling) * RotationZ(scalingRotation) * Translation((OldVector3)scalingCenter) * 
                Translation((OldVector3)(-rotationCenter)) * RotationZ(rotation) * Translation((OldVector3)rotationCenter) * Translation((OldVector3)translation);

            result.M33 = 1f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a 2D transformation OldMatrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation OldMatrix.</returns>
        public static OldMatrix Transformation2D(OldVector2 scalingCenter, float scalingRotation, OldVector2 scaling, OldVector2 rotationCenter, float rotation, OldVector2 translation)
        {
            OldMatrix result;
            Transformation2D(ref scalingCenter, scalingRotation, ref scaling, ref rotationCenter, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to add.</param>
        /// <param name="right">The second OldMatrix to add.</param>
        /// <returns>The sum of the two matrices.</returns>
        public static OldMatrix operator +(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Assert a OldMatrix (return it unchanged).
        /// </summary>
        /// <param name="value">The OldMatrix to assert (unchanged).</param>
        /// <returns>The asserted (unchanged) OldMatrix.</returns>
        public static OldMatrix operator +(OldMatrix value)
        {
            return value;
        }

        /// <summary>
        /// Subtracts two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to subtract.</param>
        /// <param name="right">The second OldMatrix to subtract.</param>
        /// <returns>The difference between the two matrices.</returns>
        public static OldMatrix operator -(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Negates a OldMatrix.
        /// </summary>
        /// <param name="value">The OldMatrix to negate.</param>
        /// <returns>The negated OldMatrix.</returns>
        public static OldMatrix operator -(OldMatrix value)
        {
            OldMatrix result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldMatrix by a given value.
        /// </summary>
        /// <param name="right">The OldMatrix to scale.</param>
        /// <param name="left">The amount by which to scale.</param>
        /// <returns>The scaled OldMatrix.</returns>
        public static OldMatrix operator *(float left, OldMatrix right)
        {
            OldMatrix result;
            Multiply(ref right, left, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldMatrix by a given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled OldMatrix.</returns>
        public static OldMatrix operator *(OldMatrix left, float right)
        {
            OldMatrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to multiply.</param>
        /// <param name="right">The second OldMatrix to multiply.</param>
        /// <returns>The product of the two matrices.</returns>
        public static OldMatrix operator *(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldMatrix by a given value.
        /// </summary>
        /// <param name="left">The OldMatrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled OldMatrix.</returns>
        public static OldMatrix operator /(OldMatrix left, float right)
        {
            OldMatrix result;
            Divide(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Divides two matrices.
        /// </summary>
        /// <param name="left">The first OldMatrix to divide.</param>
        /// <param name="right">The second OldMatrix to divide.</param>
        /// <returns>The quotient of the two matrices.</returns>
        public static OldMatrix operator /(OldMatrix left, OldMatrix right)
        {
            OldMatrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(OldMatrix left, OldMatrix right)
        {
            return left.Equals(ref right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(OldMatrix left, OldMatrix right)
        {
            return !left.Equals(ref right);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(format, CultureInfo.CurrentCulture, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(format, CultureInfo.CurrentCulture), M12.ToString(format, CultureInfo.CurrentCulture), M13.ToString(format, CultureInfo.CurrentCulture), M14.ToString(format, CultureInfo.CurrentCulture),
                M21.ToString(format, CultureInfo.CurrentCulture), M22.ToString(format, CultureInfo.CurrentCulture), M23.ToString(format, CultureInfo.CurrentCulture), M24.ToString(format, CultureInfo.CurrentCulture),
                M31.ToString(format, CultureInfo.CurrentCulture), M32.ToString(format, CultureInfo.CurrentCulture), M33.ToString(format, CultureInfo.CurrentCulture), M34.ToString(format, CultureInfo.CurrentCulture),
                M41.ToString(format, CultureInfo.CurrentCulture), M42.ToString(format, CultureInfo.CurrentCulture), M43.ToString(format, CultureInfo.CurrentCulture), M44.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(formatProvider), M12.ToString(formatProvider), M13.ToString(formatProvider), M14.ToString(formatProvider),
                M21.ToString(formatProvider), M22.ToString(formatProvider), M23.ToString(formatProvider), M24.ToString(formatProvider),
                M31.ToString(formatProvider), M32.ToString(formatProvider), M33.ToString(formatProvider), M34.ToString(formatProvider),
                M41.ToString(formatProvider), M42.ToString(formatProvider), M43.ToString(formatProvider), M44.ToString(formatProvider));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(format, formatProvider), M12.ToString(format, formatProvider), M13.ToString(format, formatProvider), M14.ToString(format, formatProvider),
                M21.ToString(format, formatProvider), M22.ToString(format, formatProvider), M23.ToString(format, formatProvider), M24.ToString(format, formatProvider),
                M31.ToString(format, formatProvider), M32.ToString(format, formatProvider), M33.ToString(format, formatProvider), M34.ToString(format, formatProvider),
                M41.ToString(format, formatProvider), M42.ToString(format, formatProvider), M43.ToString(format, formatProvider), M44.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = M11.GetHashCode();
                hashCode = (hashCode * 397) ^ M12.GetHashCode();
                hashCode = (hashCode * 397) ^ M13.GetHashCode();
                hashCode = (hashCode * 397) ^ M14.GetHashCode();
                hashCode = (hashCode * 397) ^ M21.GetHashCode();
                hashCode = (hashCode * 397) ^ M22.GetHashCode();
                hashCode = (hashCode * 397) ^ M23.GetHashCode();
                hashCode = (hashCode * 397) ^ M24.GetHashCode();
                hashCode = (hashCode * 397) ^ M31.GetHashCode();
                hashCode = (hashCode * 397) ^ M32.GetHashCode();
                hashCode = (hashCode * 397) ^ M33.GetHashCode();
                hashCode = (hashCode * 397) ^ M34.GetHashCode();
                hashCode = (hashCode * 397) ^ M41.GetHashCode();
                hashCode = (hashCode * 397) ^ M42.GetHashCode();
                hashCode = (hashCode * 397) ^ M43.GetHashCode();
                hashCode = (hashCode * 397) ^ M44.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="OldMatrix"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="OldMatrix"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="OldMatrix"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ref OldMatrix other)
        {
            return (MathUtil.NearEqual(other.M11, M11) &&
                MathUtil.NearEqual(other.M12, M12) &&
                MathUtil.NearEqual(other.M13, M13) &&
                MathUtil.NearEqual(other.M14, M14) &&
                MathUtil.NearEqual(other.M21, M21) &&
                MathUtil.NearEqual(other.M22, M22) &&
                MathUtil.NearEqual(other.M23, M23) &&
                MathUtil.NearEqual(other.M24, M24) &&
                MathUtil.NearEqual(other.M31, M31) &&
                MathUtil.NearEqual(other.M32, M32) &&
                MathUtil.NearEqual(other.M33, M33) &&
                MathUtil.NearEqual(other.M34, M34) &&
                MathUtil.NearEqual(other.M41, M41) &&
                MathUtil.NearEqual(other.M42, M42) &&
                MathUtil.NearEqual(other.M43, M43) &&
                MathUtil.NearEqual(other.M44, M44));
        }

        /// <summary>
        /// Determines whether the specified <see cref="OldMatrix"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="OldMatrix"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="OldMatrix"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(OldMatrix other)
        {
            return Equals(ref other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (!(value is OldMatrix))
                return false;

            var strongValue = (OldMatrix)value;
            return Equals(ref strongValue);
        }

        // /// <summary>
        // /// Performs an implicit conversion from <see cref="OldMatrix"/> to <see cref="RawOldMatrix"/>.
        // /// </summary>
        // /// <param name="value">The value.</param>
        // /// <returns>The result of the conversion.</returns>
        // public unsafe static implicit operator RawOldMatrix(OldMatrix value)
        // {
        //     return *(RawOldMatrix*)&value;
        // }
        //
        // /// <summary>
        // /// Performs an implicit conversion from <see cref="RawOldMatrix"/> to <see cref="OldMatrix"/>.
        // /// </summary>
        // /// <param name="value">The value.</param>
        // /// <returns>The result of the conversion.</returns>
        // public unsafe static implicit operator OldMatrix(RawOldMatrix value)
        // {
        //     return *(OldMatrix*)&value;
        // }
    }
}
