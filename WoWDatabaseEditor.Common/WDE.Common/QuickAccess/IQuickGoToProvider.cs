using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

[NonUniqueProvider]
public interface IQuickGoToProvider
{
    string Name { get; }
    string ParameterKey { get; }
    ISolutionItem Create(long value);
}