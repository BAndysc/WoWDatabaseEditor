namespace WDE.SmartScriptEditor.Editor
{
    public interface IEditorFeatures
    {
        bool SupportsSource { get; }
        bool SupportsEventCooldown { get; }
        bool SupportsTargetCondition { get; }
    }
}