using NSubstitute;
using NUnit.Framework;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Updater.Data;
using WDE.Updater.Services;

namespace WDE.Updater.Test.Services
{
    public class ChangelogProviderTest
    {
        private IUpdaterSettingsProvider settings = null!;
        private IApplicationVersion currentVersion = null!;
        private IDocumentManager documentManager = null!;
        private IFileSystem fileSystem = null!;
        
        [SetUp]
        public void Init()
        {
            settings = Substitute.For<IUpdaterSettingsProvider>();
            currentVersion = Substitute.For<IApplicationVersion>();
            documentManager = Substitute.For<IDocumentManager>();
            fileSystem = Substitute.For<IFileSystem>();

            fileSystem.Exists("changelog.json").Returns(true);
            fileSystem.ReadAllText("changelog.json").Returns(@"[{""Version"":294,""VersionName"":""Build 0.1.294"",""Date"":""2021-03-13T17:38:00"",""UpdateTitle"":null,""Changes"":[]}]");
        }
        
        [Test]
        public void TestNoShowChangelogIfShownBefore()
        {
            settings.Settings.Returns(new UpdaterSettings() {LastShowedChangelog = 1});
            currentVersion.BuildVersion.Returns(0);
            
            new ChangelogProvider(settings, currentVersion, documentManager, fileSystem);
            
            documentManager.DidNotReceive().OpenDocument(Arg.Any<IDocument>());
        }
        
        [Test]
        public void TestNoShowIfInvalidChangelog()
        {
            fileSystem.ReadAllText("changelog.json").Returns(@"[{""Version"":294a}]");
            settings.Settings.Returns(new UpdaterSettings() {LastShowedChangelog = 0});
            currentVersion.BuildVersion.Returns(1);
            
            new ChangelogProvider(settings, currentVersion, documentManager, fileSystem);
            
            documentManager.DidNotReceive().OpenDocument(Arg.Any<IDocument>());
        }

        [Test]
        public void TestNoShowIfNoChangelog()
        {
            fileSystem.Exists("changelog.json").Returns(false);
            settings.Settings.Returns(new UpdaterSettings() {LastShowedChangelog = 0});
            currentVersion.BuildVersion.Returns(1);
            
            new ChangelogProvider(settings, currentVersion, documentManager, fileSystem);
            
            documentManager.DidNotReceive().OpenDocument(Arg.Any<IDocument>());
        }
        
        [Test]
        public void TestShowChangelogUpdatesLastShowedDate()
        {
            settings.Settings.Returns(new UpdaterSettings() {LastShowedChangelog = 0});
            currentVersion.BuildVersion.Returns(1);
            
            new ChangelogProvider(settings, currentVersion, documentManager, fileSystem);

            settings.Received().Settings = new UpdaterSettings(){LastShowedChangelog = 1};
        }
    }
}