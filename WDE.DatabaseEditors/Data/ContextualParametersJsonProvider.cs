using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data;

[SingleInstance]
[AutoRegister]
public class ContextualParametersJsonProvider : IContextualParametersJsonProvider
{
    private readonly IRuntimeDataService runtimeDataService;

    public ContextualParametersJsonProvider(IRuntimeDataService runtimeDataService)
    {
        this.runtimeDataService = runtimeDataService;
    }

    public async Task<IEnumerable<(string file, string content)>> GetParameters()
    {
        var files = await runtimeDataService.GetAllFiles("DatabaseContextualParameters/", "*.json");
        List<(string file, string content)> result = new();
        foreach (var file in files)
        {
            result.Add((file, await runtimeDataService.ReadAllText(file)));
        }

        return result;
    }
}