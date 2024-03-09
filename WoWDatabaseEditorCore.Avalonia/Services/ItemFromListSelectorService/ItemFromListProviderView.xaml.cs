using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using Prism.Commands;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WoWDatabaseEditorCore.Services.ItemFromListSelectorService;

namespace WoWDatabaseEditorCore.Avalonia.Services.ItemFromListSelectorService
{
    /// <summary>
    ///     Interaction logic for ItemFromListProviderView.xaml
    /// </summary>
    public class ItemFromListProviderView : DialogViewBase
    {
        public ItemFromListProviderView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            KeyBindings.Add(new KeyBinding()
            {
                Command = new DelegateCommand(() =>
                {
                    this.GetControl<TextBox>("SearchBox").Focus();
                }),
                Gesture = new KeyGesture(Key.F, KeyGestures.CommandModifier)
            });
        }
        
        // quality of life feature: arrow down in searchbox focuses first element
        private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                GridView gridView = this.GetControl<GridView>("GridView");
                if (gridView.ListBoxImpl == null)
                    return;

                if (gridView.ListBoxImpl.SelectedItem == null)
                    gridView.ListBoxImpl.SelectedIndex = 0;
                gridView.ListBoxImpl.ContainerFromIndex(gridView.ListBoxImpl.SelectedIndex)?.Focus();
            }
        }
    }
}
