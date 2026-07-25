using System.Collections.Generic;
using Newtonsoft.Json;

namespace WDE.DbScriptsEditor.Models
{
    // One clipboard row: an action (full line), a wait (duration) or a comment (text) —
    // rows are equal entities in the editor, so all three copy/paste uniformly.
    public class DbScriptClipboardRow
    {
        public const string ActionType = "action";
        public const string WaitType = "wait";
        public const string CommentType = "comment";
        public const string IfType = "if";

        public string Type { get; set; } = ActionType;
        public AbstractDbScriptLine? Line { get; set; }
        public long Duration { get; set; }
        public string? Text { get; set; }
        public long ConditionId { get; set; }
        // block membership travels with the row, so copying an if block pastes as a block
        public bool InIf { get; set; }
    }

    // Serializes editor rows to/from a clipboard string so they can be copied within and between
    // script documents. Uses a versioned marker so we only ever paste our own payloads.
    public static class DbScriptClipboard
    {
        private const string Marker = "WDE_DBSCRIPT_ROWS_V2:";

        public static string Serialize(IEnumerable<DbScriptRow> rows)
        {
            var dtos = new List<DbScriptClipboardRow>();
            foreach (var row in rows)
            {
                switch (row)
                {
                    case EditableDbScriptStep step:
                        dtos.Add(new DbScriptClipboardRow { Type = DbScriptClipboardRow.ActionType, Line = AbstractDbScriptLine.From(step.ToLine()), InIf = step.InIf });
                        break;
                    case DbScriptWaitRow wait:
                        dtos.Add(new DbScriptClipboardRow { Type = DbScriptClipboardRow.WaitType, Duration = wait.Duration.Value, InIf = wait.InIf });
                        break;
                    case DbScriptCommentRow comment:
                        dtos.Add(new DbScriptClipboardRow { Type = DbScriptClipboardRow.CommentType, Text = comment.Text.Value, InIf = comment.InIf });
                        break;
                    case DbScriptIfRow ifRow:
                        dtos.Add(new DbScriptClipboardRow { Type = DbScriptClipboardRow.IfType, ConditionId = ifRow.ConditionId.Value });
                        break;
                }
            }
            return Marker + JsonConvert.SerializeObject(dtos);
        }

        public static bool TryDeserialize(string? text, out IReadOnlyList<DbScriptClipboardRow> rows)
        {
            rows = System.Array.Empty<DbScriptClipboardRow>();
            if (string.IsNullOrEmpty(text) || !text.StartsWith(Marker, System.StringComparison.Ordinal))
                return false;
            try
            {
                var payload = text.Substring(Marker.Length);
                var parsed = JsonConvert.DeserializeObject<List<DbScriptClipboardRow>>(payload);
                if (parsed == null || parsed.Count == 0)
                    return false;
                rows = parsed;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
