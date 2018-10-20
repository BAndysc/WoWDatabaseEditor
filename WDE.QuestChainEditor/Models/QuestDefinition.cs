using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.QuestChainEditor.Models
{
    public class QuestDefinition
    {
        public uint Id { get; }
        public string Title { get; }

        public QuestDefinition(uint Id, string Title)
        {
            this.Id = Id;
            this.Title = Title;
        }
    }
}
