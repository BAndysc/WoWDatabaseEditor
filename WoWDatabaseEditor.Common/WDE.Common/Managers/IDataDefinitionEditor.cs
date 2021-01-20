using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Managers
{
    public interface IDataDefinitionEditor
    {
        string EditorName { get; }
        IDocument Editor { get; }
    }
}
