using WDE.Common.Parameters;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public class ColumnValueTypeViewModel : ObservableBase
{
    public IParameter? Parameter { get; }
    public string? ParameterName { get; }
    public string? ValueTypeName { get; }

    public bool IsNumeric { get; }
    public bool IsParameter => Parameter != null;
    public bool IsValueType => Parameter == null;
    public string Name => (ParameterName ?? ValueTypeName)!;

    private ColumnValueTypeViewModel(IParameter parameter, string parameterName)
    {
        Parameter = parameter;
        ParameterName = parameterName;
        ValueTypeName = null!;
        IsNumeric = parameter is IParameter<long>;
    }

    private ColumnValueTypeViewModel(string valueTypeName, bool isNumeric)
    {
        Parameter = null!;
        ParameterName = null!;
        ValueTypeName = valueTypeName;
        IsNumeric = isNumeric;
    }

    public static ColumnValueTypeViewModel FromValueType(string name, bool isNumeric)
    {
        return new ColumnValueTypeViewModel(name, isNumeric);
    }

    public static ColumnValueTypeViewModel FromParameter<T>(IParameter<T> parameter, string name) where T : notnull
    {
        return new ColumnValueTypeViewModel(parameter, name);
    }

    public override string ToString()
    {
        return Name;
    }
}