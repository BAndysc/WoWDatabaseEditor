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
    /// Represents a four dimensional mathematical OldQuaternion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OldQuaternion : IEquatable<OldQuaternion>, IFormattable
    {
        /// <summary>
        /// The size of the <see cref="OldQuaternion"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Utilities.SizeOf<OldQuaternion>();

        /// <summary>
        /// A <see cref="OldQuaternion"/> with all of its components set to zero.
        /// </summary>
        public static readonly OldQuaternion Zero = new OldQuaternion();

        /// <summary>
        /// A <see cref="OldQuaternion"/> with all of its components set to one.
        /// </summary>
        public static readonly OldQuaternion One = new OldQuaternion(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The identity <see cref="OldQuaternion"/> (0, 0, 0, 1).
        /// </summary>
        public static readonly OldQuaternion Identity = new OldQuaternion(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// The X component of the OldQuaternion.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y component of the OldQuaternion.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z component of the OldQuaternion.
        /// </summary>
        public float Z;

        /// <summary>
        /// The W component of the OldQuaternion.
        /// </summary>
        public float W;

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public OldQuaternion(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the components.</param>
        public OldQuaternion(OldVector4 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = value.W;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X, Y, and Z components.</param>
        /// <param name="w">Initial value for the W component of the OldQuaternion.</param>
        public OldQuaternion(OldVector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X and Y components.</param>
        /// <param name="z">Initial value for the Z component of the OldQuaternion.</param>
        /// <param name="w">Initial value for the W component of the OldQuaternion.</param>
        public OldQuaternion(OldVector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="x">Initial value for the X component of the OldQuaternion.</param>
        /// <param name="y">Initial value for the Y component of the OldQuaternion.</param>
        /// <param name="z">Initial value for the Z component of the OldQuaternion.</param>
        /// <param name="w">Initial value for the W component of the OldQuaternion.</param>
        public OldQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OldQuaternion"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X, Y, Z, and W components of the OldQuaternion. This must be an array with four elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public OldQuaternion(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be four and only four input values for OldQuaternion.");

            X = values[0];
            Y = values[1];
            Z = values[2];
            W = values[3];
        }

        /// <summary>
        /// Gets a value indicating whether this instance is equivalent to the identity OldQuaternion.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is an identity OldQuaternion; otherwise, <c>false</c>.
        /// </value>
        public bool IsIdentity
        {
            get { return this.Equals(Identity); }
        }

        /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get { return MathUtil.IsOne((X * X) + (Y * Y) + (Z * Z) + (W * W)); }
        }

        /// <summary>
        /// Gets the angle of the OldQuaternion.
        /// </summary>
        /// <value>The OldQuaternion's angle.</value>
        public float Angle
        {
            get
            {
                float length = (X * X) + (Y * Y) + (Z * Z);
                if (MathUtil.IsZero(length))
                    return 0.0f;

                return (float)(2.0 * Math.Acos(MathUtil.Clamp(W, -1f, 1f)));
            }
        }

        /// <summary>
        /// Gets the axis components of the OldQuaternion.
        /// </summary>
        /// <value>The axis components of the OldQuaternion.</value>
        public OldVector3 Axis
        {
            get
            {
                float length = (X * X) + (Y * Y) + (Z * Z);
                if (MathUtil.IsZero(length))
                    return OldVector3.UnitX;

                float inv = 1.0f / (float)Math.Sqrt(length);
                return new OldVector3(X * inv, Y * inv, Z * inv);
            }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X, Y, Z, or W component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component, 2 for the Z component, and 3 for the W component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    case 3: return W;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for OldQuaternion run from 0 to 3, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    case 3: W = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for OldQuaternion run from 0 to 3, inclusive.");
                }
            }
        }

        /// <summary>
        /// Conjugates the OldQuaternion.
        /// </summary>
        public void Conjugate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        /// <summary>
        /// Conjugates and renormalizes the OldQuaternion.
        /// </summary>
        public void Invert()
        {
            float lengthSq = LengthSquared();
            if (!MathUtil.IsZero(lengthSq))
            {
                lengthSq = 1.0f / lengthSq;

                X = -X * lengthSq;
                Y = -Y * lengthSq;
                Z = -Z * lengthSq;
                W = W * lengthSq;
            }
        }

        /// <summary>
        /// Calculates the length of the OldQuaternion.
        /// </summary>
        /// <returns>The length of the OldQuaternion.</returns>
        /// <remarks>
        /// <see cref="OldQuaternion.LengthSquared"/> may be preferred when only the relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public float Length()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        /// <summary>
        /// Calculates the squared length of the OldQuaternion.
        /// </summary>
        /// <returns>The squared length of the OldQuaternion.</returns>
        /// <remarks>
        /// This method may be preferred to <see cref="OldQuaternion.Length"/> when only a relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        /// <summary>
        /// Converts the OldQuaternion into a unit OldQuaternion.
        /// </summary>
        public void Normalize()
        {
            float length = Length();
            if (!MathUtil.IsZero(length))
            {
                float inverse = 1.0f / length;
                X *= inverse;
                Y *= inverse;
                Z *= inverse;
                W *= inverse;
            }
        }

        /// <summary>
        /// Creates an array containing the elements of the OldQuaternion.
        /// </summary>
        /// <returns>A four-element array containing the components of the OldQuaternion.</returns>
        public float[] ToArray()
        {
            return new float[] { X, Y, Z, W };
        }

        /// <summary>
        /// Adds two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to add.</param>
        /// <param name="right">The second OldQuaternion to add.</param>
        /// <param name="result">When the method completes, contains the sum of the two OldQuaternions.</param>
        public static void Add(ref OldQuaternion left, ref OldQuaternion right, out OldQuaternion result)
        {
            result.X = left.X + right.X;
            result.Y = left.Y + right.Y;
            result.Z = left.Z + right.Z;
            result.W = left.W + right.W;
        }

        /// <summary>
        /// Adds two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to add.</param>
        /// <param name="right">The second OldQuaternion to add.</param>
        /// <returns>The sum of the two OldQuaternions.</returns>
        public static OldQuaternion Add(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Subtracts two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to subtract.</param>
        /// <param name="right">The second OldQuaternion to subtract.</param>
        /// <param name="result">When the method completes, contains the difference of the two OldQuaternions.</param>
        public static void Subtract(ref OldQuaternion left, ref OldQuaternion right, out OldQuaternion result)
        {
            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;
            result.W = left.W - right.W;
        }

        /// <summary>
        /// Subtracts two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to subtract.</param>
        /// <param name="right">The second OldQuaternion to subtract.</param>
        /// <returns>The difference of the two OldQuaternions.</returns>
        public static OldQuaternion Subtract(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldQuaternion by the given value.
        /// </summary>
        /// <param name="value">The OldQuaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the OldQuaternion.</param>
        /// <param name="result">When the method completes, contains the scaled OldQuaternion.</param>
        public static void Multiply(ref OldQuaternion value, float scale, out OldQuaternion result)
        {
            result.X = value.X * scale;
            result.Y = value.Y * scale;
            result.Z = value.Z * scale;
            result.W = value.W * scale;
        }

        /// <summary>
        /// Scales a OldQuaternion by the given value.
        /// </summary>
        /// <param name="value">The OldQuaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the OldQuaternion.</param>
        /// <returns>The scaled OldQuaternion.</returns>
        public static OldQuaternion Multiply(OldQuaternion value, float scale)
        {
            OldQuaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Multiplies a OldQuaternion by another.
        /// </summary>
        /// <param name="left">The first OldQuaternion to multiply.</param>
        /// <param name="right">The second OldQuaternion to multiply.</param>
        /// <param name="result">When the method completes, contains the multiplied OldQuaternion.</param>
        public static void Multiply(ref OldQuaternion left, ref OldQuaternion right, out OldQuaternion result)
        {
            float lx = left.X;
            float ly = left.Y;
            float lz = left.Z;
            float lw = left.W;
            float rx = right.X;
            float ry = right.Y;
            float rz = right.Z;
            float rw = right.W;
            float a = (ly * rz - lz * ry);
            float b = (lz * rx - lx * rz);
            float c = (lx * ry - ly * rx);
            float d = (lx * rx + ly * ry + lz * rz);
            result.X = (lx * rw + rx * lw) + a;
            result.Y = (ly * rw + ry * lw) + b;
            result.Z = (lz * rw + rz * lw) + c;
            result.W = lw * rw - d;
        }

        /// <summary>
        /// Multiplies a OldQuaternion by another.
        /// </summary>
        /// <param name="left">The first OldQuaternion to multiply.</param>
        /// <param name="right">The second OldQuaternion to multiply.</param>
        /// <returns>The multiplied OldQuaternion.</returns>
        public static OldQuaternion Multiply(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Reverses the direction of a given OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to negate.</param>
        /// <param name="result">When the method completes, contains a OldQuaternion facing in the opposite direction.</param>
        public static void Negate(ref OldQuaternion value, out OldQuaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        /// <summary>
        /// Reverses the direction of a given OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to negate.</param>
        /// <returns>A OldQuaternion facing in the opposite direction.</returns>
        public static OldQuaternion Negate(OldQuaternion value)
        {
            OldQuaternion result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Returns a <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <param name="result">When the method completes, contains a new <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of the specified point.</param>
        public static void Barycentric(ref OldQuaternion value1, ref OldQuaternion value2, ref OldQuaternion value3, float amount1, float amount2, out OldQuaternion result)
        {
            OldQuaternion start, end;
            Slerp(ref value1, ref value2, amount1 + amount2, out start);
            Slerp(ref value1, ref value3, amount1 + amount2, out end);
            Slerp(ref start, ref end, amount2 / (amount1 + amount2), out result);
        }

        /// <summary>
        /// Returns a <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <returns>A new <see cref="OldQuaternion"/> containing the 4D Cartesian coordinates of the specified point.</returns>
        public static OldQuaternion Barycentric(OldQuaternion value1, OldQuaternion value2, OldQuaternion value3, float amount1, float amount2)
        {
            OldQuaternion result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        /// <summary>
        /// Conjugates a OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to conjugate.</param>
        /// <param name="result">When the method completes, contains the conjugated OldQuaternion.</param>
        public static void Conjugate(ref OldQuaternion value, out OldQuaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        /// <summary>
        /// Conjugates a OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to conjugate.</param>
        /// <returns>The conjugated OldQuaternion.</returns>
        public static OldQuaternion Conjugate(OldQuaternion value)
        {
            OldQuaternion result;
            Conjugate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Calculates the dot product of two OldQuaternions.
        /// </summary>
        /// <param name="left">First source OldQuaternion.</param>
        /// <param name="right">Second source OldQuaternion.</param>
        /// <param name="result">When the method completes, contains the dot product of the two OldQuaternions.</param>
        public static void Dot(ref OldQuaternion left, ref OldQuaternion right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        /// <summary>
        /// Calculates the dot product of two OldQuaternions.
        /// </summary>
        /// <param name="left">First source OldQuaternion.</param>
        /// <param name="right">Second source OldQuaternion.</param>
        /// <returns>The dot product of the two OldQuaternions.</returns>
        public static float Dot(OldQuaternion left, OldQuaternion right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        /// <summary>
        /// Exponentiates a OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to exponentiate.</param>
        /// <param name="result">When the method completes, contains the exponentiated OldQuaternion.</param>
        public static void Exponential(ref OldQuaternion value, out OldQuaternion result)
        {
            float angle = (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z));
            float sin = (float)Math.Sin(angle);

            if (!MathUtil.IsZero(sin))
            {
                float coeff = sin / angle;
                result.X = coeff * value.X;
                result.Y = coeff * value.Y;
                result.Z = coeff * value.Z;
            }
            else
            {
                result = value;
            }

            result.W = (float)Math.Cos(angle);
        }

        /// <summary>
        /// Exponentiates a OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to exponentiate.</param>
        /// <returns>The exponentiated OldQuaternion.</returns>
        public static OldQuaternion Exponential(OldQuaternion value)
        {
            OldQuaternion result;
            Exponential(ref value, out result);
            return result;
        }

        /// <summary>
        /// Conjugates and renormalizes the OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to conjugate and renormalize.</param>
        /// <param name="result">When the method completes, contains the conjugated and renormalized OldQuaternion.</param>
        public static void Invert(ref OldQuaternion value, out OldQuaternion result)
        {
            result = value;
            result.Invert();
        }

        /// <summary>
        /// Conjugates and renormalizes the OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to conjugate and renormalize.</param>
        /// <returns>The conjugated and renormalized OldQuaternion.</returns>
        public static OldQuaternion Invert(OldQuaternion value)
        {
            OldQuaternion result;
            Invert(ref value, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two OldQuaternions.
        /// </summary>
        /// <param name="start">Start OldQuaternion.</param>
        /// <param name="end">End OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two OldQuaternions.</param>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref OldQuaternion start, ref OldQuaternion end, float amount, out OldQuaternion result)
        {
            float inverse = 1.0f - amount;

            if (Dot(start, end) >= 0.0f)
            {
                result.X = (inverse * start.X) + (amount * end.X);
                result.Y = (inverse * start.Y) + (amount * end.Y);
                result.Z = (inverse * start.Z) + (amount * end.Z);
                result.W = (inverse * start.W) + (amount * end.W);
            }
            else
            {
                result.X = (inverse * start.X) - (amount * end.X);
                result.Y = (inverse * start.Y) - (amount * end.Y);
                result.Z = (inverse * start.Z) - (amount * end.Z);
                result.W = (inverse * start.W) - (amount * end.W);
            }

            result.Normalize();
        }

        /// <summary>
        /// Performs a linear interpolation between two OldQuaternion.
        /// </summary>
        /// <param name="start">Start OldQuaternion.</param>
        /// <param name="end">End OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two OldQuaternions.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static OldQuaternion Lerp(OldQuaternion start, OldQuaternion end, float amount)
        {
            OldQuaternion result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Calculates the natural logarithm of the specified OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion whose logarithm will be calculated.</param>
        /// <param name="result">When the method completes, contains the natural logarithm of the OldQuaternion.</param>
        public static void Logarithm(ref OldQuaternion value, out OldQuaternion result)
        {
            if (Math.Abs(value.W) < 1.0)
            {
                float angle = (float)Math.Acos(value.W);
                float sin = (float)Math.Sin(angle);

                if (!MathUtil.IsZero(sin))
                {
                    float coeff = angle / sin;
                    result.X = value.X * coeff;
                    result.Y = value.Y * coeff;
                    result.Z = value.Z * coeff;
                }
                else
                {
                    result = value;
                }
            }
            else
            {
                result = value;
            }

            result.W = 0.0f;
        }

        /// <summary>
        /// Calculates the natural logarithm of the specified OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion whose logarithm will be calculated.</param>
        /// <returns>The natural logarithm of the OldQuaternion.</returns>
        public static OldQuaternion Logarithm(OldQuaternion value)
        {
            OldQuaternion result;
            Logarithm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Converts the OldQuaternion into a unit OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to normalize.</param>
        /// <param name="result">When the method completes, contains the normalized OldQuaternion.</param>
        public static void Normalize(ref OldQuaternion value, out OldQuaternion result)
        {
            OldQuaternion temp = value;
            result = temp;
            result.Normalize();
        }

        /// <summary>
        /// Converts the OldQuaternion into a unit OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to normalize.</param>
        /// <returns>The normalized OldQuaternion.</returns>
        public static OldQuaternion Normalize(OldQuaternion value)
        {
            value.Normalize();
            return value;
        }

        /// <summary>
        /// Creates a OldQuaternion given a rotation and an axis.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation.</param>
        /// <param name="result">When the method completes, contains the newly created OldQuaternion.</param>
        public static void RotationAxis(ref OldVector3 axis, float angle, out OldQuaternion result)
        {
            OldVector3 normalized = OldVector3.Normalize(axis);

            float half = angle * 0.5f;
            float sin = (float)Math.Sin(half);
            float cos = (float)Math.Cos(half);

            result.X = normalized.X * sin;
            result.Y = normalized.Y * sin;
            result.Z = normalized.Z * sin;
            result.W = cos;
        }

        /// <summary>
        /// Creates a OldQuaternion given a rotation and an axis.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation.</param>
        /// <returns>The newly created OldQuaternion.</returns>
        public static OldQuaternion RotationAxis(OldVector3 axis, float angle)
        {
            OldQuaternion result;
            RotationAxis(ref axis, angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldQuaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created OldQuaternion.</param>
        public static void RotationMatrix(ref OldMatrix matrix, out OldQuaternion result)
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
        /// Creates a OldQuaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created OldQuaternion.</param>
        public static void RotationMatrix(ref Matrix3x3 matrix, out OldQuaternion result)
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
        /// Creates a left-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        // public static void LookAtLH(ref OldVector3 eye, ref OldVector3 target, ref OldVector3 up, out OldQuaternion result)
        // {
        //     Matrix3x3 matrix;
        //     Matrix3x3.LookAtLH(ref eye, ref target, ref up, out matrix);
        //     RotationMatrix(ref matrix, out result);
        // }

        /// <summary>
        /// Creates a left-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldQuaternion.</returns>
        // public static OldQuaternion LookAtLH(OldVector3 eye, OldVector3 target, OldVector3 up)
        // {
        //     OldQuaternion result;
        //     LookAtLH(ref eye, ref target, ref up, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The direction of looking.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        public static OldQuaternion LookRotation(OldVector3 forward, OldVector3 up)
        {
            if (OldVector3.Dot(forward, up) > 0.9999f)
                return FromEuler(270, 0, 0);
            if (OldVector3.Dot(forward, up) < -0.9999f)
                return FromEuler(90, 0, 0);
            forward = OldVector3.Normalize(forward);
            OldVector3 right = OldVector3.Normalize(OldVector3.Cross(forward, up));
            up = OldVector3.Cross(right, forward);
            
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
            var OldQuaternion = new OldQuaternion();
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

        /// <summary>
        /// Creates a left-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        // public static void RotationLookAtLH(ref OldVector3 forward, ref OldVector3 up, out OldQuaternion result)
        // {
        //     OldVector3 eye = OldVector3.Zero;
        //     OldQuaternion.LookAtLH(ref eye, ref forward, ref up, out result);
        // }

        /// <summary>
        /// Creates a left-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldQuaternion.</returns>
        // public static OldQuaternion RotationLookAtLH(OldVector3 forward, OldVector3 up)
        // {
        //     OldQuaternion result;
        //     RotationLookAtLH(ref forward, ref up, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        // public static void LookAtRH(ref OldVector3 eye, ref OldVector3 target, ref OldVector3 up, out OldQuaternion result)
        // {
        //     Matrix3x3 matrix;
        //     Matrix3x3.LookAtRH(ref eye, ref target, ref up, out matrix);
        //     RotationMatrix(ref matrix, out result);
        // }

        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldQuaternion.</returns>
        // public static OldQuaternion LookAtRH(OldVector3 eye, OldVector3 target, OldVector3 up)
        // {
        //     OldQuaternion result;
        //     LookAtRH(ref eye, ref target, ref up, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at OldQuaternion.</param>
        // public static void RotationLookAtRH(ref OldVector3 forward, ref OldVector3 up, out OldQuaternion result)
        // {
        //     OldVector3 eye = OldVector3.Zero;
        //     OldQuaternion.LookAtRH(ref eye, ref forward, ref up, out result);
        // }

        /// <summary>
        /// Creates a right-handed, look-at OldQuaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at OldQuaternion.</returns>
        // public static OldQuaternion RotationLookAtRH(OldVector3 forward, OldVector3 up)
        // {
        //     OldQuaternion result;
        //     RotationLookAtRH(ref forward, ref up, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a left-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="result">When the method completes, contains the created billboard OldQuaternion.</param>
        // public static void BillboardLH(ref OldVector3 objectPosition, ref OldVector3 cameraPosition, ref OldVector3 cameraUpVector, ref OldVector3 cameraForwardVector, out OldQuaternion result)
        // {
        //     Matrix3x3 matrix;
        //     Matrix3x3.BillboardLH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out matrix);
        //     RotationMatrix(ref matrix, out result);
        // }

        /// <summary>
        /// Creates a left-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard OldQuaternion.</returns>
        // public static OldQuaternion BillboardLH(OldVector3 objectPosition, OldVector3 cameraPosition, OldVector3 cameraUpVector, OldVector3 cameraForwardVector)
        // {
        //     OldQuaternion result;
        //     BillboardLH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a right-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="result">When the method completes, contains the created billboard OldQuaternion.</param>
        // public static void BillboardRH(ref OldVector3 objectPosition, ref OldVector3 cameraPosition, ref OldVector3 cameraUpVector, ref OldVector3 cameraForwardVector, out OldQuaternion result)
        // {
        //     Matrix3x3 matrix;
        //     Matrix3x3.BillboardRH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out matrix);
        //     RotationMatrix(ref matrix, out result);
        // }

        /// <summary>
        /// Creates a right-handed spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard OldQuaternion.</returns>
        // public static OldQuaternion BillboardRH(OldVector3 objectPosition, OldVector3 cameraPosition, OldVector3 cameraUpVector, OldVector3 cameraForwardVector)
        // {
        //     OldQuaternion result;
        //     BillboardRH(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out result);
        //     return result;
        // }

        /// <summary>
        /// Creates a OldQuaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <returns>The newly created OldQuaternion.</returns>
        public static OldQuaternion RotationMatrix(OldMatrix matrix)
        {
            OldQuaternion result;
            RotationMatrix(ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Creates a OldQuaternion given a yaw, pitch, and roll value.
        /// </summary>
        /// <param name="yaw">The yaw of rotation.</param>
        /// <param name="pitch">The pitch of rotation.</param>
        /// <param name="roll">The roll of rotation.</param>
        /// <param name="result">When the method completes, contains the newly created OldQuaternion.</param>
        public static void RotationYawPitchRoll(float yaw, float pitch, float roll, out OldQuaternion result)
        {
            float halfRoll = roll * 0.5f;
            float halfPitch = pitch * 0.5f;
            float halfYaw = yaw * 0.5f;

            float sinRoll = (float)Math.Sin(halfRoll);
            float cosRoll = (float)Math.Cos(halfRoll);
            float sinPitch = (float)Math.Sin(halfPitch);
            float cosPitch = (float)Math.Cos(halfPitch);
            float sinYaw = (float)Math.Sin(halfYaw);
            float cosYaw = (float)Math.Cos(halfYaw);

            result.X = (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll);
            result.Y = (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll);
            result.Z = (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll);
            result.W = (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll);
        }

        /// <summary>
        /// Creates a OldQuaternion given a yaw, pitch, and roll value.
        /// </summary>
        /// <param name="yaw">The yaw of rotation.</param>
        /// <param name="pitch">The pitch of rotation.</param>
        /// <param name="roll">The roll of rotation.</param>
        /// <returns>The newly created OldQuaternion.</returns>
        public static OldQuaternion RotationYawPitchRoll(float yaw, float pitch, float roll)
        {
            OldQuaternion result;
            RotationYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        /// <summary>
        /// Interpolates between two OldQuaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="start">Start OldQuaternion.</param>
        /// <param name="end">End OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the spherical linear interpolation of the two OldQuaternions.</param>
        public static void Slerp(ref OldQuaternion start, ref OldQuaternion end, float amount, out OldQuaternion result)
        {
            float opposite;
            float inverse;
            float dot = Dot(start, end);

            if (Math.Abs(dot) > 1.0f - MathUtil.ZeroTolerance)
            {
                inverse = 1.0f - amount;
                opposite = amount * Math.Sign(dot);
            }
            else
            {
                float acos = (float)Math.Acos(Math.Abs(dot));
                float invSin = (float)(1.0 / Math.Sin(acos));

                inverse = (float)Math.Sin((1.0f - amount) * acos) * invSin;
                opposite = (float)Math.Sin(amount * acos) * invSin * Math.Sign(dot);
            }

            result.X = (inverse * start.X) + (opposite * end.X);
            result.Y = (inverse * start.Y) + (opposite * end.Y);
            result.Z = (inverse * start.Z) + (opposite * end.Z);
            result.W = (inverse * start.W) + (opposite * end.W);
        }

        /// <summary>
        /// Interpolates between two OldQuaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="start">Start OldQuaternion.</param>
        /// <param name="end">End OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The spherical linear interpolation of the two OldQuaternions.</returns>
        public static OldQuaternion Slerp(OldQuaternion start, OldQuaternion end, float amount)
        {
            OldQuaternion result;
            Slerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Interpolates between OldQuaternions, using spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source OldQuaternion.</param>
        /// <param name="value2">Second source OldQuaternion.</param>
        /// <param name="value3">Third source OldQuaternion.</param>
        /// <param name="value4">Fourth source OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of interpolation.</param>
        /// <param name="result">When the method completes, contains the spherical quadrangle interpolation of the OldQuaternions.</param>
        public static void Squad(ref OldQuaternion value1, ref OldQuaternion value2, ref OldQuaternion value3, ref OldQuaternion value4, float amount, out OldQuaternion result)
        {
            OldQuaternion start, end;
            Slerp(ref value1, ref value4, amount, out start);
            Slerp(ref value2, ref value3, amount, out end);
            Slerp(ref start, ref end, 2.0f * amount * (1.0f - amount), out result);
        }

        /// <summary>
        /// Interpolates between OldQuaternions, using spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source OldQuaternion.</param>
        /// <param name="value2">Second source OldQuaternion.</param>
        /// <param name="value3">Third source OldQuaternion.</param>
        /// <param name="value4">Fourth source OldQuaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of interpolation.</param>
        /// <returns>The spherical quadrangle interpolation of the OldQuaternions.</returns>
        public static OldQuaternion Squad(OldQuaternion value1, OldQuaternion value2, OldQuaternion value3, OldQuaternion value4, float amount)
        {
            OldQuaternion result;
            Squad(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
        }

        /// <summary>
        /// Sets up control points for spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source OldQuaternion.</param>
        /// <param name="value2">Second source OldQuaternion.</param>
        /// <param name="value3">Third source OldQuaternion.</param>
        /// <param name="value4">Fourth source OldQuaternion.</param>
        /// <returns>An array of three OldQuaternions that represent control points for spherical quadrangle interpolation.</returns>
        public static OldQuaternion[] SquadSetup(OldQuaternion value1, OldQuaternion value2, OldQuaternion value3, OldQuaternion value4)
        {
            OldQuaternion q0 = (value1 + value2).LengthSquared() < (value1 - value2).LengthSquared() ? -value1 : value1;
            OldQuaternion q2 = (value2 + value3).LengthSquared() < (value2 - value3).LengthSquared() ? -value3 : value3;
            OldQuaternion q3 = (value3 + value4).LengthSquared() < (value3 - value4).LengthSquared() ? -value4 : value4;
            OldQuaternion q1 = value2;

            OldQuaternion q1Exp, q2Exp;
            Exponential(ref q1, out q1Exp);
            Exponential(ref q2, out q2Exp);

            OldQuaternion[] results = new OldQuaternion[3];
            results[0] = q1 * Exponential(-0.25f * (Logarithm(q1Exp * q2) + Logarithm(q1Exp * q0)));
            results[1] = q2 * Exponential(-0.25f * (Logarithm(q2Exp * q3) + Logarithm(q2Exp * q1)));
            results[2] = q2;

            return results;
        }

        /// <summary>
        /// Adds two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to add.</param>
        /// <param name="right">The second OldQuaternion to add.</param>
        /// <returns>The sum of the two OldQuaternions.</returns>
        public static OldQuaternion operator +(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Subtracts two OldQuaternions.
        /// </summary>
        /// <param name="left">The first OldQuaternion to subtract.</param>
        /// <param name="right">The second OldQuaternion to subtract.</param>
        /// <returns>The difference of the two OldQuaternions.</returns>
        public static OldQuaternion operator -(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Reverses the direction of a given OldQuaternion.
        /// </summary>
        /// <param name="value">The OldQuaternion to negate.</param>
        /// <returns>A OldQuaternion facing in the opposite direction.</returns>
        public static OldQuaternion operator -(OldQuaternion value)
        {
            OldQuaternion result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldQuaternion by the given value.
        /// </summary>
        /// <param name="value">The OldQuaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the OldQuaternion.</param>
        /// <returns>The scaled OldQuaternion.</returns>
        public static OldQuaternion operator *(float scale, OldQuaternion value)
        {
            OldQuaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Scales a OldQuaternion by the given value.
        /// </summary>
        /// <param name="value">The OldQuaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the OldQuaternion.</param>
        /// <returns>The scaled OldQuaternion.</returns>
        public static OldQuaternion operator *(OldQuaternion value, float scale)
        {
            OldQuaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Multiplies a OldQuaternion by another.
        /// </summary>
        /// <param name="left">The first OldQuaternion to multiply.</param>
        /// <param name="right">The second OldQuaternion to multiply.</param>
        /// <returns>The multiplied OldQuaternion.</returns>
        public static OldQuaternion operator *(OldQuaternion left, OldQuaternion right)
        {
            OldQuaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(OldQuaternion left, OldQuaternion right)
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
        public static bool operator !=(OldQuaternion left, OldQuaternion right)
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
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
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

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", X.ToString(format, CultureInfo.CurrentCulture),
                Y.ToString(format, CultureInfo.CurrentCulture), Z.ToString(format, CultureInfo.CurrentCulture), W.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
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

            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X.ToString(format, formatProvider),
                Y.ToString(format, formatProvider), Z.ToString(format, formatProvider), W.ToString(format, formatProvider));
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
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                hashCode = (hashCode * 397) ^ W.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="OldQuaternion"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="OldQuaternion"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="OldQuaternion"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref OldQuaternion other)
        {
            return MathUtil.NearEqual(other.X, X) && MathUtil.NearEqual(other.Y, Y) && MathUtil.NearEqual(other.Z, Z) && MathUtil.NearEqual(other.W, W);
        }

        /// <summary>
        /// Determines whether the specified <see cref="OldQuaternion"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="OldQuaternion"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="OldQuaternion"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(OldQuaternion other)
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
            if (!(value is OldQuaternion))
                return false;

            var strongValue = (OldQuaternion)value;
            return Equals(ref strongValue);
        }
        
        private static OldVector3 NormalizeAngles(OldVector3 angles)
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

        public OldVector3 ToEulerRad() => ToEulerDeg() * MathUtil.Deg2Rad;
        
        public OldVector3 ToEulerDeg()
        {
            float sqw = this.W * this.W;
            float sqx = this.X * this.X;
            float sqy = this.Y * this.Y;
            float sqz = this.Z * this.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = X * W - Y * Z;
            OldVector3 v;

            if (test > 0.4995f * unit)
            { // singularity at north pole
                v.Y = 2f * (float)Math.Atan2(Y, X);
                v.X = MathUtil.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * MathUtil.Rad2Deg);
            }
            if (test < -0.4995f * unit)
            { // singularity at south pole
                v.Y = -2f * (float)Math.Atan2(Y, X);
                v.X = -MathUtil.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * MathUtil.Rad2Deg);
            }
            OldQuaternion q = new OldQuaternion(W, Z, X, Y);
            v.Y = (float)System.Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (q.Z * q.Z + q.W * q.W));     // Yaw
            v.X = (float)System.Math.Asin(2f * (q.X * q.Z - q.W * q.Y));                             // Pitch
            v.Z = (float)System.Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (q.Y * q.Y + q.Z * q.Z));      // Roll
            return NormalizeAngles(v * MathUtil.Rad2Deg);
        }

        public static OldQuaternion FromEuler(double yaw, double roll, double pitch)
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

            OldQuaternion q = new OldQuaternion((float)(sr * cp * cy - cr * sp * sy),
                (float)(cr * sp * cy + sr * cp * sy),
                (float)(cr * cp * sy - sr * sp * cy),
                (float)(cr * cp * cy + sr * sp * sy));
            

            return q;
        }
        
        // /// <summary>
        // /// Performs an implicit conversion from <see cref="OldQuaternion"/> to <see cref="RawOldQuaternion"/>.
        // /// </summary>
        // /// <param name="value">The value.</param>
        // /// <returns>The result of the conversion.</returns>
        // public unsafe static implicit operator RawOldQuaternion(OldQuaternion value)
        // {
        //     return *(RawOldQuaternion*)&value;
        // }
        //
        // /// <summary>
        // /// Performs an implicit conversion from <see cref="RawOldQuaternion"/> to <see cref="OldQuaternion"/>.
        // /// </summary>
        // /// <param name="value">The value.</param>
        // /// <returns>The result of the conversion.</returns>
        // public unsafe static implicit operator OldQuaternion(RawOldQuaternion value)
        // {
        //     return *(OldQuaternion*)&value;
        // }
    }
}
