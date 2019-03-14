using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Module.Attributes;

namespace WDE.Common.Windows
{
    [NonUniqueProvider]
    public interface IToolProvider
    {
        bool AllowMultiple { get; }
        string Name { get; }
        ContentControl GetView();

        bool CanOpenOnStart  { get; }
    }
}
