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
    public interface IDocument : System.IDisposable
    {
        string Title { get; }
        ICommand Undo { get; }
        ICommand Redo { get; }
        ICommand Copy { get; }
        ICommand Cut { get; }
        ICommand Paste { get; }
        ICommand Save { get; }
        ICommand CloseCommand { get; set;  }
        bool CanClose { get; }
        IHistoryManager History { get; }
    }
}
