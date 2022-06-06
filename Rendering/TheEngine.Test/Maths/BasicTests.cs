using NUnit.Framework;
using TheMaths;

namespace TheEngine.Test.Maths;

public class BasicTests
{
    [Test]
    public void TestBaseVectors()
    {
        Assert.AreEqual(new Vector3(1, 0, 0), Vectors.Forward);
        Assert.AreEqual(new Vector3(0, 0, 1), Vectors.Up);
        Assert.AreEqual(new Vector3(0, 1, 0), Vectors.Left);
    }

    [Test]
    public void TestDirectionToQuaternion()
    {
        var dir = new Vector3(1, 0, 0);
        var quat = Utilities.LookRotation(dir, Vectors.Up);
        var quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 1, 0);
        quat = Utilities.LookRotation(dir, Vectors.Up);
        quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 0, 1);
        quat = Utilities.LookRotation(dir, Vectors.Up);
        quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 0, -1);
        quat = Utilities.LookRotation(dir, Vectors.Up);
        quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);

        dir = Vectors.Normalize(new Vector3(1, 0, 1));
        quat = Utilities.LookRotation(dir, Vectors.Up);
        quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = Vectors.Normalize(new Vector3(0.1f, 0, 1));
        quat = Utilities.LookRotation(dir, Vectors.Up);
        quatToDir = quat.Multiply(Vectors.Forward);
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
    }

    [Test]
    public void TestQuaternionToEuler()
    {
        var identity = Quaternion.Identity;
        var euler = identity.ToEulerDeg();
        
        Assert.AreEqual(0, euler.Length(), 0.01f);

        var q1 = Utilities.FromEuler(0, 0, 90);
        var e1 = q1.ToEulerDeg();
        Assert.AreEqual(90, e1.X, 0.01f);
        Assert.AreEqual(0, e1.Y, 0.01f);
        Assert.AreEqual(0, e1.Z, 0.01f);
        
        var q2 = Utilities.FromEuler(0, 90, 0);
        var e2 = q2.ToEulerDeg();
        Assert.AreEqual(0, e2.X, 0.01f);
        Assert.AreEqual(0, e2.Y, 0.01f);
        Assert.AreEqual(90, e2.Z, 0.01f);
        
        var q3 = Utilities.FromEuler(90, 0, 0);
        var e3 = q3.ToEulerDeg();
        Assert.AreEqual(0, e3.X, 0.01f);
        Assert.AreEqual(90, e3.Y, 0.01f);
        Assert.AreEqual(0, e3.Z, 0.01f);
    }
}