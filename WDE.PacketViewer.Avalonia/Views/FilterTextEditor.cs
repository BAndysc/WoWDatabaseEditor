using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using AvaloniaEdit;
using WDE.Common.Avalonia.Controls;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class FilterTextEditor : TextEditor, IStyleable
    {
        Type IStyleable.StyleKey => typeof(TextEditor);

        public FilterTextEditor()
        {
            ShowLineNumbers = false;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            SetValue(FontFamilyProperty, "Consolas,Menlo,Courier,Courier New");
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            SetValue(AvalonEditExtra.SyntaxProperty, "Resources/PacketFilterSyntaxHighlight.xml");
        }
    }
}