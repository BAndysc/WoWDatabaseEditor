using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.Solutions
{
    [AutoRegister]
    public class SolutionFolderItemProvider : ISolutionItemProvider
    {
        public bool IsCompatibleWithCore(ICoreVersion core) => true;
        
        public string GetName()
        {
            return "Folder";
        }

        public ImageSource GetImage()
        {
            return new BitmapImage(new Uri("/WDE.Solutions;component/Resources/folder.png", UriKind.Relative));
        }

        public string GetDescription()
        {
            return "Container for solutions";
        }

        public ISolutionItem CreateSolutionItem()
        {
            string input = Interaction.InputBox("Put folder name", "New Folder", "My new folder");
            if (!string.IsNullOrEmpty(input))
                return new SolutionFolderItem(input);
            return null;
        }
    }
}