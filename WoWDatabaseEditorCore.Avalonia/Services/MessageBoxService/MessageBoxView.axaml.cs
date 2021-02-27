using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService
{
    public class MessageBoxView : BaseMessageBoxWindow
    {
        public MessageBoxView()
        {
            InitializeComponent();
            this.AttachDevTools();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // this is hack to make fit to content work
            ClientSize = new Size(500, 2000);
            var ret = MeasureOriginal(new Size(500, 2000));
            ClientSize = ret;
            return ret;
        }
        
        private Size MeasureOriginal(Size availableSize)
        {
            double width = 0;
            double height = 0;

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            for (var i = 0; i < visualCount; i++)
            {
                IVisual visual = visualChildren[i];

                if (visual is ILayoutable layoutable)
                {
                    layoutable.Measure(availableSize);
                    width = Math.Max(width, layoutable.DesiredSize.Width);
                    height = Math.Max(height, layoutable.DesiredSize.Height);
                }
            }

            return new Size(width, height);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is IMessageBoxViewModel dialogWindow)
            {
                dialogWindow.Close += DialogWindowOnClose;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!realClosing)
                e.Cancel = true;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is IMessageBoxViewModel dialogWindow)
            {
                dialogWindow.Close -= DialogWindowOnClose;
            }
            base.OnClosed(e);
        }

        private bool realClosing;
        private void DialogWindowOnClose()
        {
            realClosing = true;
            Close(true);
        }
    }
}