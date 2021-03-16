using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Managers;
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
        
        [SetUp]
        public void Init()
        {
            updateService = Substitute.For<IUpdateService>();
            taskRunner = Substitute.For<ITaskRunner>();
            statusBar = Substitute.For<IStatusBar>();
            settingsProvider = Substitute.For<IUpdaterSettingsProvider>();
        }

        [Test]
        public void TestNoAutoUpdate()
        {
            settingsProvider.Settings.Returns(new UpdaterSettings() {DisableAutoUpdates = true});
            new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider);
            
            taskRunner.DidNotReceive().ScheduleTask(Arg.Any<string>(), Arg.Any<Func<Task>>());
        }

        [Test]
        public void TestAutoUpdate()
        {
            updateService.CanCheckForUpdates().Returns(true);
            settingsProvider.Settings.Returns(new UpdaterSettings() {DisableAutoUpdates = false});
            new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider);
            
            taskRunner.Received().ScheduleTask(Arg.Any<string>(), Arg.Any<Func<Task>>());
        }

        [Test]
        public void TestNoUpdateIfCant()
        {
            updateService.CanCheckForUpdates().Returns(false);
            var vm = new UpdateViewModel(updateService, taskRunner, statusBar, settingsProvider);
            
            taskRunner.DidNotReceive().ScheduleTask(Arg.Any<string>(), Arg.Any<Func<Task>>());
            Assert.IsFalse(vm.CheckForUpdatesCommand.CanExecute(null));
        }
    }
}