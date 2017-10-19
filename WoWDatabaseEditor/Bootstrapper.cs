using System;
using Microsoft.Practices.Unity;
using Prism.Unity;
using System.Windows;
using Prism.Events;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Managers;
using WDE.DbcStore;
using WDE.History;
using WDE.MySqlDatabase;
using WDE.Parameters;
using WDE.SmartScriptEditor;
using WDE.Solutions;
using WDE.SQLEditor;
using WoWDatabaseEditor.Managers;
using MainWindow = WoWDatabaseEditor.Views.MainWindow;

namespace WoWDatabaseEditor
{
    class Bootstrapper : UnityBootstrapper
    {
        private IModuleCatalog categoryModuleCatalog;


        protected override DependencyObject CreateShell()
        {
            Container.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ISolutionEditorManager, SolutionEditorManager>(new ContainerControlledLifetimeManager());

            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {

        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            Application.Current.MainWindow.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            categoryModuleCatalog = base.CreateModuleCatalog();

            AddModule(typeof(HistoryModule));
            AddModule(typeof(ParametersModule));
            AddModule(typeof(DbcStoreModule));
            AddModule(typeof(MySqlDatabaseModule));
            AddModule(typeof(SmartScriptModule));
            AddModule(typeof(SqlEditorModule));
            AddModule(typeof(SolutionsModule));

            return categoryModuleCatalog;
        }

        private void AddModule(Type type)
        {
            categoryModuleCatalog.AddModule(new ModuleInfo(type.ToString(), type.AssemblyQualifiedName)
            {
                InitializationMode = InitializationMode.WhenAvailable,
                Ref = type.Assembly.CodeBase,
            });
        }

        protected override void ConfigureContainer()
        {
            Container.RegisterTypes(
            AllClasses.FromLoadedAssemblies(),
            WithMappings.FromMatchingInterface,
            WithName.Default);
            base.ConfigureContainer();
        }
    }
}
