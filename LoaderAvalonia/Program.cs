using Avalonia;
using Avalonia.ReactiveUI;
using BaseDesktopLoader;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using WDE.AzerothCore;
using WDE.CMaNGOS;
using WDE.CMMySqlDatabase;
using WDE.Common.Avalonia;
using WDE.CommonViews.Avalonia;
using WDE.Conditions;
using WDE.Conditions.Avalonia;
using WDE.DatabaseEditors;
using WDE.DatabaseEditors.Avalonia;
using WDE.DbcStore;
using WDE.History;
using WDE.MapRenderer;
using WDE.MPQ;
using WDE.MySqlDatabaseCommon;
using WDE.PacketViewer;
using WDE.PacketViewer.Avalonia;
using WDE.Parameters;
using WDE.RemoteSOAP;
using WDE.Sessions;
using WDE.SmartScriptEditor.Avalonia;
using WDE.Solutions;
using WDE.SourceCodeIntegrationEditor;
using WDE.Spells;
using WDE.SQLEditor;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WDE.TrinitySmartScriptEditor;
using WDE.Updater;
using WDE.WorldMap;
using WDE.WoWHeadConnector;
using WDE.AnniversaryInfo;
using WDE.DatabaseDefinitionEditor;
using WDE.Debugger;
using WDE.EventAiEditor.Avalonia;
using WDE.EventScriptsEditor;
using WDE.MangosEventAiEditor;
using WDE.MapSpawns;
using WDE.PathPreviewTool;
using WDE.FirstTimeWizard;
using WDE.LootEditor;
using WDE.QueryGenerators;
using WDE.Profiles;
using WDE.SqlWorkbench;
using WoWDatabaseEditorCore.Avalonia;

namespace LoaderAvalonia
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var modules = new System.Type[]
            {
                typeof(CommonAvaloniaModule),
                typeof(CommonViewsModule),
                typeof(ConditionsModule),
                typeof(ConditionsAvaloniaModule),
                typeof(DatabaseEditorsModule),
                typeof(DatabaseEditorsAvaloniaModule),
                typeof(MapRendererModule),
                typeof(MpqModule),
                typeof(MySqlDatabaseCommonModule),
                typeof(PacketViewerModule),
                typeof(PacketViewerAvaloniaModule),
                typeof(ParametersModule),
                typeof(RemoteSOAPModule),
                typeof(SmartScriptAvaloniaModule),
                typeof(SolutionsModule),
                typeof(SourceCodeIntegrationEditorModule),
                typeof(SpellsModule),
                typeof(SqlEditorModule),
                typeof(SqlInterpreterModule),
                typeof(UpdaterModule),
                typeof(WorldMapModule),
                typeof(WoWHeadConnectorModule),
                typeof(DbcStoreModule),
                typeof(HistoryModule),
                typeof(TrinityMySqlDatabaseModule),
                typeof(SmartScriptModule),
                typeof(TrinityModule),
                typeof(AzerothModule),
                typeof(CMaNGOSModule),
                typeof(CMMySqlDatabaseModule),
                typeof(AnniversaryModule),
                typeof(EventScriptsModule),
                typeof(MapSpawnsModule),
                typeof(PathPreviewToolModule),
                typeof(EventAiModule),
                typeof(EventAiAvaloniaModule),
                typeof(QueryGeneratorModule),
                typeof(SessionsModule),
                typeof(FirstTimeWizardModule),
                typeof(ProfilesModule),
                typeof(DatabaseDefinitionEditorModule),
                typeof(LootEditorModule),
                typeof(SqlWorkbenchModule),
                typeof(DebuggerModule)
            };
            BaseProgramLoader.Main(modules, args);
        }

        // Avalonia configuration, don't remove; only used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var configuration = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToTrace();

            return configuration;
        }
    }
}