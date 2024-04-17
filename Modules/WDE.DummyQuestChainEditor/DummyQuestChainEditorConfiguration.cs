using WDE.Module.Attributes;
using WDE.QuestChainEditor.Services;

namespace WDE.DummyQuestChainEditor;

[SingleInstance]
[AutoRegister]
public class DummyQuestChainEditorConfiguration : IQuestChainEditorConfiguration
{
    public int? ShowMarkConditionSourceType => null;
}