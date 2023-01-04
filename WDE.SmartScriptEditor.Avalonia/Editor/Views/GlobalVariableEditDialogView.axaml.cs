using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Managers;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    public partial class GlobalVariableEditDialogView : DialogViewBase
    {
        public GlobalVariableEditDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private IDialog? lastDataContext;
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            Unbind();
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel += RequestClose;
                dialogWindow.CloseOk += RequestClose;
                lastDataContext = dialogWindow;
            }
        }

        private void RequestClose()
        {
            var popup = this.FindLogicalAncestorOfType<Popup>();
            if (popup != null)
                popup.IsOpen = false;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Unbind();
        }

        private void Unbind()
        {
            if (lastDataContext == null)
                return;
            lastDataContext.CloseCancel -= RequestClose;
            lastDataContext.CloseOk -= RequestClose;
        }
    }
}