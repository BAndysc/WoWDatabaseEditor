using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Windows;
using WDE.Module;
using WoWDatabaseEditor.Services.CreatureEntrySelectorService;

namespace WoWDatabaseEditor
{
    public class MainModule : ModuleBase
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