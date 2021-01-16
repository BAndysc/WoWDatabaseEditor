using System;
using System.Threading.Tasks;
using System.Windows;
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

            TrinityMySqlDatabaseProvider db = new(new ConnectionSettingsProvider(), new DatabaseLogger(), new MockTaskRunner());

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