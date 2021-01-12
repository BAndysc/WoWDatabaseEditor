using System.Windows.Input;
using ExtensionMethods;
using Prism.Mvvm;
using WDE.Blueprints.Data;
using WDE.Blueprints.Editor.Views;
using WDE.Blueprints.Enums;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Utils;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class BlueprintEditorViewModel : BindableBase, IDocument
    {
        private bool showNodePicker;
        private BlueprintSolutionItem solutionItem;

        public BlueprintEditorViewModel(BlueprintSolutionItem solutionItem, NodesViewModel nodesViewModel)
        {
            this.solutionItem = solutionItem;

            GraphViewModel = new GraphViewModel();

            GraphViewModel.RequestNodePickerWindow += async connection =>
            {
                //ShowNodePicker = true;
                NodePickerWindow window = new();
                nodesViewModel.SetCurrentConnectionContext(connection);
                window.DataContext = nodesViewModel;
                await window.ShowDialogAsync(); //.ShowDialogCenteredToMouse();
            };

            GraphViewModel.AddElement(new NodeViewModel("Node 1", NodeType.Event, 0, 3), 10000, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 2", NodeType.Statement, 2, 3), 10100, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 3", NodeType.Expression, 1, 1), 10200, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 4", NodeType.Statement, 4, 1), 10300, 10000);
        }

        public GraphViewModel GraphViewModel { get; }

        public bool ShowNodePicker
        {
            get => showNodePicker;
            set => SetProperty(ref showNodePicker, value);
        }

        public void Dispose()
        {
        }

        public string Title { get; } = "Blueprints";
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; } = AlwaysDisabledCommand.Command;
        public ICommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public IHistoryManager History { get; } = null;

        public void PickerResponse(NodeDefinition def)
        {
            ShowNodePicker = false;
        }
    }
}