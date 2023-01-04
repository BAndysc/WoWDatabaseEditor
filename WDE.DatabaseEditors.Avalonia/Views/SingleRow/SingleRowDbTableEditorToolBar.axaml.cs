using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Prism.Commands;
using WDE.Common.Avalonia;
using WDE.Common.Utils;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow
{
    public partial class SingleRowDbTableEditorToolBar : UserControl
    {
        private ICommand focusCommand;
        private Window? attachedRoot = null;
        
        public SingleRowDbTableEditorToolBar()
        {
            InitializeComponent();
            focusCommand = new DelegateCommand(() =>
            {
                TextBox tb = this.GetControl<TextBox>("SearchTextBox");
                tb?.Focus();
            });
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            attachedRoot = this.GetVisualRoot() as Window;
            if (attachedRoot != null)
            {
                attachedRoot.KeyBindings.Add(new KeyBinding()
                {
                    Command = focusCommand,
                    Gesture = new KeyGesture(Key.F, KeyGestures.CommandModifier)
                });
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (attachedRoot != null)
            {
                attachedRoot.KeyBindings.RemoveIf(x => x.Command == focusCommand);
                attachedRoot = null;
            }
        }
    }
}