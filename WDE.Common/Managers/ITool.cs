using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.Common.Managers
{
    public interface ITool
    {
        string Title { get; }
        ContentControl Content { get; }
    }
}
