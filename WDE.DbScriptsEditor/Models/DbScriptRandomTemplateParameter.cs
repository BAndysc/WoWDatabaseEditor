using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace WDE.DbScriptsEditor.Models
{
    // A dbscript_random_templates id carried in a step column: TALK's datalong (type 0, values are
    // broadcast_text ids) or START_RELAY_SCRIPT's datalong2 (type 1, values are relay script ids).
    // The async display resolves the template values so the sentence shows what the core will
    // actually pick from; the "..." picker browses (and lets the user add) templates of the right
    // type in the table editor and returns the picked row's id.
    public abstract class DbScriptRandomTemplateBaseParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
    {
        protected readonly IMangosDatabaseProvider databaseProvider;
        private readonly ITableEditorPickerService tableEditorPickerService;
        private readonly IMangosDatabaseProvider.RandomTemplateType type;

        protected DbScriptRandomTemplateBaseParameter(IMangosDatabaseProvider databaseProvider,
            ITableEditorPickerService tableEditorPickerService,
            IMangosDatabaseProvider.RandomTemplateType type)
        {
            this.databaseProvider = databaseProvider;
            this.tableEditorPickerService = tableEditorPickerService;
            this.type = type;
        }

        protected abstract string Noun { get; }

        public string ToString(long value) => value == 0 ? "(no random template)" : $"{Noun} from template {value}";

        public async Task<string> ToStringAsync(long value, CancellationToken token)
        {
            if (value <= 0 || value >= uint.MaxValue)
                return ToString(value);

            var values = await databaseProvider.GetScriptRandomTemplates((uint)value, type);
            if (values == null || values.Count == 0)
                return $"{Noun} from template {value} (missing template!)";
            return await FormatValues(value, values, token);
        }

        protected abstract Task<string> FormatValues(long id, IReadOnlyList<IDbScriptRandomTemplate> values, CancellationToken token);

        // chance 0 rows are equally likely among themselves; explicitly chanced rows roll first
        protected static string ChanceSuffix(IDbScriptRandomTemplate row) => row.Chance > 0 ? $", {row.Chance}%" : "";

        public async Task<(long, bool)> PickValue(long value)
        {
            var result = await tableEditorPickerService.PickByColumn(
                DatabaseTable.WorldTable("dbscript_random_templates"), null, "id", value, null,
                $"`type` = {(int)type}");
            if (result.HasValue)
                return (result.Value, true);
            return (0, false);
        }

        public string? Prefix => null;
        public bool HasItems => true;
        public bool AllowUnknownItems => true;
        public Dictionary<long, SelectOption>? Items => null;
    }

    // TALK's random template (type 0): shows the broadcast texts the core will randomly say.
    public class DbScriptStringRandomTemplateParameter : DbScriptRandomTemplateBaseParameter
    {
        public DbScriptStringRandomTemplateParameter(IMangosDatabaseProvider databaseProvider,
            ITableEditorPickerService tableEditorPickerService)
            : base(databaseProvider, tableEditorPickerService, IMangosDatabaseProvider.RandomTemplateType.Text)
        {
        }

        protected override string Noun => "random text";

        protected override async Task<string> FormatValues(long id, IReadOnlyList<IDbScriptRandomTemplate> values, CancellationToken token)
        {
            var texts = new List<(IDbScriptRandomTemplate row, string text)>();
            foreach (var row in values)
            {
                var broadcast = row.Value > 0 ? await databaseProvider.GetBroadcastTextByIdAsync((uint)row.Value) : null;
                var text = broadcast?.FirstText();
                if (text != null)
                    texts.Add((row, text));
            }

            if (texts.Count == 0)
                return $"random text from template {id} (missing template texts!)";

            var shown = texts
                .Take(2)
                .Select(t => $"\"{t.text.TrimToLength(40)}\" ({t.row.Value}{ChanceSuffix(t.row)})");
            var orMore = texts.Count > 2 ? $" or {texts.Count - 2} more" : "";
            return $"random text: {string.Join(", ", shown)}{orMore} (template {id})";
        }
    }

    // START_RELAY_SCRIPT's random template (type 1): shows the relay script ids the core picks from.
    public class DbScriptRelayRandomTemplateParameter : DbScriptRandomTemplateBaseParameter
    {
        public DbScriptRelayRandomTemplateParameter(IMangosDatabaseProvider databaseProvider,
            ITableEditorPickerService tableEditorPickerService)
            : base(databaseProvider, tableEditorPickerService, IMangosDatabaseProvider.RandomTemplateType.RelayScript)
        {
        }

        protected override string Noun => "random relay script";

        protected override Task<string> FormatValues(long id, IReadOnlyList<IDbScriptRandomTemplate> values, CancellationToken token)
        {
            // a 0 value is a valid "do nothing" outcome in the core, keep it visible
            var shown = values
                .Take(4)
                .Select(v => v.Value == 0 ? $"nothing (0{ChanceSuffix(v)})" : $"{v.Value}{ChanceSuffix(v)}");
            var orMore = values.Count > 4 ? $" or {values.Count - 4} more" : "";
            return Task.FromResult($"random relay script: {string.Join(", ", shown)}{orMore} (template {id})");
        }
    }
}
