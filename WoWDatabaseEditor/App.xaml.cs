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
using WDE.Blueprints;
using WDE.Common;
using WDE.Common.Attributes;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Windows;
using WDE.DbcStore;
using WDE.History;
using WDE.HistoryWindow;
using WDE.Parameters;
using WDE.SmartScriptEditor;
using WDE.Solutions;
using WDE.Solutions.Manager;
using WDE.SQLEditor;
using WDE.TrinityMySqlDatabase;
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
        private SplashScreenView splash;

        protected override Window CreateShell()
        {
            splash = Container.Resolve<SplashScreenView>();

            splash.Show();

            return splash;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterRequiredTypes(containerRegistry);
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

            containerRegistry.RegisterSingleton<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule(typeof(TrinityMySqlDatabaseModule));

            moduleCatalog.AddModule(typeof(HistoryModule));
            moduleCatalog.AddModule(typeof(ParametersModule));
            moduleCatalog.AddModule(typeof(DbcStoreModule));
            moduleCatalog.AddModule(typeof(SmartScriptModule));
            moduleCatalog.AddModule(typeof(SqlEditorModule));
            moduleCatalog.AddModule(typeof(SolutionsModule));
            moduleCatalog.AddModule(typeof(BlueprintsModule));
            
            moduleCatalog.AddModule(typeof(HistoryWindowModule));
            
            AutoRegisterClasses();
        }

        /*
         * Ok, this method is reaaly ugly. Luckily it is also realy low level
         * so it doesn't affect other parts that much. I don't know how it should be done properly.
         */
        private void AutoRegisterClasses()
        {
            var defaultRegisters = AllClasses.FromLoadedAssemblies().Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true));

            HashSet<Type> alreadyInitialized = new HashSet<Type>();
            foreach (var register in defaultRegisters)
            {
                if (register.IsAbstract)
                    continue;

                var singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

                foreach (var interface_ in register.GetInterfaces())
                {
                    string name = null;

                    if (alreadyInitialized.Contains(interface_))
                        name = register.ToString() + interface_.ToString();
                    else
                        alreadyInitialized.Add(interface_);

                    LifetimeManager life = null;

                    if (singleton)
                        life = new ContainerControlledLifetimeManager();
                    else
                        life = new TransientLifetimeManager();

                    Container.GetContainer().RegisterType(interface_, register, name, life);
                }
            }
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

            splash.Close();

            mainWindow.ShowDialog();
            Current.Shutdown();
        }
    }
}
