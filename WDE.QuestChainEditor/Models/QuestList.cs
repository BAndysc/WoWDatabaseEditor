namespace WDE.QuestChainEditor.Models
{
    public class QuestList : IEnumerable<Quest>
    {
        private readonly HashSet<Quest> quests;

        public QuestList()
        {
            quests = new HashSet<Quest>();
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return quests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return quests.GetEnumerator();
        }

        public event Action<QuestList, Quest> OnAddedQuest = delegate { };
        public event Action<QuestList, Quest> OnRemovedQuest = delegate { };

        public bool AddQuest(Quest quest)
        {
            if (quest.RequiredQuests.Count > 0)
                throw new InvalidOperationException();

            bool added = quests.Add(quest);
            if (added)
                OnAddedQuest(this, quest);
            return added;
        }

        public void RemoveQuest(Quest quest)
        {
            foreach (Quest q in quests)
            {
                if (q.RequiredQuests.Contains(quest))
                    q.RequiredQuests.Remove(quest);
            }

            quests.Remove(quest);
            OnRemovedQuest(this, quest);
        }
    }
}