using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Types;

namespace WDE.LootEditor.Configuration;

public static class StaticIcons
{
    public static IEnumerable<IconViewModel> Icons { get; }
    public static IEnumerable<object> IconsSource { get; } // todo: CompletionComboBox Items should be IEnumerable, not IEnumerable<object>
                                                           // otherwise we need this extra redundant property here
    static StaticIcons()
    {
        var icons = new List<IconViewModel>();

        if (!OperatingSystem.IsBrowser())
        {
            var files = Directory.GetFiles("Icons/", "*.png");
            foreach (var file in files)
            {
                if (file.Contains("@2x"))
                    continue;
                
                if (file.Contains("_big"))
                    continue;

                if (file.Contains("_dark"))
                    continue;
                
                string relativePath;
                if (file.Contains("Icons/"))
                    relativePath = file.Substring(file.IndexOf("Icons/", StringComparison.Ordinal));
                else if (file.Contains("Icons\\"))
                   relativePath = file.Substring(file.IndexOf("Icons\\", StringComparison.Ordinal));
                else
                    continue;
                
                icons.Add(new IconViewModel(new ImageUri(relativePath)));
            }
        }
        
        Icons = icons;
        IconsSource = icons;
    }
}