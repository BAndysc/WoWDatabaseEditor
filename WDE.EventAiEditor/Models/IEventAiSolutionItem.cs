using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.EventAiEditor.Models
{
    public interface IEventAiSolutionItem : ISolutionItem
    {
        int EntryOrGuid { get; }
    }
}