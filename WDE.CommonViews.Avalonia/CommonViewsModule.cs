using WDE.Common.Windows;
using WDE.CommonViews.Avalonia.DatabaseEditors;
using WDE.CommonViews.Avalonia.DbcStore.Views;
using WDE.CommonViews.Avalonia.History.Views;
using WDE.CommonViews.Avalonia.Parameters.Views;
using WDE.CommonViews.Avalonia.RemoteSOAP.Views;
using WDE.CommonViews.Avalonia.Solutions.Explorer.Views;
using WDE.CommonViews.Avalonia.SQLEditor.Views;
using WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Tools;
using WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Views;
using WDE.CommonViews.Avalonia.Updater.Views;
using WDE.DatabaseEditors.Tools;
using WDE.DbcStore.ViewModels;
using WDE.HistoryWindow.ViewModels;
using WDE.Module;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.Parameters.ViewModels;
using WDE.RemoteSOAP.ViewModels;
using WDE.Solutions.Explorer.ViewModels;
using WDE.SQLEditor.ViewModels;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.Updater.ViewModels;

namespace WDE.CommonViews.Avalonia
{
    public class CommonViewsModule : ModuleBase
    {
        public override void RegisterViews(IViewLocator viewLocator)
        {
            // database
            viewLocator.Bind<DatabaseConfigViewModel, DatabaseConfigView>();
            viewLocator.Bind<WDE.SkyFireMySqlDatabase.ViewModels.DatabaseConfigViewModel, DatabaseConfigView>();
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
            viewLocator.BindToolBar<SqlEditorViewModel, SqlEditorToolBar>();
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