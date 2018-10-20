using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.QuestChainEditor.Models
{
    public class Quest
    {
        public readonly uint Id;
        public readonly string Name;

        public ObservableCollection<Quest> RequiredQuests;

        public Quest(uint id, string name)
        {
            Id = id;
            Name = name;

            RequiredQuests = new ObservableCollection<Quest>();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Quest))
                return false;

            return Id.Equals(((Quest)obj).Id);
        }
    }
}
