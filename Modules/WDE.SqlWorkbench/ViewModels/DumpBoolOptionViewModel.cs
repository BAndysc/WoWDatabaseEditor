using System.ComponentModel;
using System.Reflection;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.SqlDump;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class DumpBoolOptionViewModel : ObservableBase
{
    private readonly FieldInfo field;
    [Notify] private bool isChecked;
    public string Name { get; }
    public string Description { get; }

    public DumpBoolOptionViewModel(FieldInfo field)
    {
        this.field = field;
        Name = field.Name.ToTitleCase();
        Description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
    }

    public void Apply(ref MySqlDumpOptions options)
    {
        field.SetValueDirect(__makeref(options), isChecked);
    }
}