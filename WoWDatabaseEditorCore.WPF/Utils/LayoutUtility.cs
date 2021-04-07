using System.Collections.Generic;
using System.IO;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using WDE.Common.Services;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.WPF.Utils
{
    internal static class LayoutUtility
    {
        private static string DockSettingsFile = "~/dock.layout";
        
        public static void SaveLayout(DockingManager manager, IFileSystem fileSystem)
        {
            var layoutSerializer = new XmlLayoutSerializer(manager);
            layoutSerializer.Serialize(fileSystem.OpenWrite(DockSettingsFile));
        }

        public static void LoadLayout(DockingManager manager, ILayoutViewModelResolver layoutResolver, IFileSystem fileSystem)
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

            if (fileSystem.Exists(DockSettingsFile))
            {
                try
                {
                    layoutSerializer.Deserialize(fileSystem.OpenRead(DockSettingsFile));
                }
                catch (System.Exception _)
                {
                    layoutResolver.LoadDefault();
                }
            }
            else
                layoutResolver.LoadDefault();
        }
    }
}