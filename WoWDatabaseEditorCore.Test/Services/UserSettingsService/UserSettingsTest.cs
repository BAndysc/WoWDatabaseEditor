using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Managers;
using WDE.Common.Services;
using WoWDatabaseEditorCore.Services.UserSettingsService;

namespace WoWDatabaseEditorCore.Test.Services.UserSettingsService
{
    public class UserSettingsTest
    {
        private IFileSystem fileSystem = null!;
        private IStatusBar statusBar = null!;
        private UserSettings userSettings = null!;
        
        [SetUp]
        public void Init()
        {
            fileSystem = Substitute.For<IFileSystem>();
            statusBar = Substitute.For<IStatusBar>();
            userSettings = new UserSettings(fileSystem, new Lazy<IStatusBar>(statusBar));
        }
        
        [Test]
        public void TestGetNoDefaultValue()
        {
            var settings = userSettings.Get<DemoSettings>();
            Assert.AreEqual(0, settings.ValueType);
            Assert.AreEqual(null, settings.ReferenceType);
        }

        [Test]
        public void TestGetDefaultValue()
        {
            var settings = userSettings.Get<DemoSettings>(new DemoSettings() {ValueType = 4});
            Assert.AreEqual(4, settings.ValueType);
            Assert.AreEqual(null, settings.ReferenceType);
        }

        [Test]
        public void TestGetPath()
        {
            userSettings.Get<DemoSettings>();
            fileSystem.Received(1)
                .Exists(Arg.Is<string>(path => 
                    path.Replace("\\", "/") == "~/WoWDatabaseEditorCore.Test.Services.UserSettingsService.UserSettingsTest.DemoSettings.json"));
        }

        [Test]
        public void TestSave()
        {
            using var memoryStream = new MemoryStream();
            fileSystem.OpenWrite(Arg.Any<string>()).Returns(memoryStream);
            userSettings.Update(new DemoSettings(){ValueType = 1234, ReferenceType = "abcdef"});
            
            memoryStream.Flush();

            string result = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            var stripped = result.Replace("\n", "").Replace("\r", "").Replace(" ", "");
            
            // comparing string on purpose
            // if the format changes, verify if some migration is needed
            Assert.AreEqual("{\"ValueType\":1234,\"ReferenceType\":\"abcdef\"}", stripped);
        }
        
        [Test]
        public void TestGet()
        {
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"ValueType\":1234,\"ReferenceType\":\"abcdef\"}"));
            fileSystem.Exists(Arg.Any<string>()).Returns(true);
            fileSystem.OpenRead(Arg.Any<string>()).Returns(memoryStream);
            
            var settings = userSettings.Get<DemoSettings>();
            Assert.AreEqual(1234, settings.ValueType);
            Assert.AreEqual("abcdef", settings.ReferenceType);
        }
        
        [Test]
        public void TestFailGet()
        {
            fileSystem.Exists(Arg.Any<string>()).Returns(true);

            var settings = userSettings.Get<DemoSettings>();
            Assert.AreEqual(0, settings.ValueType);
            Assert.AreEqual(null, settings.ReferenceType);
            
            statusBar.Received(1).PublishNotification(Arg.Any<INotification>());
        }
        
        [ExcludeFromCodeCoverage]
        private struct DemoSettings : ISettings
        {
            public int ValueType { get; set; }
            public string ReferenceType { get; set; }
        }
    }
}