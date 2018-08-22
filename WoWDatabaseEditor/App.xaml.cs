using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity.Lifetime;
using Unity.RegistrationByConvention;
using WDE.Common;
using WDE.Common.Attributes;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Windows;
using WDE.DbcStore;
using WDE.History;
using WDE.HistoryWindow;
using WDE.MySqlDatabase;
using WDE.Parameters;
using WDE.SmartScriptEditor;
using WDE.Solutions;
using WDE.Solutions.Manager;
using WDE.SQLEditor;
using WoWDatabaseEditor.Events;
using WoWDatabaseEditor.Managers;
using WoWDatabaseEditor.Services.ConfigurationService;
using WoWDatabaseEditor.Services.NewItemService;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<SplashScreenView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IWindowManager, WindowManager>();
            containerRegistry.RegisterSingleton<ISolutionEditorManager, SolutionEditorManager>();
            containerRegistry.Register<IWindowProvider, SolutionEditorManager>("SolutionExplorer");
        }

        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterRequiredTypes(containerRegistry);
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule(typeof(MySqlDatabaseModule));

            moduleCatalog.AddModule(typeof(HistoryModule));
            moduleCatalog.AddModule(typeof(ParametersModule));
            moduleCatalog.AddModule(typeof(DbcStoreModule));
            moduleCatalog.AddModule(typeof(SmartScriptModule));
            moduleCatalog.AddModule(typeof(SqlEditorModule));
            moduleCatalog.AddModule(typeof(SolutionsModule));

            moduleCatalog.AddModule(typeof(HistoryWindowModule));

            Container.GetContainer().RegisterTypes(AllClasses.FromLoadedAssemblies().Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true)), WithMappings.FromMatchingInterface, WithName.Default, t => new ContainerControlledLifetimeManager());

        }
        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        protected override void OnInitialized()
        {
            // have no idea if it makes sense, but works
            var mainWindow = Container.Resolve<MainWindow>();

            var eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<AllModulesLoaded>().Publish();

            mainWindow.ShowDialog();
            Current.Shutdown();
        }
    }
}
