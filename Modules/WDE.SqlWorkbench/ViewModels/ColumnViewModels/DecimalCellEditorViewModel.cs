using System.Linq;
using Avalonia.Data;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal class DecimalCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly DecimalSparseColumnData overrideData;
    private PublicMySqlDecimal? maxValue;
    private PublicMySqlDecimal? minValue;
    
    public int DecimalPlaces { get; }
    
    private PublicMySqlDecimal value;
    public PublicMySqlDecimal Value
    {
        get => value;
        set
        {
            if (this.value == value)
                return;
            
            if (maxValue.HasValue && value > maxValue)
                throw new DataValidationException($"Value cannot be greater than {maxValue}");
            
            if (minValue.HasValue && value < minValue)
                throw new DataValidationException($"Value cannot be less than {minValue}");
            
            isModified = true;
            this.value = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ValueAsString));
        }
    }
    
    public string ValueAsString
    {
        get => value.ToString();
        set
        {
            if (!PublicMySqlDecimal.TryParse(value, out var v, DecimalPlaces))
                throw new DataValidationException("Invalid decimal value");
            Value = v;
        }
    }

    public PublicMySqlDecimal MaxValue => maxValue ?? PublicMySqlDecimal.Zero;
    public PublicMySqlDecimal MinValue => minValue ?? PublicMySqlDecimal.Zero;
    
    public bool HasMinMaxValues => maxValue.HasValue || minValue.HasValue;

    public DecimalCellEditorViewModel(string? mySqlType, DecimalSparseColumnData overrideData, DecimalColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        DecimalPlaces = 0;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Numeric
            && type.AsNumeric() is { } numericType
            && numericType.Kind == NumericDataTypeKind.Decimal)
        {
            var totalLength = numericType.M ?? 10; // 10 is MySql default
            var fractionLength = numericType.D ?? 0;
            DecimalPlaces = fractionLength;
            var wholeLength = totalLength - fractionLength;
            var whole = new string('9', wholeLength);
            var fraction = new string('9', fractionLength);

            if (numericType.Unsigned)
                minValue = PublicMySqlDecimal.Zero;
            else
                minValue = PublicMySqlDecimal.FromSignWholeFractional(true, whole, fraction);
            maxValue = PublicMySqlDecimal.FromSignWholeFractional(false, whole, fraction);
        }
        else
        {
            maxValue = null;
            minValue = null;
        }
        value = overrideData.HasRow(rowIndex) ? overrideData[rowIndex] : data[rowIndex];
    }
    
    public override void ApplyChanges()
    {
        if (!isModified)
            return;

        overrideData.Override(rowIndex, IsNull ? null : value);
    }
}