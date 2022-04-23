namespace WDE.SmartScriptEditor.Editor
{
    public interface IEditorFeatures
    {
        string Name { get; }
        bool SupportsSource { get; }
        bool SupportsEventCooldown { get; }
        bool SupportsTargetCondition { get; }
    }
}