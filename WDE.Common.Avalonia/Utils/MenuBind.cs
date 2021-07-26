using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Prism.Commands;
using WDE.Common.Menu;

namespace WDE.Common.Avalonia.Utils
{
    public static class MenuBind
    {
        public static readonly AvaloniaProperty MenuItemsProperty = AvaloniaProperty.RegisterAttached<NativeMenu, IList<IMainMenuItem>>("Model",
            typeof(MenuBind),coerce: OnMenuChanged);
        
        public static IList<IMainMenuItem> GetMenuItems(NativeMenu control) => (IList<IMainMenuItem>)control.GetValue(MenuItemsProperty);
        public static void SetMenuItems(NativeMenu control, object value) => control.SetValue(MenuItemsProperty, value);
        
        private static IList<IMainMenuItem> OnMenuChanged(IAvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
            
            NativeMenuItemBase item;
            foreach (var m in viewModel)
            {
                NativeMenuItem topLevelItem = new NativeMenuItem(m.ItemName.Replace("_", ""));
                topLevelItem.Menu = new NativeMenu();
                foreach (var subItem in m.SubItems)
                {
                    if (subItem.ItemName == "Separator")
                        item = new NativeMenuItemSeperator();
                    else
                    {
                        var nativeMenuItem = new NativeMenuItem(subItem.ItemName.Replace("_", ""));
                        if (subItem is IMenuCommandItem cmd)
                        {
                            nativeMenuItem.Command = cmd.ItemCommand;
                            if (cmd.Shortcut.HasValue && Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                            { 
                                var keyGesture = new KeyGesture(key, cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None);
                                nativeMenuItem.Gesture = keyGesture;
                            }
                        }
                        item = nativeMenuItem;
                    }
                    
                    topLevelItem.Menu.Add(item);
                }
                
                ((NativeMenu)targetLocation).Add(topLevelItem);
            }
            return viewModel;
        }
        
        
        public static readonly AvaloniaProperty MenuItemsGesturesProperty = AvaloniaProperty.RegisterAttached<Window, IList<IMainMenuItem>>("MenuItemsGestures",
            typeof(MenuBind),coerce: OnMenuGesturesChanged);
        
        public static IList<IMainMenuItem> GetMenuItemsGestures(Window control) => (IList<IMainMenuItem>)control.GetValue(MenuItemsGesturesProperty);
        public static void SetMenuItemsGestures(Window control, object value) => control.SetValue(MenuItemsGesturesProperty, value);
        
        private static IList<IMainMenuItem> OnMenuGesturesChanged(IAvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;

            var window = targetLocation as Window;
            if (window == null)
                return viewModel;

            foreach (var m in viewModel)
            {
                foreach (var subItem in m.SubItems)
                {
                    if (subItem is not IMenuCommandItem cmd)
                        continue;
                    
                    if (!cmd.Shortcut.HasValue || !Enum.TryParse(cmd.Shortcut.Value.Key, out Key key)) 
                        continue;
                    
                    var keyGesture = new KeyGesture(key, cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None);
                    var command = cmd.ItemCommand;
                    // ok, so this is terrible, but TextBox gestures handling is inside OnKeyDown
                    // which is executed AFTER handling application wise shortcuts
                    // However application wise shortcuts take higher priority
                    // and effectively TextBox doesn't handle copy/paste/cut/undo/redo -.-
                    command = OverrideCommand<TextBox>(command, Key.C, key, cmd, tb => tb.Copy());
                    command = OverrideCommand<TextBox>(command, Key.X, key, cmd, tb => tb.Cut());
                    command = OverrideCommand<TextBox>(command, Key.V, key, cmd, tb => tb.Paste());
                    command = OverrideCommand<TextBox>(command, Key.Z, key, cmd, Undo);
                    command = OverrideCommand<TextBox>(command, Key.Y, key, cmd, Redo);
                    
                    command = OverrideCommand<TextArea>(command, Key.Z, key, cmd, tb => GetTextEditor(tb)?.Undo());
                    command = OverrideCommand<TextArea>(command, Key.Y, key, cmd, tb => GetTextEditor(tb)?.Redo());
                    command = OverrideCommand<TextArea>(command, Key.C, key, cmd, tb => ApplicationCommands.Copy.Execute(null, tb));
                    command = OverrideCommand<TextArea>(command, Key.X, key, cmd, tb => ApplicationCommands.Cut.Execute(null, tb));
                    command = OverrideCommand<TextArea>(command, Key.V, key, cmd, tb => ApplicationCommands.Paste.Execute(null, tb));
                    
                    window.KeyBindings.Add(new KeyBinding(){Command = command, Gesture = keyGesture});
                }
            }
            return viewModel;
        }

        private static TextEditor? GetTextEditor(TextArea area)
        {
            return area.FindLogicalAncestorOfType<TextEditor>();
        }

        private static void Redo(TextBox tb)
        {
            ExecuteUndoRedo(tb, "Redo");
        }

        private static void Undo(TextBox tb)
        {
            ExecuteUndoRedo(tb, "Undo");
        }

        private static void ExecuteUndoRedo(TextBox tb, string methodName)
        {
            var field = tb.GetType().GetField("_undoRedoHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return;
            
            object? undoHelper = field.GetValue(tb);
            if (undoHelper == null)
                return;

            var undoMethod = undoHelper.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            if (undoMethod == null)
                return;

            undoMethod.Invoke(undoHelper, null);
        }

        private static ICommand OverrideCommand<T>(ICommand command, Key require, Key commandKey, IMenuCommandItem item, Action<T> func)
        {
            if (require != commandKey || !(item.Shortcut?.Control ?? false))
                return command;

            return new DelegateCommand(() =>
            {
                if (FocusManager.Instance.Current is T t)
                    func(t);
                else if (command.CanExecute(null))
                    command.Execute(null);
            }, () =>
            {
                if (FocusManager.Instance.Current is T t)
                    return true;
                return command.CanExecute(null);
            });
        }
    }
}