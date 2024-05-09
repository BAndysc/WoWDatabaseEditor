using System;
using System.Collections;
using Avalonia;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views.Common;

public class PhantomCompletionComboBox : BasePhantomCompletionComboBox
{
    private BaseDatabaseCellViewModel cellModel = null!;
    private ColumnFullName column;
    private ITableContext context = null!;

    public void Spawn(Visual parent, Rect position, string? initialText, BaseDatabaseCellViewModel cellModel, ITableContext context, ColumnFullName column)
    {
        this.cellModel = cellModel;
        this.column = column;
        this.context = context;
        Spawn(parent, position, initialText, cellModel.Items, cellModel.OptionValue);
    }

    protected override void Cleanup(CompletionComboBox element)
    {
        base.Cleanup(element);
        cellModel = null!;
        column = default;
        context = null!;
    }

    protected override void Save(CompletionComboBox element)
    {
        if (cellModel.IsLongValue)
        {
            long? newValue = null;
            if (element.SelectedItem is BaseDatabaseCellViewModel.ParameterOption option)
                newValue = option.Value;
            else if (long.TryParse(element.SearchText, out var longVal))
                newValue = longVal;

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
        }
        else
        {
            string? strValue = null;
            if (element.SelectedItem is BaseDatabaseCellViewModel.ParameterStringOption strOption)
                strValue = strOption.Key;
            else if (!string.IsNullOrWhiteSpace(element.SearchText))
                strValue = element.SearchText;
            
            if (strValue != null)
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
}