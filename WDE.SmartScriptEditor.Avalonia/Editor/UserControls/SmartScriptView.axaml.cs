using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

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
            var panel = this.FindControl<VirtualizedSmartScriptPanel>("SmartPanel");
            e.TryGetPosition(panel, out var point);

            if (point.X < VirtualizedSmartScriptPanel.BreakpointsMargin)
            {
                e.Handled = true;
                return;
            }

            var dataContext = DataContext as SmartScriptEditorViewModel;
            var control = sender as Control;
            var contextMenu = control?.ContextMenu;
            
            if (contextMenu == null || dataContext == null || control == null)
                return;

            var smartDataManager = dataContext.SmartDataManager;
            var topLevel = control.GetVisualRoot() as TopLevel;
            var pointerOverElement = topLevel?.GetValue(TopLevel.PointerOverElementProperty);

            var dynamicMenuItems = dataContext.GetDynamicContextMenuForSelected();
            
            var items = contextMenu.Items;

            if (temporaryMenuItems.Count != 0)
            {
                LOG.LogError(LOG.NonCriticalInvalidStateEventId, "While opening a custom context menu, something is really fucked up, because there are old items?");
                temporaryMenuItems.Clear();
            }

            void AddMenuItem(string header, ImageUri? icon, ICommand command)
            {
                var menuItem = new MenuItem()
                {
                    Icon = icon == null ? null! : new WdeImage(){ Image = icon.Value },
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

            bool anythingAdded = false;

            if (pointerOverElement is FormattedTextBlock ftb)
            {
                if (ftb.OverContext is ParameterWithContext context)
                {
                    AddMenuItem("Copy parameter value", new ImageUri("Icons/icon_copy.png"), new DelegateCommand(() =>
                    {
                        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(context.Parameter.Value.ToString()).ListenErrors();
                    }));
                    anythingAdded = true;
                }
                else if (ftb.OverContext is MetaSmartSourceTargetEdit sourceTarget)
                {
                    SmartSource sourceOrTarget = sourceTarget.IsSource ? sourceTarget.RelatedAction.Source : sourceTarget.RelatedAction.Target;
                    if (sourceOrTarget.IsPosition)
                    {
                        AddMenuItem("Copy coords", new ImageUri("Icons/icon_copy.png"), new DelegateCommand(() =>
                        {
                            var coords = $"{sourceOrTarget.X} {sourceOrTarget.Y} {sourceOrTarget.Z} {sourceOrTarget.O}";
                            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(coords).ListenErrors();
                        }));
                        anythingAdded = true;   
                    }
                }
            }
            
            if (dynamicMenuItems != null && dynamicMenuItems.Count > 0)
            {
                foreach (var menu in dynamicMenuItems)
                    AddMenuItem(menu.Name, menu.Icon, menu);
                anythingAdded = true;
            }


            if (pointerOverElement is FormattedTextBlock ftb2 &&
                ftb2.OverContext is ParameterWithContext context2)
            {
                if (smartDataManager.TryGetRawData(context2.Context.SmartType, context2.Context.Id, out var data) &&
                    data.Parameters != null &&
                    context2.ParameterIndex.HasValue &&
                    context2.ParameterIndex.Value >= 0 &&
                    context2.ParameterIndex.Value < data.Parameters.Count &&
                    data.Parameters[context2.ParameterIndex.Value].Type == "SpellParameter")
                {
                    var spellDebugger = ViewBind.ResolveViewModel<IBaseSpellDebugger>();
                    if (spellDebugger.Enabled)
                    {
                        var spellId = (uint)context2.Parameter.Value;
                        var header = spellDebugger.IsDebuggingSpell(spellId)
                            ? "Remove spell from the debugger"
                            : "Add spell to the debugger";

                        if (anythingAdded)
                            AddSeparator();

                        AddMenuItem(header, null,
                            new AsyncAutoCommand(async () => { await spellDebugger.ToggleSpellDebugging(spellId); })
                                .WrapMessageBox<Exception>(ViewBind.ResolveViewModel<IMessageBoxService>()));
                        anythingAdded = true;
                    }
                }
            }

            if (anythingAdded)
            {
                AddSeparator();
                contextMenu.Closed += MenuClosed;   
            }
        }

        private void MenuClosed(object? sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null)
                return;

            var items = contextMenu.Items;

            for (int i = 0; i < temporaryMenuItems.Count; ++i)
                items.Remove(temporaryMenuItems[i]);
            temporaryMenuItems.Clear();
            contextMenu.Closed -= MenuClosed;
        }
    }
}
