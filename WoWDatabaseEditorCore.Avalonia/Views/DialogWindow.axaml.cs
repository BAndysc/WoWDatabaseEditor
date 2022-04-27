using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Markup.Xaml;
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

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel += DialogWindowOnCloseCancel;
                dialogWindow.CloseOk += DialogWindowOnCloseOk;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel -= DialogWindowOnCloseCancel;
                dialogWindow.CloseOk -= DialogWindowOnCloseOk;
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
    }
    
}