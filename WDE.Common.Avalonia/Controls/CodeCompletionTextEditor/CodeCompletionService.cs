using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using Newtonsoft.Json;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Controls.CodeCompletionTextEditor;

[AutoRegister]
[SingleInstance]
public class CodeCompletionService : ICodeCompletionService, IGlobalAsyncInitializer
{
    private readonly IRuntimeDataService runtimeDataService;

    public class TraceSchema
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("keys")]
        public Dictionary<string, string> Keys { get; set; } = new();
    }

    private Dictionary<string, TraceSchema> schemata = new();

    public CodeCompletionService(IRuntimeDataService runtimeDataService)
    {
        this.runtimeDataService = runtimeDataService;
    }

    public async Task Initialize()
    {
        var files = await runtimeDataService.GetAllFiles("CodeCompletionData", "*.json");
        foreach (var file in files)
        {
            try
            {
                var schema = await runtimeDataService.ReadAllText(file);
                var deserialized = JsonConvert.DeserializeObject<TraceSchema[]>(schema);
                if (deserialized != null)
                {
                    foreach (var traceSchema in deserialized)
                    {
                        if (schemata.ContainsKey(traceSchema.Name))
                            LOG.LogWarning(LOG.NonCriticalInvalidStateEventId, "Duplicate schema name: " + traceSchema.Name + ". Ignoring the old one.");
                        schemata[traceSchema.Name] = traceSchema;
                    }
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }
    }

    public IReadOnlyList<(string property, string type)>? GetCompletions(string? rootKey, ITextSource str, int position)
    {
        position--;
        List<string> callChain = new();
        var lastWord = str.GetLastWord(position, out var startPosition);
        if (lastWord.Length == 0 && startPosition > 0 && str.GetCharAt(startPosition - 1) != '.')
            return null;

        callChain.Add(lastWord);

        while (startPosition > 0 && str.GetCharAt(startPosition - 1) == '.')
        {
            startPosition -= 2;
            lastWord = str.GetLastWord(startPosition, out startPosition);
            callChain.Add(lastWord);
        }

        if (rootKey == null || !schemata.TryGetValue(rootKey, out var schema))
            schema = new TraceSchema();

        for (int i = callChain.Count - 1; i > 0; --i)
        {
            var part = callChain[i];

            bool partHasArrayAccess = RemoveArrayAccess(ref part);

            if (!schema.Keys.TryGetValue(part, out var nextSchemaName))
            {
                if (i == callChain.Count - 1 && part is "this" or "$")
                    nextSchemaName = rootKey;
                else
                   return null;
            }

            var nextSchemaIsMap = TryGetMap(nextSchemaName!, out var mapValue);
            if (partHasArrayAccess && nextSchemaIsMap)
                nextSchemaName = mapValue;
            var nextSchemaIsArray = nextSchemaName!.EndsWith("[]");

            if (partHasArrayAccess && nextSchemaIsArray)
                nextSchemaName = nextSchemaName.Substring(0, nextSchemaName.Length - 2);

            if (!partHasArrayAccess && nextSchemaIsArray)
                return new (string property, string type)[] { ("Count", "integer") };

            if (!schemata.TryGetValue(nextSchemaName, out var nextSchema))
                return null;
            schema = nextSchema;
        }

        // on an exact match, don't offer completions
        if (schema.Keys.ContainsKey(callChain[0]))
            return null;

        var keys = schema.Keys.Where(pair =>
        {
            return pair.Key.StartsWith(callChain[0]);
        });

        var outputList = keys.Select(pair => (pair.Key, pair.Value)).ToList();
        if (rootKey != null && callChain.Count == 1 && "this".StartsWith(callChain[0]))
            outputList.Add(("this", rootKey));
        return outputList;
    }

    private bool TryGetMap(string typeName, out string valueTypeName)
    {
        if (typeName.StartsWith("map<"))
        {
            var end = typeName.IndexOf('>');
            if (end == -1)
            {
                valueTypeName = "";
                return false;
            }

            var comma = typeName.IndexOf(',');
            if (comma == -1)
            {
                valueTypeName = "";
                return false;
            }

            valueTypeName = typeName.Substring(comma + 1, end - comma - 1).Trim();
            return true;
        }
        else
        {
            valueTypeName = "";
            return false;
        }
    }

    private bool RemoveArrayAccess(ref string part)
    {
        var start = part.IndexOf('[');
        if (start == -1)
            return false;

        part = part.Substring(0, start);
        return true;
    }
}