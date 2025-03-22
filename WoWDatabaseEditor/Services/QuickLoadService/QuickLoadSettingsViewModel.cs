using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Common.Utils.DragDrop;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.QuickLoadService
{
    [AutoRegister]
    public class QuickLoadSettingsViewModel : ObservableBase, IConfigurable, IDropTarget
    {
        private readonly IQuickLoadSettings settings;
        private QuickLoadItem? selectedItem;
        private bool isModified;
        
        public ICommand Save { get; }
        public ICommand AddDivider { get; }
        public ICommand RemoveDivider { get; }
        public ICommand SetVisibleAll { get; }
        public ICommand SetVisibleNone { get; }
        public ImageUri Icon { get; } = new ImageUri("Icons/document_homepage_big.png");
        public string Name => "Quick start";
        public string? ShortDescription => "You can configure look and feel for you quick start panel";

        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }

        public bool IsRestartRequired => false;
        public ConfigurableGroup Group => ConfigurableGroup.Advanced;

        public QuickLoadItem? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public ObservableCollection<QuickLoadItem> QuickLoadItems { get; } = new();

        public QuickLoadSettingsViewModel(ISolutionItemProvideService solutionItemProvideService,
            IQuickLoadSettings settings, ICurrentCoreVersion currentCoreVersion)
        {
            this.settings = settings;
            foreach (var item in solutionItemProvideService.AllCompatible)
            {
                if (item.IsContainer || item is INamedSolutionItemProvider || !item.ShowInQuickStart(currentCoreVersion.Current))
                    continue;
                QuickLoadItems.Add(new QuickLoadItem(item));
            }
            settings.ApplySavedSettings(QuickLoadItems);

            foreach (var i in QuickLoadItems)
                AutoDispose(i.ToObservable(o => o.IsVisible).SubscribeAction(_ => IsModified = true));

            for (int i = QuickLoadItems.Count - 1; i >= 0; --i)
            {
                if (settings.IsNewLineSeparator(QuickLoadItems[i].Name))
                    QuickLoadItems.Insert(i, NewDivider());
            }
            
            Save = new DelegateCommand(() =>
            {
                settings.Update(QuickLoadItems);
                IsModified = false;
            });

            SetVisibleAll = new DelegateCommand(() =>
            {
                foreach (var item in QuickLoadItems)
                    item.IsVisible = true;
            });

            SetVisibleNone = new DelegateCommand(() =>
            {
                foreach (var item in QuickLoadItems)
                    item.IsVisible = false;
            });
            
            AddDivider = new DelegateCommand(() =>
            {
                var indexOf = SelectedItem == null ? QuickLoadItems.Count : QuickLoadItems.IndexOf(SelectedItem) + 1;
                QuickLoadItems.Insert(indexOf, NewDivider());
                IsModified = true;
            });
            
            RemoveDivider = new DelegateCommand(() =>
            {
                if (SelectedItem is { Name: "---" })
                    QuickLoadItems.Remove(SelectedItem);
                IsModified = true;
            }, () => SelectedItem is { Name: "---" })
                .ObservesProperty(() => SelectedItem);

            IsModified = false;
        }

        private QuickLoadItem NewDivider() => new QuickLoadItem("---", new ImageUri("Icons/icon_word_wrap.png"));

        public void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not QuickLoadItem data)
                return;

            int indexOf = QuickLoadItems.IndexOf(data);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            QuickLoadItems.RemoveAt(indexOf);
            QuickLoadItems.Insert(dropIndex, data);
            SelectedItem = data;
            IsModified = true;
        }
    }
}