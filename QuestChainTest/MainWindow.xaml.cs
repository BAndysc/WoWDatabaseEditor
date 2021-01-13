using System.Windows;
using WDE.QuestChainEditor.Editor.ViewModels;
using WDE.QuestChainEditor.Exporter;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;
using WDE.TrinityMySqlDatabase;
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

            TrinityMysqlDatabaseProvider db = new(new ConnectionSettingsProvider(), new DatabaseLogger());

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
    }
}