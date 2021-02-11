using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Managers;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    /// <summary>
    ///     Interaction logic for ModulesConfigView.xaml
    /// </summary>
    public class DialogWindow : Window
    {
        public DialogWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
            Close(true);
        }

        private void DialogWindowOnCloseCancel()
        {
            Close(false);
        }
    }
    
}