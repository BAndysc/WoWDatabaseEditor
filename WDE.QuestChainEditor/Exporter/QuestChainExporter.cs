using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Exporter
{
    public class QuestChainExporter
    {
        public string GenerateSQL(IEnumerable<Quest> quests)
        {
            StringBuilder builder = new();
            foreach (Quest quest in quests)
            {
                if (quest.RequiredQuests.Count >= 1)
                {
                    if (quest.RequiredQuests.Count > 1)
                    {
                        foreach (Quest required in quest.RequiredQuests)
                        {
                            builder.AppendLine(
                                $"UPDATE `quest_template` SET ExclusiveGroup = -{quest.RequiredQuests[0].Id} WHERE `id` = {required.Id}; // {required.Name}");
                        }
                    }

                    builder.AppendLine(
                        $"UPDATE `quest_template` SET PrevQuestId={quest.RequiredQuests[0].Id}, NextQuestId=0 WHERE `id` = {quest.Id}; // {quest.Name}");
                }
                else if (quest.RequiredQuests.Count == 0)
                {
                    builder.AppendLine(
                        $"UPDATE `quest_template` SET PrevQuestId=0, NextQuestId=0  WHERE `id` = {quest.Id}; // {quest.Name}");
                }
            }

            return builder.ToString();
        }
    }
}