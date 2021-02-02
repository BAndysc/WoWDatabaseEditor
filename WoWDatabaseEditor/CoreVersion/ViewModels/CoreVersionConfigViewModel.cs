using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditor.CoreVersion.ViewModels
{
    [AutoRegister]
    public class CoreVersionConfigViewModel : ObservableBase, IConfigurable
    {
        private readonly ICurrentCoreVersion currentCoreVersion;
        public ObservableCollection<ICoreVersion> CoreVersions { get; }

        private ICoreVersion selectedVersion;
        public ICoreVersion SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                IsModified = true;
            }
        }
        
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }

        public CoreVersionConfigViewModel(ICoreVersionsProvider versionsProvider,
            ICoreVersionSettings settings,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
            Watch(this, t => t.SelectedVersion, nameof(IsModified));
            selectedVersion = currentCoreVersion.Current;
            
            CoreVersions = new ObservableCollection<ICoreVersion>(versionsProvider.AllVersions);
            
            Save = new DelegateCommand(() =>
            {
                settings.UpdateCurrentVersion(SelectedVersion);
                IsModified = false;
            });
        }
        
        public ICommand Save { get; }
        public string Name => "Core version";
        public bool IsRestartRequired => true;
    }
}