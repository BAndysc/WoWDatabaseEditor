using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Prism.Commands;
using WDE.Common.Utils;

namespace WDE.LootEditor.Editor.Views;

public partial class LootEditorToolBar : UserControl
{
    private ICommand focusCommand;
    public LootEditorToolBar()
    {
        InitializeComponent();
        focusCommand = new DelegateCommand(() =>
        {
            TextBox? tb = this.FindControl<TextBox>("SearchTextBox");
            tb?.Focus();
        });
    }

    private Window? attachedRoot = null;
        
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attachedRoot = this.GetVisualRoot() as Window;
        if (attachedRoot != null)
        {
            attachedRoot.KeyBindings.Add(new KeyBinding()
            {
                Command = focusCommand,
                Gesture = new KeyGesture(Key.F, GetPlatformCommandKey())
            });
        }
    }
    
    private static KeyModifiers GetPlatformCommandKey()
    {            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return KeyModifiers.Meta;
        }

        return KeyModifiers.Control;
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
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}