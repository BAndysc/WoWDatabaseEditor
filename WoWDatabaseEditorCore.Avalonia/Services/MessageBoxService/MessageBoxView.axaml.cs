using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService
{
    public partial class MessageBoxView : BaseMessageBoxWindow
    {
        public MessageBoxView()
        {
            InitializeComponent();
            this.AttachDevTools();
        }
        
        protected virtual Size MeasureOverrideInternal(Size availableSize)
        {
            double num1 = 0.0;
            double num2 = 0.0;
            IAvaloniaList<Visual> visualChildren = this.VisualChildren;
            int count = visualChildren.Count;
            for (int index = 0; index < count; ++index)
            {
                if (visualChildren[index] is Layoutable layoutable)
                {
                    layoutable.Measure(availableSize);
                    num1 = Math.Max(num1, layoutable.DesiredSize.Width);
                    num2 = Math.Max(num2, layoutable.DesiredSize.Height);
                }
            }
            return new Size(num1, num2);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is IMessageBoxViewModel dialogWindow)
            {
                dialogWindow.Close += DialogWindowOnClose;
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!realClosing)
            {
                if (DataContext is IMessageBoxViewModel dialogWindow)
                    dialogWindow.CancelButtonCommand.Execute(null);
                e.Cancel = true;
            }
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