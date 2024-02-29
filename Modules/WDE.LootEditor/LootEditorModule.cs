using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Windows;
using WDE.LootEditor.Configuration;
using WDE.LootEditor.Configuration.Views;
using WDE.LootEditor.Parameters;
using WDE.Module;

namespace WDE.LootEditor;

public class LootEditorModule : ModuleBase
{
    public override void RegisterViews(IViewLocator viewLocator)
    {
        base.RegisterViews(viewLocator);
        viewLocator.Bind<LootEditorConfiguration, LootEditorConfigurationView>();
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
    }

    public override void OnInitialized(IContainerProvider containerProvider)
    {
        base.OnInitialized(containerProvider);
        containerProvider.Resolve<IEventAggregator>()
            .GetEvent<AllModulesLoaded>()
            .Subscribe(() =>
            {
                var parameterFactory = containerProvider.Resolve<IParameterFactory>();
                var lootPicker = containerProvider.Resolve<ILootPickerService>();
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("MinValueOrLootReferenceParameter", new MinValueOrLootReferenceParameter(lootPicker));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootGameObjectParameter", new LootParameter(lootPicker, LootSourceType.GameObject));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootDisenchantParameter", new LootParameter(lootPicker, LootSourceType.Disenchant));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootPickpocketParameter", new LootParameter(lootPicker, LootSourceType.Pickpocketing));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootSkinningParameter", new LootParameter(lootPicker, LootSourceType.Skinning));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootMailParameter", new LootParameter(lootPicker, LootSourceType.Mail));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootSpellParameter", new LootParameter(lootPicker, LootSourceType.Spell));
                
                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootReferenceParameter", new LootParameter(lootPicker, LootSourceType.Reference));

                containerProvider.Resolve<IParameterFactory>()
                    .Register("LootTreasureParameter", new LootParameter(lootPicker, LootSourceType.Treasure));
            }, true);
    }
}