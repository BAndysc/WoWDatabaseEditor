using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using WDE.Common.Database;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

public class NullableDatabaseColumnCompletionBox : TemplatedControl
{
    private string lastNonNullText = "";
    
    public static readonly DirectProperty<NullableDatabaseColumnCompletionBox, string?> ColumnNameProperty = AvaloniaProperty.RegisterDirect<NullableDatabaseColumnCompletionBox, string?>(nameof(ColumnName),
        o => o.ColumnName, 
        (o, v) => o.ColumnName = v, 
        defaultBindingMode: BindingMode.TwoWay);

    private string? columnName;
    public string? ColumnName
    {
        get
        {
            return columnName;
        }
        set
        {
            SetAndRaise(ColumnNameProperty, ref columnName, value);
        }
    }

    public DataDatabaseType Database
    {
        get => GetValue(DatabaseProperty);
        set => SetValue(DatabaseProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsNotNullProperty = AvaloniaProperty.Register<NullableDatabaseColumnCompletionBox, bool>(nameof(IsNotNull));
    public static readonly StyledProperty<DataDatabaseType> DatabaseProperty = AvaloniaProperty.Register<NullableDatabaseColumnCompletionBox, DataDatabaseType>(nameof(Database));

    static NullableDatabaseColumnCompletionBox()
    {
        ColumnNameProperty.Changed.AddClassHandler<NullableDatabaseColumnCompletionBox>((x, _) => x.UpdateIsNull());
        IsNotNullProperty.Changed.AddClassHandler<NullableDatabaseColumnCompletionBox>((x, _) => x.UpdateText());
    }

    private bool inEvent = false;
    public static readonly StyledProperty<string> TableNameProperty = AvaloniaProperty.Register<NullableDatabaseColumnCompletionBox, string>("TableName");

    private void UpdateText()
    {
        if (inEvent)
            return;
        
        inEvent = true;
        if (IsNotNull && columnName == null)
        {
            columnName = lastNonNullText;
        }
        else if (!IsNotNull)
        {
            lastNonNullText = columnName ?? "";
            columnName = null;
        }
        inEvent = false;
    }

    private void UpdateIsNull()
    {
        if (inEvent)
            return;
        
        inEvent = true;
        SetCurrentValue(IsNotNullProperty, !string.IsNullOrWhiteSpace(columnName));
        inEvent = false;
    }
    
    public bool IsNotNull
    {
        get => GetValue(IsNotNullProperty);
        set => SetValue(IsNotNullProperty, value);
    }

    public string TableName
    {
        get { return (string)GetValue(TableNameProperty); }
        set { SetValue(TableNameProperty, value); }
    }
}