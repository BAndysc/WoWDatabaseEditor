using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.History
{
    public interface IHistoryAction
    {
        void Undo();
        void Redo();

        string GetDescription();
    }
}
