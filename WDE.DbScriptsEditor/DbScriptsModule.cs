using Prism.Ioc;
using SmartFormat;
using WDE.Common.Services.QueryParser;
using WDE.DbScriptsEditor.Providers;
using WDE.Module;
using WDE.Module.Attributes;

[assembly: ModuleRequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]

namespace WDE.DbScriptsEditor
{
    public class DbScriptsModule : ModuleBase
    {
        public DbScriptsModule()
        {
            Smart.Default.Parser.UseAlternativeEscapeChar();
        }

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            containerRegistry.Register(typeof(IQueryParserProvider), typeof(DbScriptQueryParser), nameof(DbScriptQueryParser));
        }
    }
}
