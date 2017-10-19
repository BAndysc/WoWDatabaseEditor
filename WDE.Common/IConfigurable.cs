using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WDE.Common
{
    public interface IConfigurable
    {
        KeyValuePair<ContentControl, Action> GetConfigurationView();
        string GetName();
    }
}
