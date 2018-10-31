using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WDE.Common;
using WDE.Module.Attributes;

namespace WDE.Blueprints.Providers
{
    [AutoRegister]
    public class BlueprintItemProvider : ISolutionItemProvider
    {
        public ISolutionItem CreateSolutionItem()
        {
            return new BlueprintSolutionItem();
        }

        public string GetDescription()
        {
            return "Script in new Blueprints system";
        }

        public ImageSource GetImage()
        {
            return new BitmapImage(new Uri($"/WDE.Blueprints;component/Resources/icon.png", UriKind.Relative));
        }

        public string GetName()
        {
            return "Blueprint";
        }
    }
}
