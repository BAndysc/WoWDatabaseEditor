using System;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class WdeToolsTabControl : ToolsTabControl, IStyleable
    {
        Type IStyleable.StyleKey => typeof(ToolsTabControl);
        protected override IControl GenerateView(object viewModel)
        {
            if (ViewBind.TryResolve(viewModel, out var view) && view is IControl control)
                return control;
            return new TextBlock() {Text = $"Cannot locate View for {viewModel}"};
        }
    }
}