using UIKit;
using WDE.Common.Avalonia;
using WDE.Common.Tasks;
using WDE.CommonViews.Avalonia;
using WDE.Conditions;
using WDE.DatabaseEditors;
using WDE.DatabaseEditors.Avalonia;
using WDE.DbcStore;
using WDE.EventAiEditor.Avalonia;
using WDE.EventScriptsEditor;
using WDE.History;
using WDE.HttpDatabase;
using WDE.LootEditor;
using WDE.MySqlDatabaseCommon;
using WDE.Parameters;
using WDE.Profiles;
using WDE.QueryGenerators;
using WDE.Sessions;
using WDE.SmartScriptEditor.Avalonia;
using WDE.Solutions;
using WDE.Spells;
using WDE.SQLEditor;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinitySmartScriptEditor;

namespace LoaderAvalonia.iOS;

public class Application
{
    
    static System.Type[] modules = new System.Type[]
    {
        typeof(iOSModule),
        typeof(CommonAvaloniaModule),
        typeof(CommonViewsModule),
        typeof(ConditionsModule),
        typeof(DatabaseEditorsModule),
        typeof(DatabaseEditorsAvaloniaModule),
        typeof(MySqlDatabaseCommonModule),
        typeof(ParametersModule),
        typeof(SmartScriptAvaloniaModule),
        typeof(SpellsModule),
        typeof(SqlInterpreterModule),
        typeof(DbcStoreModule),
        typeof(HistoryModule),
        typeof(EventAiAvaloniaModule),
        typeof(SessionsModule),
        typeof(SolutionsModule),
        typeof(QueryGeneratorModule),
        typeof(TrinityModule),
        typeof(SmartScriptModule),
        typeof(LootEditorModule),
        typeof(EventScriptsModule),
        typeof(HttpDatabaseModule),
        typeof(ProfilesModule),
        typeof(SqlEditorModule)
    };
    
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        WoWDatabaseEditorCore.Avalonia.Program.PreloadedModules = modules;
        GlobalApplication.Arguments.Init(new[]{"--href", "http://192.168.1.106:5000"});
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}