using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Windows;
using WDE.Module;
using WoWDatabaseEditorCore.Services.CreatureEntrySelectorService;
using WoWDatabaseEditorCore.WPF.Services.CreatureEntrySelectorService;

namespace WoWDatabaseEditorCore.WPF
{
    public class MainModuleWPF : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            var viewLocator = containerProvider.Resolve<IViewLocator>();

            viewLocator.Bind<GenericSelectorDialogViewModel<ICreatureTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<IGameObjectTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<IQuestTemplate>, GenericSelectorDialogView>();
            viewLocator.Bind<GenericSelectorDialogViewModel<SpellMiniEntry>, GenericSelectorDialogView>();
        }
    }
}