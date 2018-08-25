using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WDE.Common.Windows
{
    public interface IToolProvider
    {
        bool AllowMultiple { get; }
        string Name { get; }
        ContentControl GetView();
    }
}
