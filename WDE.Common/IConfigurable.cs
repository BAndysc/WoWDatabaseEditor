using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface IConfigurable
    {
        KeyValuePair<ContentControl, Action> GetConfigurationView();
        string GetName();
    }
}
