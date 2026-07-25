using System;
using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // The static object kind of a script actor, as a flag mask so subtyping is plain intersection:
    // Unit = Creature|Player, WorldObject = everything. Two kinds are compatible iff masks overlap.
    [Flags]
    public enum DbScriptActorKind
    {
        None = 0,
        Creature = 1,
        Player = 2,
        GameObject = 4,
        Unit = Creature | Player,
        WorldObject = Unit | GameObject,
    }

    public static class DbScriptActorKinds
    {
        public static DbScriptActorKind Parse(string type) => type switch
        {
            "Creature" => DbScriptActorKind.Creature,
            "Player" => DbScriptActorKind.Player,
            "GameObject" => DbScriptActorKind.GameObject,
            "Unit" => DbScriptActorKind.Unit,
            "WorldObject" => DbScriptActorKind.WorldObject,
            _ => DbScriptActorKind.None, // "None" and unknown strings contribute nothing
        };

        // Mask of a commands.json source_types/target_types list. An empty list means the command
        // did not declare anything — treat as "any" rather than "nothing".
        public static DbScriptActorKind FromList(IReadOnlyList<string> types)
        {
            if (types.Count == 0)
                return DbScriptActorKind.WorldObject;
            var mask = DbScriptActorKind.None;
            foreach (var t in types)
                mask |= Parse(t);
            return mask;
        }

        public static bool Compatible(DbScriptActorKind a, DbScriptActorKind b) => (a & b) != 0;
    }
}
