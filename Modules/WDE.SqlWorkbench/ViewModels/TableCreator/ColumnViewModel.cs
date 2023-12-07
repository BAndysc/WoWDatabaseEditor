using System;
using System.Collections.Generic;
using System.Linq;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class ColumnViewModel : ObservableBase
{
    private ColumnInfo? originalColumnInfo;
    [Notify] private string columnName = "";
    [Notify] private bool primaryKey;
    [Notify] private bool autoIncrement;
    [Notify] private bool notNull;
    [Notify] private DataTypeViewModel dataType = new DataTypeViewModel("");
    [Notify] [AlsoNotify(nameof(HasDefaultValue))] private string? defaultValue = null;
    [Notify] private CharsetViewModel? charset;
    [Notify] private CollationViewModel? collation;

    public readonly MySqlType? OriginalMySqlType;
    
    public ColumnInfo? OriginalColumnInfo => originalColumnInfo;

    public bool HasDefaultValue
    {
        get => defaultValue != null;
        set
        {
            if (value)
            {
                if (defaultValue == null)
                    DefaultValue = "";
            }
            else
            {
                DefaultValue = null;
            }
        }
    }
    
    public bool IsNew => originalColumnInfo == null;

    public bool IsModified => originalColumnInfo is { } info &&
                              (info.Name != ColumnName ||
                               //info.IsPrimaryKey != PrimaryKey || // primary key is not part of the column definition in the query
                               info.IsAutoIncrement != AutoIncrement ||
                               info.IsNullable != !NotNull ||
                               OriginalMySqlType != DataType.Type ||
                               info.DefaultValue != DefaultValue ||
                               info.Charset != CharsetToSqlValue(charset) ||
                               info.Collation != CollationToSqlValue(collation)) ||
                              IsPositionChanged;
    
    private static string? CharsetToSqlValue(CharsetViewModel? charset) => charset == null || charset.IsDefaultPlaceholder ? null : charset.Name;

    private static string? CollationToSqlValue(CollationViewModel? collation) => collation == null || collation.IsDefaultPlaceholder ? null : collation.Name;
    
    public bool IsPositionChanged { get; set; }
    
    public ColumnViewModel()
    {
    }
    
    public ColumnViewModel(ColumnInfo columnInfo, IReadOnlyList<CharsetViewModel> allCharsets)
    {
        originalColumnInfo = columnInfo;
        ColumnName = columnInfo.Name;
        PrimaryKey = columnInfo.IsPrimaryKey;
        AutoIncrement = columnInfo.IsAutoIncrement;
        NotNull = !columnInfo.IsNullable;
        if (!MySqlType.TryParse(columnInfo.Type, out var mysqlType))
            throw new Exception("Unknown type " + columnInfo.Type);
        OriginalMySqlType = mysqlType;
        DataType = new DataTypeViewModel(mysqlType);
        DefaultValue = columnInfo.DefaultValue;
        Charset = allCharsets.FirstOrDefault(c => c.Name == columnInfo.Charset);
        Collation = Charset?.Collations.FirstOrDefault(c => c.Name == columnInfo.Collation);
    }

    public override string ToString() => columnName;
}