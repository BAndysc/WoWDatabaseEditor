using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // A maximal run of consecutive steps sharing the same nonzero condition id, rendered in the
    // editor as one "if <condition>" block. Neutral rows (waits, comments) strictly between two
    // member steps belong to the block; leading/trailing neutrals don't. Start/End are inclusive
    // row indices. Grouping is presentation only — the condition id stays a per-step column.
    public readonly record struct DbScriptConditionBlock(int Start, int End, long ConditionId);

    public static class DbScriptConditionBlocks
    {
        // rows: per row (is it a step, its condition id — meaningful only for steps).
        public static List<DbScriptConditionBlock> Compute(IReadOnlyList<(bool IsStep, long ConditionId)> rows)
        {
            var blocks = new List<DbScriptConditionBlock>();
            var i = 0;
            while (i < rows.Count)
            {
                if (!rows[i].IsStep || rows[i].ConditionId <= 0)
                {
                    i++;
                    continue;
                }
                var cond = rows[i].ConditionId;
                var last = i;
                for (var k = i + 1; k < rows.Count; k++)
                {
                    if (!rows[k].IsStep)
                        continue; // a wait/comment doesn't end the run — unless no member follows
                    if (rows[k].ConditionId != cond)
                        break;
                    last = k;
                }
                blocks.Add(new DbScriptConditionBlock(i, last, cond));
                i = last + 1;
            }
            return blocks;
        }
    }
}
