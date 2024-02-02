using System.Diagnostics.CodeAnalysis;
using WDE.Common.Debugging;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Inspector;

internal class AddDebugPointViewModel : ObservableBase
{
    [ExcludeFromCodeCoverage]
    public IDebugPointSource Source { get; }

    [ExcludeFromCodeCoverage]
    public string Name { get; }

    [ExcludeFromCodeCoverage]
    public AddDebugPointViewModel(IDebugPointSource source)
    {
        Source = source;
        Name = $"Add {source.Name}";
    }
}