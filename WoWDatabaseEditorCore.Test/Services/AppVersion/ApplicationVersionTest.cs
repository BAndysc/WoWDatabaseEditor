using NSubstitute;
using NUnit.Framework;
using WDE.Common.Services;
using WoWDatabaseEditorCore.Services.AppVersion;

namespace WoWDatabaseEditorCore.Test.Services.AppVersion
{
    public class ApplicationVersionTest
    {
        private IApplicationReleaseConfiguration applicationReleaseConfiguration = null!;

        [SetUp]
        public void Init()
        {
            applicationReleaseConfiguration = Substitute.For<IApplicationReleaseConfiguration>();
            applicationReleaseConfiguration.GetString("BRANCH").Returns("master");
            applicationReleaseConfiguration.GetString("COMMIT").Returns("da5d17");
            applicationReleaseConfiguration.GetInt("BUILD_VERSION").Returns(12345);
        }

        [Test]
        public void TestCorrectConfiguration()
        {
            var appVersion = new ApplicationVersion(applicationReleaseConfiguration);
            
            Assert.AreEqual("master", appVersion.Branch);
            Assert.AreEqual("da5d17", appVersion.CommitHash);
            Assert.AreEqual(12345, appVersion.BuildVersion);
            Assert.AreEqual(true, appVersion.VersionKnown);
        }
        
        [Test]
        public void TestNoBranch()
        {
            applicationReleaseConfiguration.GetString("BRANCH").Returns((string)null!);
            
            var appVersion = new ApplicationVersion(applicationReleaseConfiguration);
            Assert.AreEqual(false, appVersion.VersionKnown);
        }
        
        [Test]
        public void TestNoCommit()
        {
            applicationReleaseConfiguration.GetString("COMMIT").Returns((string)null!);
            
            var appVersion = new ApplicationVersion(applicationReleaseConfiguration);
            Assert.AreEqual(false, appVersion.VersionKnown);
        }
        
        [Test]
        public void TestNoBuild()
        {
            applicationReleaseConfiguration.GetInt("BUILD_VERSION").Returns((int?)null);
            
            var appVersion = new ApplicationVersion(applicationReleaseConfiguration);
            Assert.AreEqual(false, appVersion.VersionKnown);
        }
    }
}