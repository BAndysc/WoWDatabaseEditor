using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    public interface IQuestPicker
    {
        QuestDefinition ChooseQuest();
    }
}