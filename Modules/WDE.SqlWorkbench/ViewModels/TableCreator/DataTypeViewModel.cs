using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal class DataTypeViewModel
{
    public DataTypeViewModel(MySqlType? type)
    {
        Type = type;
        CustomTypeText = null;
    }
    
    public DataTypeViewModel(string customTypeText)
    {
        Type = null;
        CustomTypeText = customTypeText;
    }

    public MySqlType? Type { get; }
    public string? CustomTypeText { get; }

    public string Text => Type?.ToString() ?? CustomTypeText ?? "";

    public override string ToString() => Text;
}