using WDE.Common.Parameters;
using WDE.Common.Windows;
using WDE.CommonViews.Avalonia.DbcStore.Views;
using WDE.CommonViews.Avalonia.History.Views;
using WDE.CommonViews.Avalonia.Mpq;
using WDE.CommonViews.Avalonia.Parameters;
using WDE.CommonViews.Avalonia.Parameters.Views;
using WDE.CommonViews.Avalonia.RemoteSOAP.Views;
using WDE.CommonViews.Avalonia.Sessions;
using WDE.CommonViews.Avalonia.Solutions.Explorer.Views;
using WDE.CommonViews.Avalonia.SQLEditor.Views;
using WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Tools;
using WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Views;
using WDE.CommonViews.Avalonia.Updater.Views;
using WDE.DbcStore.ViewModels;
using WDE.HistoryWindow.ViewModels;
using WDE.Module;
using WDE.MPQ.ViewModels;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.Parameters.ViewModels;
using WDE.RemoteSOAP.ViewModels;
using WDE.Solutions.Explorer.ViewModels;
using WDE.SQLEditor.ViewModels;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.CMMySqlDatabase.ViewModels;
using WDE.Parameters.QuickAccess;
using WDE.Parameters.Views;
using WDE.Sessions.Sessions;
using WDE.Updater.ViewModels;

namespace WDE.CommonViews.Avalonia
{
    public class CommonViewsModule : ModuleBase
    {
        public override void RegisterViews(IViewLocator viewLocator)
        {
            // database
            viewLocator.Bind<DatabaseConfigViewModel, DatabaseConfigView>();
            viewLocator.Bind<CMDatabaseConfigViewModel, DatabaseConfigView>();
            viewLocator.Bind<DebugQueryToolViewModel, DebugQueryToolView>();
            // solutions
            viewLocator.Bind<SolutionExplorerViewModel, SolutionExplorerView>();
            viewLocator.Bind<SessionToolViewModel, SessionToolView>();
            viewLocator.Bind<SessionsConfigurationViewModel, SessionsConfigurationView>();
            // parameters
            viewLocator.Bind<ParameterSearchConfiguration, SearchConfigurationView>();
            viewLocator.Bind<ParametersViewModel, ParametersView>();
            viewLocator.Bind<StringPickerViewModel, StringPickerView>();
            viewLocator.Bind<UnitBytes1EditorViewModel, UnitBytes1EditorView>();
            viewLocator.Bind<UnitBytes2EditorViewModel, UnitBytes2EditorView>();
            viewLocator.Bind<UnixTimestampEditorViewModel, UnixTimestampEditorView>();
            viewLocator.Bind<MultipleParametersPickerViewModel, MultipleParametersPickerView>();
            viewLocator.BindToolBar<MultipleParametersPickerViewModel, MultipleParametersPickerToolBar>();
            // history
            viewLocator.Bind<HistoryViewModel, HistoryView>();
            // dbc store
            viewLocator.Bind<DBCConfigViewModel, DBCConfigView>();
            // sql editor
            viewLocator.Bind<SqlEditorViewModel, SqlEditorView>();
            viewLocator.BindToolBar<SqlEditorViewModel, SqlEditorToolBar>();
            viewLocator.Bind<CustomQueryEditorViewModel, CustomQueryEditorView>();
            // updater
            viewLocator.Bind<ChangeLogViewModel, ChangeLogView>();
            viewLocator.Bind<UpdaterConfigurationViewModel, UpdaterConfigurationView>();
            // remote soap
            viewLocator.Bind<SoapConfigViewModel, SoapConfigView>();
            //mpq
            viewLocator.Bind<MpqSettingsViewModel, MpqSettingsView>();
        }
    }
}