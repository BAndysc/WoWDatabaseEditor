using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Avalonia.Editor.Views.Editing;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class SmartGroupView : SelectableTemplatedControl
{
    private string header = "";
    private string? description;
    public static readonly DirectProperty<SmartGroupView, string> HeaderProperty = AvaloniaProperty.RegisterDirect<SmartGroupView, string>(nameof(Header), o => o.Header, (o, v) => o.Header = v);
    public static readonly DirectProperty<SmartGroupView, string?> DescriptionProperty = AvaloniaProperty.RegisterDirect<SmartGroupView, string?>(nameof(Description), o => o.Description, (o, v) => o.Description = v);
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<SmartGroupView, bool>(nameof(IsExpanded));
    public static readonly StyledProperty<ICommand?> ToggleAllCommandProperty = AvaloniaProperty.Register<SmartGroupView, ICommand?>(nameof(ToggleAllCommand));
    
    public static readonly AvaloniaProperty DeselectAllButGroupsRequestProperty =
        AvaloniaProperty.Register<SmartGroupView, ICommand>(nameof(DeselectAllButGroupsRequest));

    public ICommand DeselectAllButGroupsRequest
    {
        get => (ICommand?) GetValue(DeselectAllButGroupsRequestProperty) ?? AlwaysDisabledCommand.Command;
        set => SetValue(DeselectAllButGroupsRequestProperty, value);
    }

    public SmartGroupView()
    {
        AddHandler(DoubleTappedEvent, (sender, args) =>
        {
            if (DataContext is SmartGroup group)
            {
                OpenEditFlyout(group);
                args.Handled = true;
            }
        });
    }

    private void OpenEditFlyout(SmartGroup group)
    {
        var flyout = new Flyout();
        flyout.FlyoutPresenterClasses.Add("no-horiz-scroll");
        var view = new SmartGroupEditView();
        var vm = new SmartGroupEditViewModel(group);
        view.DataContext = vm;
        view.MinWidth = vm.DesiredWidth;
        view.MaxWidth = vm.DesiredWidth;
        view.Width = vm.DesiredWidth;
        flyout.Content = view;
        flyout.Placement = PlacementMode.BottomEdgeAlignedLeft;
        flyout.ShowAt(this);
    }

    public string Header
    {
        get => header;
        set => SetAndRaise(HeaderProperty, ref header, value);
    }
    
    public string? Description
    {
        get => description;
        set => SetAndRaise(DescriptionProperty, ref description, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }
    
    public ICommand? ToggleAllCommand
    {
        get => GetValue(ToggleAllCommandProperty);
        set => SetValue(ToggleAllCommandProperty, value);
    }

    protected override void DeselectOthers()
    {
        DeselectAllButGroupsRequest?.Execute(null);
    }
}