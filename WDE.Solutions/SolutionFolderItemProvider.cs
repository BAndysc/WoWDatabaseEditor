using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WDE.Common;
using WDE.Common.Attributes;

namespace WDE.Solutions
{
    [AutoRegister]
    public class SolutionFolderItemProvider : ISolutionItemProvider
    {
        public string GetName()
        {
            return "Folder";
        }

        public ImageSource GetImage()
        {
            return new BitmapImage(new Uri($"/WDE.Solutions;component/Resources/folder.png", UriKind.Relative));
        }

        public string GetDescription()
        {
            return "Container for solutions";
        }

        public ISolutionItem CreateSolutionItem()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Put folder name", "New Folder", "My new folder", -1, -1);
            if (!string.IsNullOrEmpty(input))
                return new SolutionFolderItem(input);
            return null;
        }
    }
}
