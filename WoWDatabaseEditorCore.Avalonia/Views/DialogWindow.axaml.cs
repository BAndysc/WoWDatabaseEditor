using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    public partial class DialogWindow : ExtendedWindow
    {
        private bool reallyCloseNow = false;
        
        public DialogWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
        }
        
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (DataContext is IDialogWindowBase windowBase)
                windowBase.OnWindowOpened();
            //(Content as IInputElement)?.Focus();
        }

        private IDialog? boundDialog;
        private IClosableDialog? boundWindow;
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (boundDialog != null)
            {
                boundDialog.CloseCancel -= DialogWindowOnCloseCancel;
                boundDialog.CloseOk -= DialogWindowOnCloseOk;
                boundDialog = null;
            }
            if (boundWindow != null)
            {
                boundWindow.Close -= WindowOnClose;
                boundWindow = null;
            }
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel += DialogWindowOnCloseCancel;
                dialogWindow.CloseOk += DialogWindowOnCloseOk;
                boundDialog = dialogWindow;
            }
            if (DataContext is IClosableDialog closable)
            {
                closable.Close += WindowOnClose;
                boundWindow = closable;
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
            if (boundWindow != null)
            {
                boundWindow.Close -= WindowOnClose;
                boundWindow = null;
            }
            base.OnClosed(e);
        }
        
        private void WindowOnClose()
        {
            reallyCloseNow = true;
            Close(true);
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

        protected override void OnClosing(WindowClosingEventArgs e)
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

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            var visualLayerManager = this.FindDescendantOfType<VisualLayerManager>();

            // this ia bug in Avalonia 11
            // if you add a TextBox to adorner layer and it will be focused and no textbox has been ever focused
            // then visualLayerManager.TextSelectorLayer property will be accessed from within VisualLayerManager
            // Measure (or Arrange) causing Collection was modified; enumeration operation may not execute.
            // until this is fixed, we workaround that by fetching this property here, so that a getter invoked in
            // Measure will not create a layer
            // https://github.com/AvaloniaUI/Avalonia/issues/14483
            if (visualLayerManager != null)
            {
                var layer = visualLayerManager.TextSelectorLayer;
            }
        }
    }
}