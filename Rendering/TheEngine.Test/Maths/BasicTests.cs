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
}