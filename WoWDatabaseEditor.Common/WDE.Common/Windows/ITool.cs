using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Windows
{
    [NonUniqueProvider]
    public interface IToolProvider
    {
        bool AllowMultiple { get; }
        string Name { get; }

        bool CanOpenOnStart { get; }
        ITool Provide();
    }
}