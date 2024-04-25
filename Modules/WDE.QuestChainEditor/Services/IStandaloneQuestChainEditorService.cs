using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Services;

[UniqueProvider]
public interface IStandaloneQuestChainEditorService
{
    void OpenStandaloneEditor(uint? entry = null);
}