using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Editor
{
    // Per-version editor capability flags. Phase 1 targets CMaNGOS-WoTLK, where every
    // column exists and TALK text is a broadcast_text id. Older cores gate features here
    // (resolved from ICurrentCoreVersion in a future phase) rather than crashing.
    [UniqueProvider]
    public interface IDbScriptEditorFeatures
    {
        bool HasPriority { get; }
        bool HasDatafloat { get; }
        bool HasSpeed { get; }
        // true = TALK dataint is a broadcast_text id (wotlk); false = legacy dbscript_string
        bool TalkUsesBroadcastText { get; }
        uint MaxCommandId { get; }
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptEditorFeatures : IDbScriptEditorFeatures
    {
        public bool HasPriority => true;
        public bool HasDatafloat => true;
        public bool HasSpeed => true;
        public bool TalkUsesBroadcastText => true;
        public uint MaxCommandId => 57;
    }
}
