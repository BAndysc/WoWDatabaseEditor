using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Ioc;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow;

public class CustomCellInteractor : SingleRow.CustomCellDrawerInteractorBase, ICustomCellInteractor
{
    private IParameterPickerService ParameterPickerService => ViewBind.ContainerProvider.Resolve<IParameterPickerService>();
    private PhantomFlagsComboBox flagPicker = new();
    private PhantomCompletionComboBox comboBox = new();
    private PhantomTextBox textBox = new();

    public bool SpawnEditorFor(string? initialText, Visual parent, Rect rect, ITableCell c)
    {
        if (c is not DatabaseCellViewModel cell)
            return false;

        if (cell.ActionCommand != null)
        {
            if (cell.ActionCommand.CanExecute(cell))
                cell.ActionCommand.Execute(cell);
            return true;
        }
        
        if (initialText != null)
            return false; // then use builtin-text editor

        var longValue = cell.ParameterValue as IParameterValue<long>;
        if (cell.UseFlagsPicker && longValue != null)
        {
            flagPicker.Spawn(parent, rect, longValue);
            return true;
        }
        if (cell.UseItemPicker)
        {
            comboBox.Spawn(parent, rect, cell);
            return true;
        }
        // if (cell.HasItems && longValue != null)
        // {
        //     Pick(longValue).ListenErrors();
        //     return true;
        // }

        return false;
    }

    public bool PointerDown(ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount)
    {
        if (c is not DatabaseCellViewModel cell)
            return false;
        
        return false;
    }
    
    public bool PointerUp(ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton)
    {
        if (c is not DatabaseCellViewModel cell)
            return false;

        var threeDots = GetThreeDotRectForCell(cellRect);
        if (leftButton && threeDots.Deflate(3).Contains(mouse) && cell.HasItems && !cell.UseItemPicker && !cell.UseFlagsPicker)
        {
            switch (cell.ParameterValue)
            {
                case IParameterValue<long> longValue:
                    Pick(longValue).ListenErrors();
                    break;
                case IParameterValue<string> stringValue:
                    Pick(stringValue).ListenErrors();
                    break;
            }
        }
        
        if (leftButton && cell.ActionCommand != null && cell.ActionCommand.CanExecute(cell))
        {
            cell.ActionCommand.Execute(cell);
            return true;
        }
        
        return false;
    }

    private async Task Pick<T>(IParameterValue<T> parameter) where T : notnull
    {
        var (value, ok) = await ParameterPickerService.PickParameter<T>(parameter.Parameter, parameter.Value!, parameter.Context);
        if (ok)
            parameter.Value = value;
    }
}


public class PhantomFlagsComboBox : PhantomControlBase<FlagComboBox>
{
    private IParameterValue<long>? parameter;

    public void Spawn(Visual parent, Rect position, IParameterValue<long> parameter)
    {
        this.parameter = parameter;
        var flagsComboBox = new FlagComboBox();
        flagsComboBox.Flags = parameter.Parameter.Items;
        flagsComboBox.SelectedValue = parameter.Value;
        flagsComboBox.HideButton = true;
        flagsComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
        flagsComboBox.Closed += CompletionComboBoxOnClosed;

        if (!AttachAsAdorner(parent, position, flagsComboBox))
            return;

        DispatcherTimer.RunOnce(() =>
        {
            flagsComboBox.IsDropDownOpen = true;
        }, TimeSpan.FromMilliseconds(1));
    }

    private void CompletionComboBoxOnClosed()
    {
        Despawn(true);
    }

    protected override void Cleanup(FlagComboBox element)
    {
        element.Closed -= CompletionComboBoxOnClosed;
        parameter = null;
    }

    protected override void Save(FlagComboBox element)
    {
        parameter!.Value = element.SelectedValue;
    }
}

public class PhantomCompletionComboBox : PhantomControlBase<CompletionComboBox>
{
    private BaseDatabaseCellViewModel? cellModel;

    public void Spawn(Visual parent, Rect position, BaseDatabaseCellViewModel cellModel)
    {
        this.cellModel = cellModel;
        var flagsComboBox = new CompletionComboBox();
        flagsComboBox.Items = cellModel.Items;
        flagsComboBox.SelectedItem = cellModel.OptionValue;
        flagsComboBox.HideButton = true;
        flagsComboBox.IsLightDismissEnabled = false; // we are handling it ourselves, without doing .Handled = true so that as soon as user press outside of popup, the click is treated as actual click
        flagsComboBox.Closed += CompletionComboBoxOnClosed;

        if (!AttachAsAdorner(parent, position, flagsComboBox))
            return;

        DispatcherTimer.RunOnce(() =>
        {
            flagsComboBox.IsDropDownOpen = true;
        }, TimeSpan.FromMilliseconds(1));
    }

    private void CompletionComboBoxOnClosed()
    {
        Despawn(true);
    }

    protected override void Cleanup(CompletionComboBox element)
    {
        element.Closed -= CompletionComboBoxOnClosed;
        cellModel = null;
    }

    protected override void Save(CompletionComboBox element)
    {
        if (cellModel != null)
        {
            if (element.SelectedItem is BaseDatabaseCellViewModel.ParameterOption option)
                cellModel.OptionValue = option;
            else if (long.TryParse(element.SearchText, out var longVal))
                cellModel.OptionValue = new BaseDatabaseCellViewModel.ParameterOption(longVal, "");
        }
    }
}