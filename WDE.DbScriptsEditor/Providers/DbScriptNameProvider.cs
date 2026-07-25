using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DbScriptNameProvider : ISolutionNameProvider<DbScriptSolutionItem>,
        ISolutionNameProviderAsync<DbScriptSolutionItem>
    {
        private readonly IParameterFactory parameterFactory;

        public DbScriptNameProvider(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }

        public string GetName(DbScriptSolutionItem item)
        {
            return BuildName(DbScriptTypes.GetInfo(item.ScriptType), item.ScriptId, parameterFactory);
        }

        public static string BuildName(DbScriptTypeInfo info, uint scriptId, IParameterFactory parameterFactory)
        {
            var parameter = parameterFactory.Factory(info.IdPicker);
            // async-only pickers (e.g. GO by guid) render a placeholder synchronously
            if (parameter is IAsyncParameter<long>)
                return $"{info.ReadableName} {scriptId}";
            return Format(info, scriptId, parameter.ToString(scriptId));
        }

        public async Task<string> GetNameAsync(DbScriptSolutionItem item)
        {
            var info = DbScriptTypes.GetInfo(item.ScriptType);
            var parameter = parameterFactory.Factory(info.IdPicker);
            var name = parameter is IAsyncParameter<long> asyncParameter
                ? await asyncParameter.ToStringAsync(item.ScriptId, default)
                : parameter.ToString(item.ScriptId);
            return Format(info, item.ScriptId, name);
        }

        private static string Format(DbScriptTypeInfo info, uint scriptId, string name)
        {
            if (string.IsNullOrEmpty(name) || name == scriptId.ToString())
                return $"{info.ReadableName} {scriptId}";
            // most pickers already embed the id ("Name (123)" or "123 (Name - entry)")
            if (name.Contains(scriptId.ToString()))
                return $"{info.ReadableName}: {name}";
            return $"{info.ReadableName}: {name} ({scriptId})";
        }
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptIconProvider : ISolutionItemIconProvider<DbScriptSolutionItem>
    {
        public ImageUri GetIcon(DbScriptSolutionItem item) => new("Icons/document_event_script.png");
    }
}
