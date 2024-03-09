using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module;
using WoWDatabaseEditorCore.Avalonia.Services.CreatureEntrySelectorService;
using WDE.Common.Windows;
using WoWDatabaseEditorCore.Avalonia.Controls.LogViewer;
using WoWDatabaseEditorCore.Avalonia.Managers;
using WoWDatabaseEditorCore.Avalonia.Services.InputEntryProviderService;
using WoWDatabaseEditorCore.Avalonia.Services.ItemFromListSelectorService;
using WoWDatabaseEditorCore.Avalonia.Views;
using WoWDatabaseEditorCore.Services.InputEntryProviderService;
using WoWDatabaseEditorCore.Services.ItemFromListSelectorService;
using WoWDatabaseEditorCore.Services.LogService.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class MainModuleAvalonia : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            var viewLocator = containerProvider.Resolve<IViewLocator>();

            viewLocator.Bind<InputEntryProviderViewModel<uint>, InputEntryProviderView>();
            viewLocator.Bind<InputEntryProviderViewModel<string>, InputEntryProviderView>();
            
            viewLocator.Bind<LongItemFromListProviderViewModel, ItemFromListProviderView>();
            viewLocator.Bind<StringItemFromListProviderViewModel, ItemFromListProviderView>();
            viewLocator.Bind<FloatItemFromListProviderViewModel, ItemFromListProviderView>();
            viewLocator.Bind<WindowViewModelDocumentWrapper, WindowViewDocumentWrapper>();
            viewLocator.BindToolBar<WindowViewModelDocumentWrapper, WindowToolbarDocumentWrapper>();
            viewLocator.Bind<LogViewerControlViewModel,  LogViewerControl>();
        }
    }
}