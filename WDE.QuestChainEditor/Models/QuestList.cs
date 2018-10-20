using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.QuestChainEditor.Models
{
    public class QuestList : IEnumerable<Quest>
    {
        public event Action<QuestList, Quest> OnAddedQuest = delegate { };
        public event Action<QuestList, Quest> OnRemovedQuest = delegate { };

        private HashSet<Quest> quests;

        public QuestList()
        {
            quests = new HashSet<Quest>();
        }

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
            foreach (var q in quests)
            {
                if (q.RequiredQuests.Contains(quest))
                {
                    q.RequiredQuests.Remove(quest);
                }
            }
            quests.Remove(quest);
            OnRemovedQuest(this, quest);
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return quests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return quests.GetEnumerator();
        }
    }
}
