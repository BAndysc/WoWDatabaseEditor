using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Parameters.ViewModels;

namespace WDE.EventAiEditor.Parameters;

public class EventAiNewTextParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
{
    private readonly IMangosDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;

    public EventAiNewTextParameter(IMangosDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val < 0)
            return ToString(val);
        
        var result = await databaseProvider.GetBroadcastTextByIdAsync((uint)val);
        var text = result?.FirstText();
        if (text == null || result == null)
            return ToString(val);

        text = text.TrimToLength(60);
        return $"{GetTextTypeByChatId(result.ChatTypeId)} [p=0]{text}[/p]";
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
        return $"Say broadcast text [p=0]{value}[/p]";
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable("broadcast_text"), default, "Id", value);
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    
    public Dictionary<long, SelectOption>? Items => null;
}