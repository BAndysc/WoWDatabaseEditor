using WDE.Common.Windows;
using WDE.CommonViews.Avalonia.DatabaseEditors;
using WDE.CommonViews.Avalonia.RemoteSOAP.Views;
using WDE.CommonViews.Avalonia.Updater.Views;
using WDE.DatabaseEditors.Tools;
using WDE.DbcStore.ViewModels;
using WDE.DbcStore.Views;
using WDE.HistoryWindow.ViewModels;
using WDE.HistoryWindow.Views;
using WDE.Module;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.Parameters.ViewModels;
using WDE.Parameters.Views;
using WDE.RemoteSOAP.ViewModels;
using WDE.Solutions.Explorer.ViewModels;
using WDE.Solutions.Explorer.Views;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;
using WDE.TrinityMySqlDatabase.Tools;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.TrinityMySqlDatabase.Views;
using WDE.Updater.ViewModels;

namespace WDE.CommonViews.Avalonia
{
    public class CommonViewsModule : ModuleBase
    {
        public override void RegisterViews(IViewLocator viewLocator)
        {
            // database
            viewLocator.Bind<DatabaseConfigViewModel, DatabaseConfigView>();
            viewLocator.Bind<DebugQueryToolViewModel, DebugQueryToolView>();
            // solutions
            viewLocator.Bind<SolutionExplorerViewModel, SolutionExplorerView>();
            // parameters
            viewLocator.Bind<ParametersViewModel, ParametersView>();
            // history
            viewLocator.Bind<HistoryViewModel, HistoryView>();
            // dbc store
            viewLocator.Bind<DBCConfigViewModel, DBCConfigView>();
            // sql editor
            viewLocator.Bind<SqlEditorViewModel, SqlEditorView>();
            // updater
            viewLocator.Bind<ChangeLogViewModel, ChangeLogView>();
            viewLocator.Bind<UpdaterConfigurationViewModel, UpdaterConfigurationView>();
            // remote soap
            viewLocator.Bind<SoapConfigViewModel, SoapConfigView>();
            // table editor
            viewLocator.Bind<ToolsViewModel, DefinitionToolView>();
        }
    }
}