using Avalonia.Controls.Templates;

namespace AvaloniaStyles.Utils;

public interface IExtendedDataTemplate : IDataTemplate
{
    bool HasContent(object? data);
}