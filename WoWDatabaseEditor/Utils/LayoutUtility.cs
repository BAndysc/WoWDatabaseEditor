using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using WDE.Common.Managers;
using WDE.Common.Windows;

namespace WoWDatabaseEditor.Utils
{
    internal static class LayoutUtility
    {
        public static void SaveLayout(DockingManager manager)
        {
            var layoutSerializer = new XmlLayoutSerializer(manager);
            layoutSerializer.Serialize("dock.layout");
        }

        public static void LoadLayout(DockingManager manager, ILayoutViewModelResolver layoutResolver)
        {
            var layoutSerializer = new XmlLayoutSerializer(manager);
            HashSet<string> resolved = new HashSet<string>();
            
            layoutSerializer.LayoutSerializationCallback += (s, e) =>
                {
                    if (e.Model?.ContentId != null && e.Model is LayoutAnchorable anchorable && !resolved.Contains(e.Model.ContentId))
                    {
                        var tool = layoutResolver.ResolveViewModel(e.Model.ContentId);
                        resolved.Add(e.Model.ContentId);

                        if (tool != null)
                        {
                            e.Content = tool;
                            tool.Visibility = anchorable.IsVisible;
                            return;
                        }
                    }

                    // Don't create any panels if something went wrong.
                    e.Cancel = true;
                };

            if (File.Exists("dock.layout"))
                layoutSerializer.Deserialize("dock.layout");
            else
                layoutResolver.LoadDefault();
        }
    }
}