using System;

namespace WDE.DbScriptsEditor.Models
{
    // Who a step acts as (actor) and on whom (patient), expressed structurally so the user never
    // touches data_flags bits. "Self" combos are represented by Source == Target.
    public enum SourceTargetKind
    {
        OriginalSource,  // the script's built-in source (per DbScriptType, e.g. gossip NPC)
        OriginalTarget,  // the script's built-in target (e.g. the player)
        Buddy,           // the separately-located buddy object
    }

    public readonly struct ScriptDirection : IEquatable<ScriptDirection>
    {
        public SourceTargetKind Source { get; }
        public SourceTargetKind Target { get; }

        public ScriptDirection(SourceTargetKind source, SourceTargetKind target)
        {
            Source = source;
            Target = target;
        }

        public bool UsesBuddy => Source == SourceTargetKind.Buddy || Target == SourceTargetKind.Buddy;

        public bool Equals(ScriptDirection other) => Source == other.Source && Target == other.Target;
        public override bool Equals(object? obj) => obj is ScriptDirection o && Equals(o);
        public override int GetHashCode() => HashCode.Combine((int)Source, (int)Target);
        public override string ToString() => $"{Source} -> {Target}";

        public static bool operator ==(ScriptDirection a, ScriptDirection b) => a.Equals(b);
        public static bool operator !=(ScriptDirection a, ScriptDirection b) => !a.Equals(b);
    }
}
