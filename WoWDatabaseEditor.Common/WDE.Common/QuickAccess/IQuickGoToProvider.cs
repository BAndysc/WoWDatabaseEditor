using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

[NonUniqueProvider]
public interface IQuickGoToProvider
{
    string Name { get; }
    string ParameterKey { get; }
    int Order => 0;
    ISolutionItem Create(long value);
}