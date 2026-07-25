using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WDE.Common.Database;

namespace WDE.MangosEventAiEditor.Acid
{
    /// <summary>
    /// Text-level patcher for cmangos ACID .sql files (the big `creature_ai_scripts` inserts).
    /// Replicates the way cmangos commits edit the file: a creature's rows are replaced in place,
    /// new creatures are inserted in creature_id order together with a `-- Name entry` header comment,
    /// removed scripts also remove the header comment. Everything else is preserved byte for byte.
    /// The file is ~5 MB, so this is a single-pass line scan + a single StringBuilder emit.
    /// </summary>
    public static class AcidFilePatcher
    {
        // ('224001','2240', ...........),   or   ..........);
        private static readonly Regex RowRegex = new(@"^\('(-?\d+)'\s*,\s*'(-?\d+)',.*\)[,;]$", RegexOptions.Compiled);

        private class Node
        {
            public string Text = "";     // raw line for non-rows; for rows: the row without the trailing , or ;
            public bool IsRow;
            public long Id;
            public long CreatureId;
            public bool IsCasInsertHeader;
            public bool IsOtherInsertHeader;
            public string? OriginalLine;  // for pre-existing rows: the raw line, emitted verbatim when the terminator doesn't change
            public bool OriginallyClosed; // pre-existing row ended with );
        }

        /// <summary>
        /// Returns the patched file content or null (with <paramref name="error"/> set) when the file
        /// doesn't look like an ACID file. <paramref name="lines"/> empty means "delete the script".
        /// </summary>
        public static string? UpdateCreatureScript(string content,
            long creatureEntry,
            string? creatureName,
            IReadOnlyList<IEventAiLine> lines,
            out string? error)
        {
            error = null;
            var newLine = content.Contains("\r\n") ? "\r\n" : "\n";
            var rawLines = content.Split(new[] { newLine }, StringSplitOptions.None);

            var nodes = ParseNodes(rawLines);

            if (!nodes.Any(n => n.IsCasInsertHeader))
            {
                error = "No INSERT INTO `creature_ai_scripts` statement found in the ACID file";
                return null;
            }

            var newRowNodes = lines
                .OrderBy(l => l.Id)
                .Select(l => new Node { Text = SerializeRow(l), IsRow = true, Id = l.Id, CreatureId = l.CreatureIdOrGuid })
                .ToList();

            var existing = new List<int>();
            for (int i = 0; i < nodes.Count; ++i)
                if (nodes[i].IsRow && nodes[i].CreatureId == creatureEntry)
                    existing.Add(i);

            if (existing.Count > 0)
                ReplaceExisting(nodes, existing, newRowNodes, creatureEntry, creatureName);
            else if (newRowNodes.Count > 0)
                InsertNew(nodes, newRowNodes, creatureEntry, creatureName);
            else
                return content; // nothing to delete, nothing to add

            return Emit(nodes, newLine, content.Length + 4096);
        }

        private static List<Node> ParseNodes(string[] rawLines)
        {
            var nodes = new List<Node>(rawLines.Length + 16);
            bool inCas = false, inOther = false;
            foreach (var line in rawLines)
            {
                var trimmed = line.TrimEnd();
                if (trimmed.StartsWith("INSERT INTO `creature_ai_scripts`", StringComparison.OrdinalIgnoreCase))
                {
                    nodes.Add(new Node { Text = line, IsCasInsertHeader = true });
                    inCas = true;
                    inOther = false;
                    continue;
                }
                if (trimmed.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase))
                {
                    nodes.Add(new Node { Text = line, IsOtherInsertHeader = true });
                    inCas = false;
                    inOther = true;
                    continue;
                }
                if (inCas && trimmed.StartsWith('('))
                {
                    var m = RowRegex.Match(trimmed);
                    if (m.Success)
                    {
                        bool closes = trimmed.EndsWith(';');
                        nodes.Add(new Node
                        {
                            Text = trimmed[..^1],
                            IsRow = true,
                            Id = long.Parse(m.Groups[1].Value),
                            CreatureId = long.Parse(m.Groups[2].Value),
                            OriginalLine = line,
                            OriginallyClosed = closes
                        });
                        if (closes)
                            inCas = false;
                        continue;
                    }
                }
                else if (inOther && trimmed.StartsWith('(') && trimmed.EndsWith(");"))
                    inOther = false;

                nodes.Add(new Node { Text = line });
            }
            return nodes;
        }

