using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.Common.WPF.Themes
{
    internal class ParameterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? Generic { get; set; }
        public DataTemplate? BoolParameter { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ParameterValueHolder<long> intParam && intParam.Parameter is BoolParameter boolParameter && BoolParameter != null)
                return BoolParameter;
            return Generic!;
        }
    }

}
