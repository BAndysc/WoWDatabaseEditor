using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using ReactiveUI.Avalonia;
using WDE.CMaNGOS;
using WDE.CMangosConditions;
using WDE.CMangosConditions.Avalonia;
using WDE.Common.Avalonia;
using WDE.Common.Tasks;
using WDE.CommonViews.Avalonia;
using WDE.Conditions;
using WDE.DatabaseEditors;
using WDE.DatabaseEditors.Avalonia;
using WDE.DbcStore;
using WDE.DbScriptsEditor;
using WDE.DbScriptsEditor.Avalonia;
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
using WDE.WorldStateExpressions;
using WDE.WorldStateExpressions.Avalonia;
using WoWDatabaseEditorCore.Avalonia;

[assembly: SupportedOSPlatform("browser")]

namespace LoaderAvalonia.Web;

internal partial class Program
{
    static System.Type[] modules = new System.Type[]
    {
        typeof(CommonAvaloniaModule),
        typeof(CommonViewsModule),
        // typeof(ConditionsModule),
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
        typeof(DbScriptsModule),
        typeof(DbScriptsAvaloniaModule),
        typeof(CMaNGOSModule),
        typeof(CMangosConditionsAvaloniaModule),
        typeof(CMangosConditionsModule),
        typeof(WorldStateExpressionsModule),
        typeof(WorldStateExpressionsAvaloniaModule),
        typeof(SessionsModule),
        typeof(SolutionsModule),
        typeof(QueryGeneratorModule),
        // typeof(TrinityModule),
        // typeof(SmartScriptModule),
        typeof(WebModule),
        typeof(LootEditorModule),
        // typeof(EventScriptsModule),
        typeof(HttpDatabaseModule),
        // typeof(ProfilesModule),
        typeof(SqlEditorModule)
    };
        
    private static async Task Main(string[] args)
    {
        GlobalApplication.Arguments.Init(args);
        await BuildAvaloniaApp()
            .WithInterFont()
            .UseReactiveUI(_ => { })
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}