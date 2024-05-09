using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow;

public class PhantomFlagsComboBox : BasePhantomFlagsComboBox
{
    private IParameterValue<long> parameter = null!;
    private ITableContext context = null!;
    private ColumnFullName column;

    public void Spawn(Visual parent, Rect position, string? initialText, IParameterValue<long> parameter, ITableContext context, ColumnFullName column)
    {
        this.parameter = parameter;
        this.context = context;
        this.column = column;
        Spawn(parent, position, initialText, this.parameter.Parameter.Items, parameter.Value);
    }

    protected override void Cleanup(FlagComboBox element)
    {
        base.Cleanup(element);
        parameter = null!;
        context = null!;
        column = default;
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