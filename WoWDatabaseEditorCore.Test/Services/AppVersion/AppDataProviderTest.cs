using NSubstitute;
using NUnit.Framework;
using WDE.Common.Services;
using WoWDatabaseEditorCore.Services.AppVersion;

namespace WoWDatabaseEditorCore.Test.Services.AppVersion
{
    public class AppDataProviderTest
    {
        private IFileSystem fileSystem = null!;
        
        [SetUp]
        public void Init()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.Exists("app.ini").Returns(true);
        }
        
        [Test]
        public void TestNoFile()
        {
            fileSystem.Exists("app.ini").Returns(false);
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.IsNull(provider.GetString("a"));
            Assert.IsNull(provider.GetInt("b"));
        }
        
        [Test]
        public void TestCorrectFile()
        {
            fileSystem.ReadAllText("app.ini").Returns("abc=c b d  \nx=51");
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.AreEqual("c b d", provider.GetString("abc"));
            Assert.AreEqual(51, provider.GetInt("x"));
        }
        
        [Test]
        public void TestDuplicates()
        {
            fileSystem.ReadAllText("app.ini").Returns("abc=c b d  \nabc=nn");
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.AreEqual("nn", provider.GetString("abc"));
        }
        
        [Test]
        public void TestMultipleEqualSigns()
        {
            fileSystem.ReadAllText("app.ini").Returns("abc= b == cas  ");
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.AreEqual("b == cas", provider.GetString("abc"));
        }
        
        [Test]
        public void TestIncorrectLines()
        {
            fileSystem.ReadAllText("app.ini").Returns("asd\nddd\n\n\n\r\rx=7\n\n\ns");
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.AreEqual("7", provider.GetString("x"));
        }
        
        [Test]
        public void TestGetNotInt()
        {
            fileSystem.ReadAllText("app.ini").Returns("x=a");
            
            var provider = new ApplicationReleaseConfiguration(fileSystem);
            Assert.AreEqual(null, provider.GetInt("x"));
        }
    }
}