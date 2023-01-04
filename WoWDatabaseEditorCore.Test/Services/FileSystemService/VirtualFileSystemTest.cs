using System;
using System.IO;
using NUnit.Framework;
using WoWDatabaseEditorCore.Services.FileSystemService;

namespace WoWDatabaseEditorCore.Test.Services.FileSystemService
{
    public class VirtualFileSystemTest
    {
        private IVirtualFileSystem vfs = null!;
        
        [SetUp]
        public void Init()
        {
            vfs = new VirtualFileSystem();
        }
        
        [Test]
        public void TestMount()
        {
            Assert.Throws<ArgumentException>(() => vfs.MountDirectory("   ", "/"));
        }
        
        [Test]
        public void TestResolve()
        {
            var a = new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "a/b/abcd"));
            Assert.AreEqual(a.FullName, vfs.ResolvePath("a/b/abcd").FullName);
        }
        
        [Test]
        public void TestMountResolve()
        {
            vfs.MountDirectory("/internal/", "/usr/tmp");

            var a = new FileInfo("/usr/tmp/smartscript/actions.json");
            Assert.AreEqual(a.FullName, vfs.ResolvePath("/internal/smartscript/actions.json").FullName);
        }
    }
}