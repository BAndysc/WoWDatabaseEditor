using System;
using System.Reflection.Metadata;
using AvaloniaEdit.Document;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal class StringCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly StringSparseColumnData overrideData;
    private int? maxLength;
    
    public TextDocument Document { get; } = new();
    
    public bool IsTooLong => maxLength.HasValue && Document.TextLength > maxLength.Value;
    
    public int Length => Document.TextLength;
    
    public StringCellEditorViewModel(string? mySqlType, StringSparseColumnData overrideData, StringColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Text)
        {
            maxLength = type.AsText()!.Value.Length;
        }
        Document.Text = (overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex]) ?? "";
        Document.TextChanged += DocumentOnTextChanged;
    }

    private void DocumentOnTextChanged(object? sender, EventArgs e)
    {
        RaisePropertyChanged(nameof(Length));
        if (maxLength.HasValue)
            RaisePropertyChanged(nameof(IsTooLong));
        isModified = true;
    }

    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : Document.Text);
    }
}