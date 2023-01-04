using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Updater.Client;
using WDE.Updater.Data;
using WDE.Updater.Models;
using WDE.Updater.Services;

namespace WDE.Updater.Test.Services
{
    public class UpdateServiceTest
    {
        private IUpdateServerDataProvider data = null!;
        private IUpdateClientFactory clientFactory = null!;
        private IApplicationVersion applicationVersion = null!;
        private IApplication application = null!;
        private IFileSystem fileSystem = null!;
        private IStandaloneUpdater standaloneUpdate = null!;
        private IUpdateClient updateClient = null!;
        private IUpdaterSettingsProvider settings = null!;
        private IAutoUpdatePlatformService platformService = null!;
        private IUpdateVerifier updateVerifier = null!;
        
        [SetUp]
        public void Init()
        {
            data = Substitute.For<IUpdateServerDataProvider>();
            clientFactory = Substitute.For<IUpdateClientFactory>();
            applicationVersion = Substitute.For<IApplicationVersion>();
            application = Substitute.For<IApplication>();
            fileSystem = Substitute.For<IFileSystem>();
            standaloneUpdate = Substitute.For<IStandaloneUpdater>();
            updateClient = Substitute.For<IUpdateClient>();
            settings = Substitute.For<IUpdaterSettingsProvider>();
            platformService = Substitute.For<IAutoUpdatePlatformService>();
            updateVerifier = Substitute.For<IUpdateVerifier>();
            clientFactory
                .Create(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<UpdatePlatforms>())
                .Returns(updateClient);
        }
        
        [Test]
        public void TestNoUpdateIfNoVersion()
        {
            applicationVersion.VersionKnown.Returns(false);
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.IsFalse(updateService.CanCheckForUpdates());
        }
        
        [Test]
        public void TestNoUpdateIfNoUpdateData()
        {
            data.HasUpdateServerData.Returns(false);
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.IsFalse(updateService.CanCheckForUpdates());
        }

        [Test]
        public void TestCanCheckForUpdatesIfHaveData()
        {
            applicationVersion.VersionKnown.Returns(true);
            data.HasUpdateServerData.Returns(true);
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.IsTrue(updateService.CanCheckForUpdates());
        }

        [Test]
        public async Task TestCheckForUpdatesNoUpdateLink()
        {
            applicationVersion.VersionKnown.Returns(true);
            data.HasUpdateServerData.Returns(true);
            updateClient
                .CheckForUpdates(Arg.Any<string>(), Arg.Any<long>())
                .Returns(new CheckVersionResponse() {DownloadUrl = null, LatestVersion = 2});
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.IsNull(await updateService.CheckForUpdates(false));
        }
        
        [Test]
        public async Task TestCheckForUpdatesNoUpdate()
        {
            applicationVersion.VersionKnown.Returns(true);
            applicationVersion.BuildVersion.Returns(1);
            data.HasUpdateServerData.Returns(true);
            updateClient
                .CheckForUpdates(Arg.Any<string>(), Arg.Any<long>())
                .Returns(new CheckVersionResponse() {DownloadUrl = null, LatestVersion = 1});
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.IsNull(await updateService.CheckForUpdates(false));
        }
        
        [Test]
        public async Task TestCheckForUpdatesHasUpdate()
        {
            applicationVersion.VersionKnown.Returns(true);
            data.HasUpdateServerData.Returns(true);
            updateClient
                .CheckForUpdates(Arg.Any<string>(), Arg.Any<long>())
                .Returns(new CheckVersionResponse() {DownloadUrl = "localhost", LatestVersion = 2});
            
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            Assert.AreEqual("localhost", await updateService.CheckForUpdates(false));
        }

        [Test]
        public async Task TestCloseForUpdateAsks()
        {
            application.CanClose().Returns(Task.FromResult(false));
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            await updateService.CloseForUpdate();

            await application.Received().CanClose();
            
            application.DidNotReceive().ForceClose();
            standaloneUpdate.DidNotReceive().Launch();
        }
        
        [Test]
        public async Task TestCloseForUpdate()
        {
            application.CanClose().Returns(Task.FromResult(true));
            var updateService = new UpdateService(data, clientFactory, applicationVersion, application, fileSystem,
                standaloneUpdate, settings, platformService, updateVerifier);

            await updateService.CloseForUpdate();

            await application.Received().CanClose();
            
            application.Received().ForceClose();
            standaloneUpdate.Received().Launch();
        }
    }
}