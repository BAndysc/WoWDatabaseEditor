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
using WDE.Common.Managers;

namespace WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService
{
    public partial class MessageBoxView : BaseMessageBoxWindow
    {
        public MessageBoxView()
        {
            InitializeComponent();
            this.AttachDevTools();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is IClosableDialog dialogWindow)
            {
                dialogWindow.Close += DialogWindowOnClose;
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!realClosing)
            {
                if (DataContext is IClosableDialog dialogWindow)
                    dialogWindow.OnClose();
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is IClosableDialog dialogWindow)
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