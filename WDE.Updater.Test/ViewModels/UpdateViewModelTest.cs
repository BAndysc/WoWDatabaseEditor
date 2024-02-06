using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Updater.Data;
using WDE.Updater.Services;
using WDE.Updater.ViewModels;

namespace WDE.Updater.Test.ViewModels
{
    public class UpdateViewModelTest
    {
        private IUpdateService updateService = null!;
        private ITaskRunner taskRunner = null!;
        private IStatusBar statusBar = null!;
        private IUpdaterSettingsProvider settingsProvider = null!;
        private IAutoUpdatePlatformService platformService = null!;
        private IFileSystem fileSystem = null!;
        private IMessageBoxService messageBoxService = null!;
        private IApplication application = null!;
        private IStandaloneUpdater standaloneUpdater = null!;
        private IMainThread mainThread = null!;

        [SetUp]
        public void Init()
        {
            updateService = Substitute.For<IUpdateService>();
            taskRunner = Substitute.For<ITaskRunner>();
            statusBar = Substitute.For<IStatusBar>();
            settingsProvider = Substitute.For<IUpdaterSettingsProvider>();
            platformService = Substitute.For<IAutoUpdatePlatformService>();
            fileSystem = Substitute.For<IFileSystem>();
            messageBoxService = Substitute.For<IMessageBoxService>();
            application = Substitute.For<IApplication>();
            standaloneUpdater = Substitute.For<IStandaloneUpdater>();
            mainThread = Substitute.For<IMainThread>();
        }

        [Test]
        public void TestNoAutoUpdate()
        {
            settingsProvider.Settings.Returns(new UpdaterSettings() {DisableAutoUpdates = true});
            new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider, platformService, fileSystem, standaloneUpdater, application, mainThread, messageBoxService);
            
            taskRunner.DidNotReceive().ScheduleTask(Arg.Any<string>(), Arg.Any<Func<Task>>());
            taskRunner.DidNotReceive().ScheduleTask(Arg.Any<IAsyncTask>());
        }

        [Test]
        public void TestAutoUpdate()
        {
            updateService.CanCheckForUpdates().Returns(true);
            settingsProvider.Settings.Returns(new UpdaterSettings() {DisableAutoUpdates = false});
            new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider, platformService, fileSystem, standaloneUpdater, application, mainThread, messageBoxService);
            
            taskRunner.Received().ScheduleTask(Arg.Any<IAsyncTask>());
        }

        [Test]
        public void TestNoUpdateIfCant()
        {
            updateService.CanCheckForUpdates().Returns(false);
            var vm = new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider, platformService, fileSystem, standaloneUpdater, application, mainThread, messageBoxService);
            
            taskRunner.DidNotReceive().ScheduleTask(Arg.Any<string>(), Arg.Any<Func<Task>>());
            Assert.IsFalse(vm.CheckForUpdatesCommand.CanExecute(null));
        }
    }
}