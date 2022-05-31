using NUnit.Framework;
using TheMaths;

namespace TheEngine.Test.Maths;

public class BasicTests
{
    [Test]
    public void TestBaseVectors()
    {
        Assert.AreEqual(new Vector3(1, 0, 0), Vector3.Forward);
        Assert.AreEqual(new Vector3(0, 0, 1), Vector3.Up);
        Assert.AreEqual(new Vector3(0, 1, 0), Vector3.Left);
    }

    [Test]
    public void TestDirectionToQuaternion()
    {
        var dir = new Vector3(1, 0, 0);
        var quat = Quaternion.LookRotation(dir, Vector3.Up);
        var quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 1, 0);
        quat = Quaternion.LookRotation(dir, Vector3.Up);
        quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 0, 1);
        quat = Quaternion.LookRotation(dir, Vector3.Up);
        quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0, 0, -1);
        quat = Quaternion.LookRotation(dir, Vector3.Up);
        quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);

        dir = new Vector3(1, 0, 1).Normalized;
        quat = Quaternion.LookRotation(dir, Vector3.Up);
        quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
        
        dir = new Vector3(0.1f, 0, 1).Normalized;
        quat = Quaternion.LookRotation(dir, Vector3.Up);
        quatToDir = quat * Vector3.Forward;
        Assert.AreEqual(1, Vector3.Dot(quatToDir, dir), 0.01f);
    }

    [Test]
    public void TestQuaternionToEuler()
    {
        var identity = Quaternion.Identity;
        var euler = identity.ToEulerDeg();
        
        Assert.AreEqual(0, euler.Length(), 0.01f);

        var q1 = Quaternion.FromEuler(0, 0, 90);
        var e1 = q1.ToEulerDeg();
        Assert.AreEqual(90, e1.X, 0.01f);
        Assert.AreEqual(0, e1.Y, 0.01f);
        Assert.AreEqual(0, e1.Z, 0.01f);
        
        var q2 = Quaternion.FromEuler(0, 90, 0);
        var e2 = q2.ToEulerDeg();
        Assert.AreEqual(0, e2.X, 0.01f);
        Assert.AreEqual(0, e2.Y, 0.01f);
        Assert.AreEqual(90, e2.Z, 0.01f);
        
        var q3 = Quaternion.FromEuler(90, 0, 0);
        var e3 = q3.ToEulerDeg();
        Assert.AreEqual(0, e3.X, 0.01f);
        Assert.AreEqual(90, e3.Y, 0.01f);
        Assert.AreEqual(0, e3.Z, 0.01f);
    }
}