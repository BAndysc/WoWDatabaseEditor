﻿namespace QuestChainTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private readonly QuestList quests;

        public MainWindow()
        {
            InitializeComponent();

            /*quests = new QuestList();

            new TrinityMySqlDatabaseModule().OnInitialized(null);

            TrinityMySqlDatabaseProvider db = new(new WorldDatabaseSettingsProvider(null), new DatabaseLogger(), new MockCoreVersion());

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
                q.RequiredQuests.CollectionChanged += (sender, e) => { Update(); };*/
        }

        /*public class MockCoreVersion : ICurrentCoreVersion, ICoreVersion, IDatabaseFeatures,ISmartScriptFeatures, IConditionFeatures
        {
            public ICoreVersion Current => this;
            public string Tag => "mock";
            public string FriendlyName => "mock";
            public IDatabaseFeatures DatabaseFeatures => this;
            public ISmartScriptFeatures SmartScriptFeatures => this;
            public IConditionFeatures ConditionFeatures => this;
            public ISet<Type> UnsupportedTables => new HashSet<Type>();
            public ISet<SmartScriptType> SupportedTypes => null;

            public string ConditionsFile => "SmartData/conditions.json";
            public string ConditionGroupsFile => "SmartData/conditions_groups.json";
            public string ConditionSourcesFile => "SmartData/condition_sources.json";
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
        }*/
    }
}