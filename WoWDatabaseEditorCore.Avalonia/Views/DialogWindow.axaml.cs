using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Managers;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    /// <summary>
    ///     Interaction logic for ModulesConfigView.xaml
    /// </summary>
    public class DialogWindow : ExtendedWindow
    {
        private bool reallyCloseNow = false;
        
        public DialogWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            //(Content as IInputElement)?.Focus();
        }

        private IDialog? boundDialog;
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (boundDialog != null)
            {
                boundDialog.CloseCancel -= DialogWindowOnCloseCancel;
                boundDialog.CloseOk -= DialogWindowOnCloseOk;
                boundDialog = null;
            }
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel += DialogWindowOnCloseCancel;
                dialogWindow.CloseOk += DialogWindowOnCloseOk;
                boundDialog = dialogWindow;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (boundDialog != null)
            {
                boundDialog.CloseCancel -= DialogWindowOnCloseCancel;
                boundDialog.CloseOk -= DialogWindowOnCloseOk;
                boundDialog = null;
            }
            base.OnClosed(e);
        }
        
        private void DialogWindowOnCloseOk()
        {
            reallyCloseNow = true;
            Close(true);
        }

        private void DialogWindowOnCloseCancel()
        {
            reallyCloseNow = true;
            Close(false);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (reallyCloseNow)
                return;
            
            if (DataContext is IClosableDialog closable)
            {
                e.Cancel = true;
                closable.OnClose();
            }
        }
        
        
        // AVALONIA FIX
        
        protected virtual Size MeasureOverrideInternal(Size availableSize)
        {
            double num1 = 0.0;
            double num2 = 0.0;
            IAvaloniaList<IVisual> visualChildren = this.VisualChildren;
            int count = visualChildren.Count;
            for (int index = 0; index < count; ++index)
            {
                if (visualChildren[index] is ILayoutable layoutable)
                {
                    layoutable.Measure(availableSize);
                    num1 = Math.Max(num1, layoutable.DesiredSize.Width);
                    num2 = Math.Max(num2, layoutable.DesiredSize.Height);
                }
            }
            return new Size(num1, num2);
        }
        
        // this is a workaround to broken SizeToContent with MaxWidth
        // this is copied base.MeasureOverride with added Math.Min MaxWidth constraint
        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var clientSize = ClientSize;
            var constraint = clientSize;
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;

            if (sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                constraint = constraint.WithWidth(Math.Min(maxAutoSize.Width, MaxWidth));
            }

            if (sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var result = MeasureOverrideInternal(constraint);

            if (!sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    result = result.WithWidth(availableSize.Width);
                }
                else
                {
                    result = result.WithWidth(clientSize.Width);
                }
            }

            if (!sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                if (!double.IsInfinity(availableSize.Height))
                {
                    result = result.WithHeight(availableSize.Height);
                }
                else
                {
                    result = result.WithHeight(clientSize.Height);
                }
            }

            return result;
        }
    }
}