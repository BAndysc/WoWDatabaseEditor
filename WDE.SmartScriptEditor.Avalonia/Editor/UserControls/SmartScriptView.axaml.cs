using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Prism.Commands;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartScriptView.xaml
    /// </summary>
    public partial class SmartScriptView : UserControl
    {
        public SmartScriptView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private List<object> temporaryMenuItems = new();
        
        private void Control_OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            var dataContext = DataContext as SmartScriptEditorViewModel;
            var control = sender as Control;
            var contextMenu = control?.ContextMenu;

            if (contextMenu == null || dataContext == null)
                return;

            var dynamicMenuItems = dataContext.GetDynamicContextMenuForSelected();
            if (dynamicMenuItems == null)
                return;
            
            var items = contextMenu.Items as AvaloniaList<object>;
            if (items == null)
                return;

            if (temporaryMenuItems.Count != 0)
            {
                Console.WriteLine("While opening a custom context menu, something is really fucked up, because there are old items?");
                temporaryMenuItems.Clear();
            }

            void AddMenuItem(string header, ICommand command)
            {
                var menuItem = new MenuItem()
                {
                    Header = header,
                    Command = command,
                    CommandParameter = dataContext
                };
                items!.Insert(temporaryMenuItems.Count, menuItem);
                temporaryMenuItems.Add(menuItem);
            }

            void AddSeparator()
            {
                var separator = new Separator();
                items!.Insert(temporaryMenuItems.Count, separator);
                temporaryMenuItems.Add(separator);
            }

            if (dynamicMenuItems.Count > 0)
            {
                foreach (var menu in dynamicMenuItems)
                    AddMenuItem(menu.Name, menu);
                AddSeparator();

                contextMenu.MenuClosed += MenuClosed;   
            }
        }

        private void MenuClosed(object? sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null)
                return;

            var items = contextMenu.Items as AvaloniaList<object>;
            if (items == null)
                return;

            for (int i = 0; i < temporaryMenuItems.Count; ++i)
                items.Remove(temporaryMenuItems[i]);
            temporaryMenuItems.Clear();
            contextMenu.MenuClosed -= MenuClosed;
        }
    }
}