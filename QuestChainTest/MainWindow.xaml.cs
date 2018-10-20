using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WDE.QuestChainEditor.Editor.ViewModels;
using WDE.QuestChainEditor.Exporter;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;
using WDE.TrinityMySqlDatabase;

namespace QuestChainTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        QuestList quests;
        public MainWindow()
        {
            InitializeComponent();
            
            quests = new QuestList();
            
            new TrinityMySqlDatabaseModule().OnInitialized(null);

            var db = new TrinityMysqlDatabaseProvider();

            View.DataContext = new QuestChainEditorViewModel(new QuestPicker(new DatabaseQuestsProvider(db)), quests);

            quests.OnAddedQuest += (sender, quest) =>
            {
                Update();
                quest.RequiredQuests.CollectionChanged += (sender2, e2) =>
                {
                    Update();
                };
            };
            quests.OnRemovedQuest += (sender, quest) => Update();
            Update();

            foreach (var q in quests)
            {
                q.RequiredQuests.CollectionChanged += (sender, e) =>
                {
                    Update();
                };
            }
        }
        private void Update()
        {
            Text.Text = new QuestChainExporter().GenerateSQL(quests);
        }
    }
}
