using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Database
{
    public interface IQuestTemplate
    {
        uint Entry { get; }
        string Name { get; }

        uint PrevQuestId { get; }
        uint NextQuestId { get; }
        uint ExclusiveGroup { get; }
        uint NextQuestInChain { get; }
    }
}
