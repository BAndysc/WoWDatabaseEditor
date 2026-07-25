using System;
using System.Collections.Generic;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;

namespace WDE.CMangosConditions.Models
{
    public class MangosConditionSerializeResult
    {
        /// <summary>Dependency-ordered (children before parents) rows, entries assigned.</summary>
        public List<AbstractMangosConditionLine> Lines { get; } = new();
        /// <summary>Assigned entry of each root, in root order.</summary>
        public List<uint> RootEntries { get; } = new();
    }

    /// <summary>
    /// Converts between flat `conditions` rows (logical types reference children via value1..4)
    /// and the editor's tree of view models, and back.
    /// </summary>
    internal static class MangosConditionTreeCodec
    {
        public const int TypeNot = -3;
        public const int TypeOr = -2;
        public const int TypeAnd = -1;

        public static bool IsLogicalType(int type) => type is TypeNot or TypeOr or TypeAnd;

        public static IEnumerable<uint> ChildRefs(IMangosConditionLine line)
        {
            if (line.ConditionType == TypeNot)
            {
                if (line.Value1 != 0)
                    yield return line.Value1;
            }
            else if (line.ConditionType is TypeOr or TypeAnd)
            {
                if (line.Value1 != 0)
                    yield return line.Value1;
                if (line.Value2 != 0)
                    yield return line.Value2;
                if (line.Value3 != 0)
                    yield return line.Value3;
                if (line.Value4 != 0)
                    yield return line.Value4;
            }
        }

        public static List<MangosConditionViewModel> BuildTree(IReadOnlyList<IMangosConditionLine> lines,
            IMangosConditionsFactory factory)
        {
            var byEntry = new Dictionary<uint, IMangosConditionLine>();
            foreach (var line in lines)
                if (line.ConditionEntry != 0)
                    byEntry.TryAdd(line.ConditionEntry, line);

            var referenced = new HashSet<uint>();
            foreach (var line in lines)
                foreach (var r in ChildRefs(line))
                    referenced.Add(r);

            var roots = new List<MangosConditionViewModel>();
            foreach (var line in lines)
            {
                if (line.ConditionEntry != 0 && referenced.Contains(line.ConditionEntry))
                    continue;
                roots.Add(BuildNode(line, byEntry, factory, new HashSet<uint>()));
            }

            return roots;
        }

        private static MangosConditionViewModel BuildNode(IMangosConditionLine line,
            Dictionary<uint, IMangosConditionLine> byEntry, IMangosConditionsFactory factory, HashSet<uint> path)
        {
            var vm = factory.Create(line);
            if (!IsLogicalType(line.ConditionType))
                return vm;

            if (line.ConditionEntry != 0)
                path.Add(line.ConditionEntry);
            foreach (var childEntry in ChildRefs(line))
            {
                if (path.Contains(childEntry))
                    continue; // broken data: cycle - skip the back edge

                var child = byEntry.TryGetValue(childEntry, out var childLine)
                    ? BuildNode(childLine, byEntry, factory, path)
                    : factory.CreateMissing(childEntry);
                child.Parent = vm;
                vm.Children.Add(child);
            }
            if (line.ConditionEntry != 0)
                path.Remove(line.ConditionEntry);

            return vm;
        }

        /// <summary>
        /// Flattens the tree back to rows. Children are emitted before parents and always get
        /// a lower condition_entry than any parent referencing them (cmangos validates this
        /// at load). Nodes keep their original entry when possible ("edit in place"); nodes
        /// that cannot (new, duplicated entry, or ordering violation) get fresh ids starting
        /// at firstFreeEntry, which must be greater than every entry known to the caller.
        /// Structurally identical rows (same type, values and flags) collapse into one entry,
        /// matching the table's unique key.
        /// </summary>
        public static MangosConditionSerializeResult Serialize(IReadOnlyList<MangosConditionViewModel> roots,
            uint firstFreeEntry)
        {
            var result = new MangosConditionSerializeResult();
            var byContent = new Dictionary<(int, uint, uint, uint, uint, uint), uint>();
            var usedEntries = new HashSet<uint>();
            uint next = firstFreeEntry;

            foreach (var root in roots)
                result.RootEntries.Add(SerializeNode(root, result.Lines, byContent, usedEntries, ref next));

            return result;
        }

        private static uint SerializeNode(MangosConditionViewModel node, List<AbstractMangosConditionLine> lines,
            Dictionary<(int, uint, uint, uint, uint, uint), uint> byContent, HashSet<uint> usedEntries, ref uint next)
        {
            var line = node.ToLine();
            uint maxChildEntry = 0;

            if (node.IsLogical)
            {
                Span<uint> childEntries = stackalloc uint[4];
                int count = 0;
                foreach (var child in node.Children)
                {
                    var childEntry = SerializeNode(child, lines, byContent, usedEntries, ref next);
                    if (count < 4)
                        childEntries[count++] = childEntry;
                    if (childEntry > maxChildEntry)
                        maxChildEntry = childEntry;
                }

                line.Value1 = count > 0 ? childEntries[0] : 0;
                line.Value2 = count > 1 ? childEntries[1] : 0;
                line.Value3 = count > 2 ? childEntries[2] : 0;
                line.Value4 = count > 3 ? childEntries[3] : 0;
            }

            var key = (line.ConditionType, line.Value1, line.Value2, line.Value3, line.Value4, line.Flags);
            if (byContent.TryGetValue(key, out var existingEntry))
                return existingEntry;

            uint entry;
            if (node.OriginalEntry != 0 && node.OriginalEntry > maxChildEntry && usedEntries.Add(node.OriginalEntry))
                entry = node.OriginalEntry;
            else
            {
                entry = next++;
                usedEntries.Add(entry);
            }

            line.ConditionEntry = entry;
            byContent[key] = entry;
            lines.Add(line);
            return entry;
        }

        /// <summary>Structural validation errors that must block saving.</summary>
        public static List<string> Validate(IReadOnlyList<MangosConditionViewModel> roots)
        {
            var errors = new List<string>();
            foreach (var root in roots)
                foreach (var node in root.Descendants())
                {
                    var label = node.Readable.RemoveTagsSafe();
                    if (node.IsLogical)
                    {
                        if (node.Children.Count < node.MinChildren || node.Children.Count > node.MaxChildren)
                            errors.Add(node.MinChildren == node.MaxChildren
                                ? $"\"{label}\" requires exactly {node.MinChildren} nested condition(s), has {node.Children.Count}."
                                : $"\"{label}\" requires {node.MinChildren} to {node.MaxChildren} nested conditions, has {node.Children.Count}.");
                    }
                    else if (node.Children.Count > 0)
                        errors.Add($"\"{label}\" is not AND/OR/NOT and cannot have nested conditions.");
                }

            return errors;
        }

        private static string RemoveTagsSafe(this string text) =>
            text.Replace("[p]", "").Replace("[/p]", "").Replace("[s]", "").Replace("[/s]", "");
    }
}
