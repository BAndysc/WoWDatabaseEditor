using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Providers
{
    public class DbScriptSolutionItemProvider : INumberSolutionItemProvider
    {
        private readonly DbScriptTypeInfo info;
        private readonly IParameterPickerService parameterPickerService;
        private readonly ImageUri icon = new("Icons/document_event_script_big.png");

        public DbScriptSolutionItemProvider(DbScriptTypeInfo info, IParameterPickerService parameterPickerService)
        {
            this.info = info;
            this.parameterPickerService = parameterPickerService;
        }

        public string GetName() => $"Dbscripts {info.ReadableName}";
        public ImageUri GetImage() => icon;
        public string GetDescription() => $"Edit the {info.TableName} script.";
        public string GetGroupName() => "DB Scripts";
        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public string ParameterName => info.IdPicker;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var (value, ok) = await parameterPickerService.PickParameter(info.IdPicker, 0);
            if (!ok)
                return null;
            return new DbScriptSolutionItem(info.Type, (uint)value);
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number) =>
            Task.FromResult<ISolutionItem?>(new DbScriptSolutionItem(info.Type, (uint)number));
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptSolutionItemProviderProvider : ISolutionItemProviderProvider
    {
        private readonly IParameterPickerService parameterPickerService;

        public DbScriptSolutionItemProviderProvider(IParameterPickerService parameterPickerService)
        {
            this.parameterPickerService = parameterPickerService;
        }

        public IEnumerable<ISolutionItemProvider> Provide() =>
            DbScriptTypes.All.Select(info => new DbScriptSolutionItemProvider(info, parameterPickerService));
    }
}
