using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;

namespace WDE.Common.Solution
{
    public interface ISolutionItemEditorRegistry
    {
        DocumentEditor GetEditor(ISolutionItem item);
    }
}
