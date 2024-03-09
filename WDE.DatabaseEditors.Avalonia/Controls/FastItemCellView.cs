using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using WDE.DatabaseEditors.ViewModels;
using Thickness = Avalonia.Thickness;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class FastItemCellView : OpenableFastCellViewBase
    {
        public static readonly DirectProperty<FastItemCellView, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<FastItemCellView, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);
        
        public static readonly DirectProperty<FastItemCellView, IEnumerable<object>?> ItemsProperty = 
            AvaloniaProperty.RegisterDirect<FastItemCellView, IEnumerable<object>?>("Items", 
                o => o.Items, 
                (o, v) => o.Items = v);

        private IEnumerable<object>? items;
        public IEnumerable<object>? Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }

        private object? selectedItem;
        public object? SelectedItem
        {
            get => selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref selectedItem, value);
        }

        protected override bool DismissOnWindowFocusLost => true;

        protected override Type StyleKeyOverride => typeof(FastCellView);
        
        private CompletionComboBox? completionComboBox;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            partPanel = e.NameScope.Find<Panel>("PART_Panel");
            partText = e.NameScope.Find<TextBlock>("PART_text");
        }
        
        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            OpenForEditing();
            e.Handled = true;
            if (e.Text == "\n" || e.Text == " " || e.Text == "\r")
                return;
            DispatcherTimer.RunOnce(() =>
            {
                var args = new TextInputEventArgs();
                args.Handled = false;
                args.Text = e.Text;
                args.Route = e.Route;
                args.RoutedEvent = e.RoutedEvent;
                args.Source = completionComboBox;
                completionComboBox!.RaiseEvent(args);
            }, TimeSpan.FromMilliseconds(2));
        }

        private void CompletionComboBoxOnOnEnterPressed(object? sender, CompletionComboBox.EnterPressedArgs e)
        {
            var box = (CompletionComboBox)sender!;
            if (e.SelectedItem == null && long.TryParse(e.SearchText, out var l))
            {
                box.SelectedItem = new BaseDatabaseCellViewModel.ParameterOption(l, "(unknown)");
                e.Handled = true;
            }
        }

        private void CompletionComboBoxOnClosed()
        {
            EndEditing(true);
            Focus(NavigationMethod.Tab);
        }

        protected override Control CreateEditingControl()
        {
            completionComboBox = new CompletionComboBox();
            completionComboBox.Items = items;
            completionComboBox.SelectedItem = selectedItem;
            completionComboBox.HideButton = true;
            completionComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
            completionComboBox.Margin = new Thickness(partText?.Margin.Left ?? 0,partText?.Margin.Top ?? 0, 0, 0);
            completionComboBox.Padding = new Thickness(partText?.Padding.Left ?? 0,partText?.Padding.Top ?? 0, 0, 0);
            completionComboBox.Closed += CompletionComboBoxOnClosed;
            completionComboBox.OnEnterPressed += CompletionComboBoxOnOnEnterPressed;
            DispatcherTimer.RunOnce(() =>
            {
                if (completionComboBox != null)
                    completionComboBox.IsDropDownOpen = true;
            }, TimeSpan.FromMilliseconds(1));
            return completionComboBox;
        }

        protected override void PasteImpl(string text)
        {
            if (long.TryParse(text, out var l))
                Value = l;
        }

        public override void DoCopy()
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Value.ToString()!);
        }

        protected override void EndEditingInternal(bool commit)
        {
            if (completionComboBox == null)
                return;
            
            if (commit)
                SelectedItem = completionComboBox.SelectedItem;
            completionComboBox.Closed -= CompletionComboBoxOnClosed;
            completionComboBox.OnEnterPressed -= CompletionComboBoxOnOnEnterPressed;
            completionComboBox = null;
        }
    }
}
