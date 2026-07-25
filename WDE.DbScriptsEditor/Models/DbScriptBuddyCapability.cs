namespace WDE.DbScriptsEditor.Models
{
    // What kind of object a command's buddy locator can find, declared by the "buddy" field in
    // commands.json. Drives which buddy actor choices the source/target editor offers and the
    // advisory buddy-kind warning.
    public enum DbScriptBuddyCapability
    {
        Creature,   // "creature" (the core default)
        GameObject, // "gameobject"
        Both,       // "both"
    }

    public static class DbScriptBuddyCapabilities
    {
        // "creature" | "gameobject" | "both"; empty/unknown falls back to Creature (the default the
        // core assumes when a command does not declare a buddy kind).
        public static DbScriptBuddyCapability Parse(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "gameobject" => DbScriptBuddyCapability.GameObject,
            "both" => DbScriptBuddyCapability.Both,
            _ => DbScriptBuddyCapability.Creature,
        };

        public static bool AllowsCreature(this DbScriptBuddyCapability capability) =>
            capability is DbScriptBuddyCapability.Creature or DbScriptBuddyCapability.Both;

        public static bool AllowsGameObject(this DbScriptBuddyCapability capability) =>
            capability is DbScriptBuddyCapability.GameObject or DbScriptBuddyCapability.Both;
    }
}
