using System;
using System.Diagnostics;
using NUnit.Framework;
using TheMaths;

namespace TheEngine.Test.Maths;

public class PerformanceVectorTests
{
    //[Test]
    public void TestPerformanceVector3()
    {
        Vector3[] vectors = new Vector3[10000000];
        TheMaths.OldVector3[] vectors2 = new TheMaths.OldVector3[10000000];
        Stopwatch sw = new Stopwatch();
        for (int i = 0; i < vectors.Length; ++i)
        {
            vectors[i] = new Vector3(i, i * 1.0f / 4f, 1.0f / i);
            vectors2[i] = new TheMaths.OldVector3(i, i * 1.0f / 4f, 1.0f / i);
        }
        Vector3 temp = Vector3.Zero;
        sw.Start();
        for (int i = 1; i < vectors.Length; ++i)
            temp += vectors[i];
        sw.Stop();
        double numerics = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine("System.Numerics.Vector3: " + numerics);
        sw.Reset();
        TheMaths.OldVector3 temp2 = TheMaths.OldVector3.Zero;
        sw.Start();
        for (int i = 1; i < vectors2.Length; ++i)
            temp2 += vectors2[i];
        sw.Stop();
        var managed = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine("TheMaths.Vector3: " + managed);
        Assert.IsTrue(numerics < managed);
    }
    
    
    //[Test]
    public void TestPerformanceMatrixInverse()
    {
        Matrix[] matrices = new Matrix[1000000];
        TheMaths.OldMatrix[] matrices2 = new TheMaths.OldMatrix[1000000];
        Stopwatch sw = new Stopwatch();
        for (int i = 0; i < matrices.Length; ++i)
        {
            matrices[i] = new Matrix(i, i * 1.0f / 4f, 1.0f / i, (float)Math.Sqrt(i), 0, 0, i, 2 * i, 0, 3, 1, 1.0f / i, 3, 4 * i, 5, 6);
            matrices2[i] = new TheMaths.OldMatrix(matrices[i].M11, matrices[i].M12, matrices[i].M13, matrices[i].M14, 
                                                     matrices[i].M21, matrices[i].M22, matrices[i].M23, matrices[i].M24,
                                                        matrices[i].M31, matrices[i].M32, matrices[i].M33, matrices[i].M34, 
                                                            matrices[i].M41, matrices[i].M42, matrices[i].M43, matrices[i].M44);
        }

        float det = 0;
        Matrix inv;
        sw.Start();
        for (int i = 1; i < matrices.Length; ++i)
        {
            Matrix.Invert(matrices[i], out inv);
            det += inv.GetDeterminant();
        }
        sw.Stop();
        double numerics = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine("System.Numerics.Matrix4x4: " + numerics);
        sw.Reset();
        float det2 = 0;
        OldMatrix inv2;
        sw.Start();
        for (int i = 1; i < matrices2.Length; ++i)
        {
            OldMatrix.Invert(ref matrices2[i], out inv2);
            det2 += inv2.Determinant();
        }
        sw.Stop();
        var managed = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine("TheMaths.Matrix: " + managed);
        Assert.IsTrue(numerics < managed);
    }
}