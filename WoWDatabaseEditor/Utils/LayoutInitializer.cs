using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;
using WDE.Common.Windows;

namespace WoWDatabaseEditor.Utils
{
	// by https://github.com/tgjones/gemini/blob/master/src/Gemini/Modules/Shell/Controls/LayoutInitializer.cs
	public class LayoutInitializer : ILayoutUpdateStrategy
	{
		public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
		{
		    var tool = anchorableToShow.Content as ITool;
		    if (tool != null)
			{
				var preferredLocation = tool.PreferedPosition;
				string paneName = GetPaneName(preferredLocation);
				var toolsPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == paneName);
				if (toolsPane == null)
				{
                    switch (preferredLocation)
                    {
                        case ToolPreferedPosition.Left:
                            toolsPane = CreateAnchorablePane(layout, Orientation.Horizontal, paneName, InsertPosition.Start);
                            break;
                        case ToolPreferedPosition.Right:
                            toolsPane = CreateAnchorablePane(layout, Orientation.Horizontal, paneName, InsertPosition.End);
                            break;
                        case ToolPreferedPosition.Bottom:
                            toolsPane = CreateAnchorablePane(layout, Orientation.Vertical, paneName, InsertPosition.End);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
				}
				toolsPane.Children.Add(anchorableToShow);
				return true;
			}

			return false;
		}

		private static string GetPaneName(ToolPreferedPosition location)
		{
			switch (location)
			{
				case ToolPreferedPosition.Left:
					return "LeftPane";
				case ToolPreferedPosition.Right:
					return "RightPane";
				case ToolPreferedPosition.Bottom:
					return "BottomPane";
				default:
					throw new ArgumentOutOfRangeException("location");
			}
		}

        private static LayoutAnchorablePane CreateAnchorablePane(LayoutRoot layout, Orientation orientation,
            string paneName, InsertPosition position)
        {
            var parent = layout.Descendents().OfType<LayoutPanel>().First(d => d.Orientation == orientation);
            var toolsPane = new LayoutAnchorablePane { Name = paneName };
            if (position == InsertPosition.Start)
                parent.InsertChildAt(0, toolsPane);
            else
                parent.Children.Add(toolsPane);
            return toolsPane;
        }

        private enum InsertPosition
        {
            Start,
            End
        }

		public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
		{
			// If this is the first anchorable added to this pane, then use the preferred size.
            var tool = anchorableShown.Content as ITool;
		    if (tool != null)
		    {
		        var anchorablePane = anchorableShown.Parent as LayoutAnchorablePane;
                if (anchorablePane != null && anchorablePane.ChildrenCount == 1)
                {
                    switch (tool.PreferedPosition)
                    {
                        case ToolPreferedPosition.Left:
                        case ToolPreferedPosition.Right:
                            anchorablePane.DockWidth = new GridLength(250, GridUnitType.Pixel);
                            break;
                        case ToolPreferedPosition.Bottom:
                            anchorablePane.DockHeight = new GridLength(250, GridUnitType.Pixel);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
		    }
		}

	    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
	    {
            return false;
	    }

	    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
	    {
	        
	    }
	}
}