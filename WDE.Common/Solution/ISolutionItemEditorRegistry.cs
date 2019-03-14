using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemEditorRegistry
    {
        Document GetEditor(ISolutionItem item);
    }
}
