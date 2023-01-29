using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using FuzzySharp;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.Parameters.ViewModels
{
    [AutoRegister]
    public partial class ParametersViewModel : ObservableBase, IDocument
    {
        [Notify] private string searchText = "";
        private bool hasSelected = true;
        private ParameterSpecModel? selected;

        public ParametersViewModel(ParameterFactory factory)
        {
            foreach (string key in factory.GetKeys())
                Parameters.Add(factory.GetDefinition(key));
            Parameters.Sort((a, b) => String.Compare(a!.Name, b!.Name, StringComparison.Ordinal));
            if (Parameters.Count > 0)
                Selected = Parameters[0];
            
            On(() => SearchText, t =>
            {
                if (string.IsNullOrWhiteSpace(t))
                {
                    FilteredParameters.Clear();
                    FilteredParameters.AddRange(Parameters);
                    if (selected != null && FilteredParameters.IndexOf(selected) == -1)
                        Selected = FilteredParameters.FirstOrDefault();
                }
                else
                {
                    DoFilter(t).ListenErrors();
                }
            });
        }

        private async Task DoFilter(string text)
        {
            var indices = await Task.Run(() =>
            {
                List<int> result = new();
                var results = Process.ExtractAll(text, Parameters.Select(x => x.Name), x => x.ToLowerInvariant(), null, 75);
                foreach (var r in results)
                    result.Add(r.Index);
                return result;
            });
            if (searchText == text)
            {
                FilteredParameters.Clear();
                foreach (var index in indices)
                    FilteredParameters.Add(Parameters[index]);

                if (selected != null && FilteredParameters.IndexOf(selected) == -1)
                    Selected = FilteredParameters.Count == 0 ? null : FilteredParameters[0];
            }
        }

        public ObservableCollection<ParameterSpecModel> FilteredParameters { get; } = new();
        public ObservableCollection<ParameterSpecModel> Parameters { get; } = new();

        public ParameterSpecModel? Selected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public bool HasSelected
        {
            get => hasSelected;
            set => SetProperty(ref hasSelected, value);
        }

        public string Title => "Parameters browser";
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public IHistoryManager? History { get; set; }
        public bool IsModified => false;
    }
    
    [AutoRegister]
    public class ParametersMenu : IToolMenuItem
    {
        public string ItemName => "Parameters browser";
        public ICommand ItemCommand { get; }
        public MenuShortcut? Shortcut => null;

        public ParametersMenu(Func<ParametersViewModel> creator,
            Lazy<IDocumentManager> documentManager)
        {
            ItemCommand = new DelegateCommand(() =>
            {
                documentManager.Value.OpenDocument(creator());
            });
        }
    }
}