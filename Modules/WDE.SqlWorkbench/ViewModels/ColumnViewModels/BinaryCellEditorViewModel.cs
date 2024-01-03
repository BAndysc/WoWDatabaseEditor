using System;
using WDE.Common.Avalonia.Controls;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal class BinaryCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly BinarySparseColumnData overrideData;
    private int? maxLength;
    
    public CopyOnWriteMemory<byte> Bytes { get; private set; }

    public int Length
    {
        get => Bytes.Length;
        set
        {
            if (value == Bytes.Length)
                return;

            isModified = true;
            var newBytes = new byte[value];
            Bytes.Slice(0, Math.Min(value, Bytes.Length)).CopyTo(newBytes);
            Bytes = new CopyOnWriteMemory<byte>(newBytes);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Bytes));
            ApplyChanges();
        }
    }
    
    public BinaryCellEditorViewModel(string? mySqlType, BinarySparseColumnData overrideData, BinaryColumnData data, int rowIndex, bool nullable) : base(mySqlType, overrideData, data, rowIndex, nullable)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Text)
        {
            maxLength = type.AsText()!.Value.Length;
        }

        Bytes = overrideData.HasRow(rowIndex)
            ? new CopyOnWriteMemory<byte>(overrideData.AsWriteableMemory(rowIndex))
            : new CopyOnWriteMemory<byte>(data.AsReadOnlyMemory(rowIndex));
    }

    public override void ApplyChanges()
    {
        if (!isModified && !Bytes.IsModified)
            return;
        
        overrideData.OverrideNull(rowIndex, IsNull);
        if (!IsNull && Bytes.IsCopy)
            overrideData.Override(rowIndex, Bytes.ModifiedArray);
    }
}