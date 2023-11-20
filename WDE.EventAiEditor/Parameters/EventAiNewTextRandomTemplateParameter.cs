using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Parameters;

public class EventAiNewTextRandomTemplateParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
{
    private readonly IMangosDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;

    public EventAiNewTextRandomTemplateParameter(IMangosDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val <= 0)
            return ToString(val);

        var result = await databaseProvider.GetScriptRandomTemplates((uint)val, IMangosDatabaseProvider.RandomTemplateType.Text);

        if (result == null || result.Count == 0)
            return $"Say random text from template [p=2]{val}[/p] (missing template!)";

        if (result.Count == 1)
            return await GetBroadcastTextAsync(result[0].Value);
        
        List<IBroadcastText> texts = new List<IBroadcastText>();
        foreach (var text in result)
        {
            var bCast = await databaseProvider.GetBroadcastTextByIdAsync((uint)text.Value);
            if (bCast != null)
                texts.Add(bCast);
        }
        
        if (texts.Count == 0)
            return $"Say random text from template [p=2]{val}[/p] (missing template texts!)";

        var chatType = GetTextTypeByChatId(texts[0].ChatTypeId);

        var strings = texts
            .Where(t => t.Text != null || t.Text1 != null)
            .Take(2)
            .Select(t => t.FirstText()!.TrimToLength(40) + $" ({t.Id})")
            .Select(t => $"[p=2]{t.TrimToLength(40)}[/p]");

        var orMore = texts.Count >= 3 ? $" or {texts.Count - 2} more" : "";
        
        return $"{chatType} random text: {string.Join(", ", strings)}{orMore} from template [p=2]{val}[/p]";
    }

    private async Task<string> GetBroadcastTextAsync(long id)
    {
        var result = await databaseProvider.GetBroadcastTextByIdAsync((uint)id);
        var text = result?.FirstText();
        if (text == null || result == null)
            return ToString(id);

        text = text.TrimToLength(60);
        return $"{GetTextTypeByChatId(result.ChatTypeId)} [p=2]{text}[/p]";
    }

    private string GetTextTypeByChatId(int chatId)
    {
        switch (chatId)
        {
            case 0:
                return $"Say";
            case 1:
                return $"Yell";
            case 2:
                return $"Text emote";
            case 3:
                return $"Boss emote";
            case 4:
                return $"Whisper";
            case 5:
                return $"Boss whisper";
            case 6:
                return $"Yell in zone";
            case 7:
                return $"Text emote in zone";
        }

        return "Say";
    }

    public string ToString(long value)
    {
        if (value == 0)
            return "";
        return $"Say random text from template [p=2]{value}[/p]";
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable("dbscript_random_templates"), default, "id", value, null, "type = 0");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;

    public Dictionary<long, SelectOption>? Items => null;
}