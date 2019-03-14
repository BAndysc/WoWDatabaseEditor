using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public interface IDocument
    {
        string Title { get; }
        ICommand Undo { get; }
        ICommand Redo { get; }
        ICommand Save { get; }

        ICommand CloseCommand { get; }
        bool CanClose { get; }

        IHistoryManager History { get; }

        ContentControl Content { get; }
    }
}
