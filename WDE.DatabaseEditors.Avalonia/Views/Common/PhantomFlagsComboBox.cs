using System;
using Avalonia;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow;

public class PhantomFlagsComboBox : PhantomControlBase<FlagComboBox>
{
    private IParameterValue<long> parameter = null!;
    private ITableContext context = null!;
    private string column = null!;

    public void Spawn(Visual parent, Rect position, IParameterValue<long> parameter, ITableContext context, string column)
    {
        this.parameter = parameter;
        this.context = context;
        this.column = column;
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
        parameter = null!;
        context = null!;
        column = null!;
    }

    protected override void Save(FlagComboBox element)
    {
        var multiSelect = context!.MultiSelectionEntities!;
        if (multiSelect.Count == 1)
            parameter!.Value = element.SelectedValue;
        else
        {
            using var _ = context.BulkEdit("Change parameter");
            context.MultiSelectionEntities!.Each(entity => entity.SetTypedCellOrThrow(column, element.SelectedValue));
        }
    }
}