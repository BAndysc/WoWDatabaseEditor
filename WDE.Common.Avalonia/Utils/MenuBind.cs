using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Menu;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Utils
{
    public static class MenuBind
    {
        public static readonly AvaloniaProperty<string> IconProperty = AvaloniaProperty.RegisterAttached<MenuItem, string>("Icon", typeof(MenuBind));
        public static string? GetIcon(MenuItem control) => (string?)control.GetValue(IconProperty);
        public static void SetIcon(MenuItem control, string value) => control.SetValue(IconProperty, value);

        public static readonly AvaloniaProperty<ImageUri> IconUriProperty = AvaloniaProperty.RegisterAttached<MenuItem, ImageUri>("IconUri", typeof(MenuBind));
        public static ImageUri? GetIconUri(MenuItem control) => (ImageUri?)control.GetValue(IconUriProperty);
        public static void SetIconUri(MenuItem control, ImageUri? value) => control.SetValue(IconUriProperty, value);

        private static IDisposable iconSubscription;
        private static IDisposable iconUriSubscription;
        static MenuBind()
        {
            iconSubscription = IconProperty.Changed.SubscribeAction(args =>
            {
                var menuItem = (MenuItem)args.Sender!;
                if (args.NewValue.HasValue)
                    menuItem.Icon = new WdeImage() { ImageUri = args.NewValue.Value };
            });
            iconUriSubscription = IconUriProperty.Changed.SubscribeAction(args =>
            {
                var menuItem = (MenuItem)args.Sender!;
                if (args.NewValue.HasValue)
                    menuItem.Icon = new WdeImage() { Image = args.NewValue.Value };
            });
        }

        public static readonly AvaloniaProperty MenuItemsProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, IList<IMainMenuItem>>("MenuItems",
            typeof(MenuBind),coerce: OnMenuChanged);
        
        public static IList<IMainMenuItem> GetMenuItems(AvaloniaObject control) => (IList<IMainMenuItem>?)control.GetValue(MenuItemsProperty) ?? new List<IMainMenuItem>();
        public static void SetMenuItems(AvaloniaObject control, object value) => control.SetValue(MenuItemsProperty, value);
        
        private static IList<IMainMenuItem> OnMenuChanged(AvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            if (viewModel == null)
                return new List<IMainMenuItem>();
            
            var systemWideControlModifier = KeyGestures.CommandModifier;
            
            NativeMenuItemBase item;
            var nativeMenu = new NativeMenu();
            foreach (var m in viewModel)
            {
                NativeMenuItem topLevelItem = new NativeMenuItem(m.ItemName.Replace("_", ""));
                var menu = new NativeMenu();
                foreach (var subItem in m.SubItems)
                {
                    if (subItem.ItemName == "Separator")
                        item = new NativeMenuItemSeparator();
                    else
                    {
                        var nativeMenuItem = new NativeMenuItem(subItem.ItemName.Replace("_", ""));
                        if (subItem is IMenuCommandItem cmd)
                        {
                            nativeMenuItem.Command = WrapCommand(targetLocation as Visual, cmd.ItemCommand, cmd);
                            if (cmd.Shortcut.HasValue && Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                            {
                                var modifier = cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None;
                                if (cmd.Shortcut.Value.Shift)
                                    modifier |= KeyModifiers.Shift;
                                var keyGesture = new KeyGesture(key, modifier);
                                nativeMenuItem.Gesture = keyGesture;
                            }
                        }
                        if (subItem is ICheckableMenuItem checkable)
                        {
                            nativeMenuItem.ToggleType = NativeMenuItemToggleType.CheckBox;
                            checkable.ToObservable(o => o.IsChecked)
                                .SubscribeAction(@is => nativeMenuItem.IsChecked = @is);
                        }
                        item = nativeMenuItem;
                    }

                    menu.Add(item);
                }

                topLevelItem.Menu = menu;
                
                nativeMenu.Add(topLevelItem);
            }
            NativeMenu.SetMenu(targetLocation, nativeMenu);
            return viewModel;
        }
        
        
        public static readonly AvaloniaProperty ManagedMenuItemsProperty = AvaloniaProperty.RegisterAttached<global::Avalonia.Controls.Menu, IList<IMainMenuItem>>("ManagedMenuItems",
            typeof(MenuBind),coerce: OnManagedMenuChanged);
        
        public static IList<IMainMenuItem> GetManagedMenuItems(global::Avalonia.Controls.Menu control) => (IList<IMainMenuItem>?)control.GetValue(ManagedMenuItemsProperty) ?? new List<IMainMenuItem>();
        public static void SetMenuItems(global::Avalonia.Controls.Menu control, object value) => control.SetValue(ManagedMenuItemsProperty, value);
        
        private static IList<IMainMenuItem> OnManagedMenuChanged(AvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            if (viewModel == null)
                return new List<IMainMenuItem>();
            
            var systemWideControlModifier = KeyGestures.CommandModifier;

            List<MenuItem> items = new List<MenuItem>();
            foreach (var m in viewModel)
            {
                MenuItem topLevelItem = new MenuItem(){Header = m.ItemName.Replace("_", "")};
                var subItems = new List<Control>();
                foreach (var subItem in m.SubItems)
                {
                    if (subItem.ItemName == "Separator")
                        subItems.Add(new Separator());
                    else
                    {
                        var nativeMenuItem = new MenuItem(){Header=subItem.ItemName.Replace("_", "")};
                        if (subItem is IMenuCommandItem cmd)
                        {
                            nativeMenuItem.Command = WrapCommand(targetLocation as Visual, cmd.ItemCommand, cmd);
                            if (cmd.Shortcut.HasValue && Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                            { 
                                var modifier = cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None;
                                if (cmd.Shortcut.Value.Shift)
                                    modifier |= KeyModifiers.Shift;
                                var keyGesture = new KeyGesture(key, modifier);
                                nativeMenuItem.HotKey = keyGesture;
                            }
                        }
                        if (subItem is ICheckableMenuItem checkable)
                        {
                            var cb = new CheckBox();
                            nativeMenuItem.Icon = cb;
                            checkable.ToObservable(o => o.IsChecked)
                                .SubscribeAction(@is => cb.IsChecked = @is);
                        }
                        subItems.Add(nativeMenuItem);
                    }
                }

                topLevelItem.ItemsSource = subItems;
                items.Add(topLevelItem);

                ((global::Avalonia.Controls.Menu)targetLocation).ItemsSource = items;
            }
            return viewModel;
        }
        
        
        public static readonly AvaloniaProperty MenuItemsGesturesProperty = AvaloniaProperty.RegisterAttached<Control, IList<IMainMenuItem>>("MenuItemsGestures",
            typeof(MenuBind),coerce: OnMenuGesturesChanged);
        
        public static IList<IMainMenuItem> GetMenuItemsGestures(Control control) => (IList<IMainMenuItem>?)control.GetValue(MenuItemsGesturesProperty) ?? new List<IMainMenuItem>();
        public static void SetMenuItemsGestures(Control control, object value) => control.SetValue(MenuItemsGesturesProperty, value);

        private static ICommand WrapCommand(Visual? owner, ICommand command, IMenuCommandItem cmd)
        {
            if (!cmd.Shortcut.HasValue || !Enum.TryParse(cmd.Shortcut.Value.Key, out Key key)) 
                return command;

            var modifierKey = (cmd.Shortcut.Value.Shift ? KeyModifiers.Shift : KeyModifiers.None);

            // ok, so this is terrible, but TextBox gestures handling is inside OnKeyDown
            // which is executed AFTER handling application wise shortcuts
            // However application wise shortcuts take higher priority
            // and effectively TextBox doesn't handle copy/paste/cut/undo/redo -.-
            var original = command;
            command = OverrideCommand<ICustomCopyPaste>(owner, command, Key.C, KeyModifiers.None, key, modifierKey, cmd, tb => tb.DoCopy());
            command = OverrideCommand<ICustomCopyPaste>(owner, command, Key.V, KeyModifiers.None, key, modifierKey, cmd, tb => tb.DoPaste().ListenErrors());
            command = OverrideCommand<TextBox>(owner, command, Key.C, KeyModifiers.None, key, modifierKey, cmd, tb => tb.Copy());
            command = OverrideCommand<TextBox>(owner, command, Key.X, KeyModifiers.None, key, modifierKey, cmd, tb => tb.Cut());
            command = OverrideCommand<TextBox>(owner, command, Key.V, KeyModifiers.None, key, modifierKey, cmd, tb => tb.Paste());
            command = OverrideCommand<TextBox>(owner, command, Key.Z, KeyModifiers.None, key, modifierKey, cmd, Undo);
            command = OverrideCommand<TextBox>(owner, command, Key.Y, KeyModifiers.None, key, modifierKey, cmd, Redo);
            command = OverrideCommand<TextBox>(owner, command, Key.Z, KeyModifiers.Shift, key, modifierKey, cmd, Redo);
            command = OverrideCommand<FixedTextBox>(owner, command, Key.V, KeyModifiers.None, key, modifierKey, cmd, tb => tb.CustomPaste());

            command = OverrideCommand<TextArea>(owner, command, Key.Z, KeyModifiers.None, key, modifierKey, cmd, tb =>
            {
                var te = GetTextEditor(tb);
                if (te == null || te.Document.UndoStack.SizeLimit == 0)
                    return false;
                te.Undo();
                return true;
            });
            command = OverrideCommand<TextArea>(owner, command, Key.Z, KeyModifiers.Shift, key, modifierKey, cmd, tb =>
            {
                var te = GetTextEditor(tb);
                if (te == null || te.Document.UndoStack.SizeLimit == 0)
                    return false;
                te.Redo();
                return true;
            });
            command = OverrideCommand<TextArea>(owner, command, Key.Y, KeyModifiers.None, key, modifierKey, cmd, tb =>
            {
                var te = GetTextEditor(tb);
                if (te == null || te.Document.UndoStack.SizeLimit == 0)
                    return false;
                te.Redo();
                return true;
            });
            command = OverrideCommand<TextArea>(owner, command, Key.C, KeyModifiers.None, key, modifierKey, cmd, tb => ApplicationCommands.Copy.Execute(null, tb));
            command = OverrideCommand<TextArea>(owner, command, Key.X, KeyModifiers.None, key, modifierKey, cmd, tb => ApplicationCommands.Cut.Execute(null, tb));
            command = OverrideCommand<TextArea>(owner, command, Key.V, KeyModifiers.None, key, modifierKey, cmd, tb => ApplicationCommands.Paste.Execute(null, tb));

            var newCommand = new DelegateCommand(() => command.Execute(null), () => command.CanExecute(null));
            original.CanExecuteChanged += (_, _) => newCommand.RaiseCanExecuteChanged();
            return newCommand;
        }
        
        private static IList<IMainMenuItem> OnMenuGesturesChanged(AvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            if (viewModel == null)
                return new List<IMainMenuItem>();
            
            var systemWideControlModifier = KeyGestures.CommandModifier;

            var owner = targetLocation as Control;
            if (owner == null)
                return viewModel;

            foreach (var m in viewModel)
            {
                foreach (var subItem in m.SubItems)
                {
                    if (subItem is not IMenuCommandItem cmd)
                        continue;

                    if (!cmd.Shortcut.HasValue || !Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                        continue;

                    var modifier = cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None;
                    if (cmd.Shortcut.Value.Shift)
                        modifier |= KeyModifiers.Shift;
                    var keyGesture = new KeyGesture(key, modifier);
                    var command = WrapCommand(targetLocation as Visual, cmd.ItemCommand, cmd);

                    owner.KeyBindings.Add(new KeyBinding(){Command = command, Gesture = keyGesture});
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

        private static ICommand OverrideCommand<T>(Visual? owner, ICommand command, Key require, KeyModifiers requireModifier, Key commandKey, KeyModifiers commandModifier, IMenuCommandItem item, Func<T, bool> func)
        {
            var focusManager = TopLevel.GetTopLevel(owner)?.FocusManager;
            
            if (require != commandKey || !(item.Shortcut?.Control ?? false) || requireModifier != commandModifier)
                return command;

            return new DelegateCommand(() =>
            {
                bool executed = false;
                if (focusManager?.GetFocusedElement() is T t)
                    executed = func(t);
                if (!executed && command.CanExecute(null))
                    command.Execute(null);
            }, () =>
            {
                if (focusManager?.GetFocusedElement() is T t)
                    return true;
                return command.CanExecute(null);
            });
        }
        
        private static ICommand OverrideCommand<T>(Visual? owner, ICommand command, Key require, KeyModifiers requireModifier, Key commandKey, KeyModifiers commandModifier, IMenuCommandItem item, Action<T> func)
        {
            var focusManager = TopLevel.GetTopLevel(owner)?.FocusManager;

            if (require != commandKey || !(item.Shortcut?.Control ?? false) || requireModifier != commandModifier)
                return command;

            return new DelegateCommand(() =>
            {
                if (focusManager?.GetFocusedElement() is T t)
                    func(t);
                else if (command.CanExecute(null))
                    command.Execute(null);
            }, () =>
            {
                if (focusManager?.GetFocusedElement() is T t)
                    return true;
                return command.CanExecute(null);
            });
        }
    }
}
