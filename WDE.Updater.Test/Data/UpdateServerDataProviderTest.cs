using System;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Services;
using WDE.Updater.Data;
using WDE.Updater.Models;

namespace WDE.Updater.Test.Data
{
    public class UpdateServerDataProviderTest
    {
        private IApplicationReleaseConfiguration config = null!;
        
        [Test]
        public void TestCorrect()
        {
            Setup("http://localhost", "abc", "def", "Linux");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsTrue(provider.HasUpdateServerData);
            Assert.AreEqual(new Uri("http://localhost"), provider.UpdateServerUrl);
            Assert.AreEqual("abc", provider.UpdateKey);
            Assert.AreEqual("def", provider.Marketplace);
            Assert.AreEqual(UpdatePlatforms.Linux, provider.Platform);
        }

        [Test]
        public void TestNoServer()
        {
            Setup(null, "abc", "def", "Linux");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsFalse(provider.HasUpdateServerData);
        }
        
        [Test]
        public void TestNoKey()
        {
            Setup("http://localhost", null, "def", "Linux");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsTrue(provider.HasUpdateServerData);
        }
        
        [Test]
        public void TestNoMarketplace()
        {
            Setup("http://localhost", "abc", null, "Linux");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsFalse(provider.HasUpdateServerData);
        }
        
        [Test]
        public void TestNoPlatform()
        {
            Setup("http://localhost", "abc", "def", null);
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsFalse(provider.HasUpdateServerData);
        }
        
        [Test]
        public void TestWrongPlatform()
        {
            Setup("http://localhost", "abc", "def", "Doors");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsFalse(provider.HasUpdateServerData);
        }

        [Test]
        public void TestInvalidUri()
        {
            Setup("localhost", "abc", "def", "Doors");
            var provider = new UpdateServerDataProvider(config);
            
            Assert.IsFalse(provider.HasUpdateServerData);
        }
        
        [Test]
        public void TestPlatforms()
        {
            Setup("http://localhost", "abc", "def", "Linux");
            var provider = new UpdateServerDataProvider(config);
            Assert.AreEqual(UpdatePlatforms.Linux, provider.Platform);
            
            Setup("http://localhost", "abc", "def", "Windows");
            provider = new UpdateServerDataProvider(config);
            Assert.AreEqual(UpdatePlatforms.Windows, provider.Platform);
            
            Setup("http://localhost", "abc", "def", "MacOs");
            provider = new UpdateServerDataProvider(config);
            Assert.AreEqual(UpdatePlatforms.MacOs, provider.Platform);
            
            Setup("http://localhost", "abc", "def", "MacOsArm");
            provider = new UpdateServerDataProvider(config);
            Assert.AreEqual(UpdatePlatforms.MacOsArm, provider.Platform);

            Setup("http://localhost", "abc", "def", "WindowsWpf");
            provider = new UpdateServerDataProvider(config);
            Assert.AreEqual(UpdatePlatforms.WindowsWpf, provider.Platform);
        }
        
        private void Setup(string? server, string? key, string? marketplace, string? platform)
        {
            config = Substitute.For<IApplicationReleaseConfiguration>();

            config.GetString("UPDATE_SERVER").Returns(server);
            config.GetString("UPDATE_KEY").Returns(key);
            config.GetString("MARKETPLACE").Returns(marketplace);
            config.GetString("PLATFORM").Returns(platform);
        }
    }
}