        private static void ReplaceExisting(List<Node> nodes, List<int> existing, List<Node> newRowNodes,
            long creatureEntry, string? creatureName)
        {
            int firstIdx = existing[0];
            for (int i = existing.Count - 1; i >= 0; --i)
                nodes.RemoveAt(existing[i]);

            if (newRowNodes.Count > 0)
                nodes.InsertRange(firstIdx, newRowNodes);
            else
            {
                // script deleted - also drop the creature's own header comment(s) directly above,
                // but only comments clearly referring to this creature and never section separators
                int i = firstIdx - 1;
                while (i >= 0 &&
                       IsPlainComment(nodes[i]) &&
                       MentionsCreature(nodes[i].Text, creatureEntry, creatureName))
                {
                    nodes.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void InsertNew(List<Node> nodes, List<Node> newRowNodes, long creatureEntry, string? creatureName)
        {
            // creature blocks are kept sorted by creature_id within each zone section of each INSERT
            // statement (but the ordering restarts per section!), so the right spot is the "sorted gap":
            // an adjacent pair of blocks a < entry < b, preferring the tightest lower neighbour a
            var blocks = new List<(int stmt, long id, int firstRow, int lastRow)>();
            int stmt = -1;
            bool inCas = false;
            long? prevCreature = null;
            for (int i = 0; i < nodes.Count; ++i)
            {
                var n = nodes[i];
                if (n.IsCasInsertHeader)
                {
                    stmt++;
                    inCas = true;
                    prevCreature = null;
                    continue;
                }
                if (n.IsOtherInsertHeader)
                {
                    inCas = false;
                    prevCreature = null;
                    continue;
                }
                if (!n.IsRow || !inCas)
                    continue;
                if (prevCreature != n.CreatureId)
                    blocks.Add((stmt, n.CreatureId, i, i));
                else
                {
                    var b = blocks[^1];
                    b.lastRow = i;
                    blocks[^1] = b;
                }
                prevCreature = n.CreatureId;
            }

            int insertAt = -1;
            long bestLower = long.MinValue;
            long bestUpper = long.MaxValue;
            void ConsiderGap(long lower, long upper, int position)
            {
                if (lower >= creatureEntry || creatureEntry >= upper)
                    return;
                if (lower > bestLower || (lower == bestLower && upper < bestUpper))
                {
                    bestLower = lower;
                    bestUpper = upper;
                    insertAt = position;
                }
            }
            for (int bi = 0; bi < blocks.Count; ++bi)
            {
                var cur = blocks[bi];
                if (bi == 0 || blocks[bi - 1].stmt != cur.stmt)
                    ConsiderGap(long.MinValue, cur.id, HopAboveComments(nodes, cur.firstRow));
                if (bi == blocks.Count - 1 || blocks[bi + 1].stmt != cur.stmt)
                    ConsiderGap(cur.id, long.MaxValue, cur.lastRow + 1);
                else
                    ConsiderGap(cur.id, blocks[bi + 1].id, cur.lastRow + 1);
            }
            if (insertAt == -1)
                insertAt = nodes.FindLastIndex(n => n.IsCasInsertHeader) + 1;

            var block = new List<Node>(newRowNodes.Count + 1);
            block.Add(new Node { Text = $"-- {(string.IsNullOrWhiteSpace(creatureName) ? "Creature" : creatureName)} {creatureEntry}" });
            block.AddRange(newRowNodes);
            nodes.InsertRange(insertAt, block);
        }

        private static int HopAboveComments(List<Node> nodes, int index)
        {
            while (index > 0 && IsPlainComment(nodes[index - 1]))
                index--;
            return index;
        }

        private static string Emit(List<Node> nodes, string newLine, int capacity)
        {
            // reassign row terminators per statement: every row ends with `),` except the statement's last: `);`
            // statement membership is positional (rows belong to the closest creature_ai_scripts INSERT above)
            var lastRowOfStatement = new HashSet<int>();
            var emptyHeaders = new HashSet<int>();
            int currentHeader = -1;
            int currentLastRow = -1;
            void CloseStatement()
            {
                if (currentHeader == -1)
                    return;
                if (currentLastRow == -1)
                    emptyHeaders.Add(currentHeader); // all rows were deleted - drop the dangling INSERT header
                else
                    lastRowOfStatement.Add(currentLastRow);
            }
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i].IsCasInsertHeader || nodes[i].IsOtherInsertHeader)
                {
                    CloseStatement();
                    currentHeader = nodes[i].IsCasInsertHeader ? i : -1;
                    currentLastRow = -1;
                }
                else if (nodes[i].IsRow)
                    currentLastRow = i;
            }
            CloseStatement();

            var sb = new StringBuilder(capacity);
            bool first = true;
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (emptyHeaders.Contains(i))
                    continue;
                if (!first)
                    sb.Append(newLine);
                first = false;
                if (nodes[i].IsRow)
                {
                    bool closes = lastRowOfStatement.Contains(i);
                    if (nodes[i].OriginalLine != null && nodes[i].OriginallyClosed == closes)
                        sb.Append(nodes[i].OriginalLine);
                    else
                        sb.Append(nodes[i].Text).Append(closes ? ";" : ",");
                }
                else
                    sb.Append(nodes[i].Text);
            }
            return sb.ToString();
        }

        private static bool IsPlainComment(Node node)
        {
            if (node.IsRow || node.IsCasInsertHeader || node.IsOtherInsertHeader)
                return false;
            var t = node.Text.TrimStart();
            if (!t.StartsWith("--"))
                return false;
            var rest = t[2..].TrimStart();
            return rest.Length > 0 && rest[0] != '=' && rest[0] != '|';
        }

        private static bool MentionsCreature(string comment, long entry, string? name)
        {
            if (Regex.IsMatch(comment, $@"\b{entry}\b"))
                return true;
            return !string.IsNullOrWhiteSpace(name) && comment.Contains(name, StringComparison.OrdinalIgnoreCase);
        }

        private static string SerializeRow(IEventAiLine l)
        {
            var sb = new StringBuilder(256);
            sb.Append("('").Append(l.Id);
            void Append(long v) => sb.Append("','").Append(v);
            Append(l.CreatureIdOrGuid);
            Append(l.EventType);
            Append(l.EventInversePhaseMask);
            Append(l.EventChance);
            Append(l.EventFlags);
            Append(l.EventParam1);
            Append(l.EventParam2);
            Append(l.EventParam3);
            Append(l.EventParam4);
            Append(l.EventParam5);
            Append(l.EventParam6);
            Append(l.Action1Type);
            Append(l.Action1Param1);
            Append(l.Action1Param2);
            Append(l.Action1Param3);
            Append(l.Action2Type);
            Append(l.Action2Param1);
            Append(l.Action2Param2);
            Append(l.Action2Param3);
            Append(l.Action3Type);
            Append(l.Action3Param1);
            Append(l.Action3Param2);
            Append(l.Action3Param3);
            sb.Append("','").Append(EscapeComment(l.Comment)).Append("')");
            return sb.ToString();
        }

        private static string EscapeComment(string comment)
        {
            var c = comment.TrimEnd();
            if (c.EndsWith(','))
                c = c[..^1].TrimEnd();
            if (c.EndsWith(" -"))
                c = c[..^2].TrimEnd();
            return c.Replace("\\", "\\\\").Replace("'", "''");
        }
    }
}
