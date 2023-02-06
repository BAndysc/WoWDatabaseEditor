using System;
using Avalonia;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views.Common;

public class PhantomCompletionComboBox : PhantomControlBase<CompletionComboBox>
{
    private BaseDatabaseCellViewModel cellModel = null!;
    private string column = null!;
    private ITableContext context = null!;

    public void Spawn(Visual parent, Rect position, BaseDatabaseCellViewModel cellModel, ITableContext context, string column)
    {
        this.cellModel = cellModel;
        this.column = column;
        this.context = context;
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
        cellModel = null!;
        column = null!;
        context = null!;
    }

    protected override void Save(CompletionComboBox element)
    {
        long? newValue = null;
        string? strValue = null;
        if (element.SelectedItem is BaseDatabaseCellViewModel.ParameterOption option)
            newValue = option.Value;
        else if (long.TryParse(element.SearchText, out var longVal))
            newValue = longVal;
        else if (element.SelectedItem is BaseDatabaseCellViewModel.ParameterStringOption strOption)
            strValue = strOption.Value;
        else if (!string.IsNullOrWhiteSpace(element.SearchText))
            strValue = element.SearchText;

        if (newValue.HasValue)
        {
            if (context.MultiSelectionEntities!.Count == 1)
            {
                cellModel.AsLongValue = newValue.Value;
            }
            else
            {
                using var _ = context.BulkEdit("Change " + column);
                context.MultiSelectionEntities!.Each(entity => entity.SetTypedCellOrThrow(column, newValue.Value));
            }
        }
        else if (strValue != null)
        {
            if (context.MultiSelectionEntities!.Count == 1)
            {
                cellModel.AsStringValue = strValue;
            }
            else
            {
                using var _ = context.BulkEdit("Change " + column);
                context.MultiSelectionEntities!.Each(entity => entity.SetTypedCellOrThrow(column, strValue));
            }   
        }
    }
}