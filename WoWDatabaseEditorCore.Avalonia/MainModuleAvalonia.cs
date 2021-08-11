using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module;
using WoWDatabaseEditorCore.Avalonia.Services.CreatureEntrySelectorService;
using WoWDatabaseEditorCore.Services.CreatureEntrySelectorService;
using WDE.Common.Windows;
using WoWDatabaseEditorCore.Avalonia.Services.InputEntryProviderService;
using WoWDatabaseEditorCore.Avalonia.Services.ItemFromListSelectorService;
using WoWDatabaseEditorCore.Services.InputEntryProviderService;
using WoWDatabaseEditorCore.Services.ItemFromListSelectorService;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class MainModuleAvalonia : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            var viewLocator = containerProvider.Resolve<IViewLocator>();

            viewLocator.Bind<GenericSelectorDialogViewModel<ICreatureTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<IGameObjectTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<IQuestTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<SpellMiniEntry>, GenericSelectorDialogView>();

            viewLocator.Bind<InputEntryProviderViewModel<uint>, InputEntryProviderView>();
            viewLocator.Bind<InputEntryProviderViewModel<string>, InputEntryProviderView>();
            
            viewLocator.Bind<LongItemFromListProviderViewModel, ItemFromListProviderView>();
            viewLocator.Bind<StringItemFromListProviderViewModel, ItemFromListProviderView>();
            viewLocator.Bind<FloatItemFromListProviderViewModel, ItemFromListProviderView>();
        }
    }
}