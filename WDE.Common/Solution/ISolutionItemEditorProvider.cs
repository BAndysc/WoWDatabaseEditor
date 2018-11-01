using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemEditorProvider
    {

    }

    [NonUniqueProvider]
    public interface ISolutionItemEditorProvider<T> : ISolutionItemEditorProvider where T : ISolutionItem
    {
        Document GetEditor(T item);
    }
}
