using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace WDE.Common
{
    public interface ISolutionItemProvider
    {
        string GetName();
        ImageSource GetImage();
        string GetDescription();

        ISolutionItem CreateSolutionItem();
    }
}
