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

        int PrevQuestId { get; }
        int NextQuestId { get; }
        int ExclusiveGroup { get; }
        int NextQuestInChain { get; }
    }
}
