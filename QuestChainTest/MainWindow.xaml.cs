using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.QuestChainEditor.Editor.ViewModels;
using WDE.QuestChainEditor.Exporter;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;
using WDE.TrinityMySqlDatabase;
using WDE.TrinityMySqlDatabase.Database;
using WDE.TrinityMySqlDatabase.Providers;
using WDE.TrinityMySqlDatabase.Services;

namespace QuestChainTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly QuestList quests;

        public MainWindow()
        {
            InitializeComponent();

            quests = new QuestList();

            new TrinityMySqlDatabaseModule().OnInitialized(null);

            // TODO: this null probably will crash so fix it when module is needed
            TrinityMySqlDatabaseProvider db = new(new ConnectionSettingsProvider(null), new DatabaseLogger(), new MockCoreVersion());

            ExampleQuestsProvider exampleQuestProvider = new();

            View.DataContext = new QuestChainEditorViewModel(new QuestPicker(exampleQuestProvider), quests);

            quests.OnAddedQuest += (sender, quest) =>
            {
                Update();
                quest.RequiredQuests.CollectionChanged += (sender2, e2) => { Update(); };
            };
            quests.OnRemovedQuest += (sender, quest) => Update();
            Update();

            foreach (Quest q in quests)
                q.RequiredQuests.CollectionChanged += (sender, e) => { Update(); };
        }

        public class MockCoreVersion : ICurrentCoreVersion, ICoreVersion, IDatabaseFeatures,ISmartScriptFeatures
        {
            public ICoreVersion Current => this;
            public string Tag => "mock";
            public string FriendlyName => "mock";
            public IDatabaseFeatures DatabaseFeatures => this;
            public ISmartScriptFeatures SmartScriptFeatures => this;
            public ISet<Type> UnsupportedTables => new HashSet<Type>();
            public ISet<SmartScriptType> SupportedTypes => null;
        }

        private void Update()
        {
            Text.Text = new QuestChainExporter().GenerateSQL(quests);
        }

        private class MockTaskRunner : ITaskRunner
        {
            public void ScheduleTask(IThreadedTask threadedTask)
            {
            }

            public void ScheduleTask(IAsyncTask task)
            {
            }

            public void ScheduleTask(string name, Func<ITaskProgress, Task> task)
            {
            }

            public void ScheduleTask(string name, Func<Task> task)
            {
            }
        }
    }
}