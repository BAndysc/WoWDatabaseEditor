using WDE.Module.Attributes;

namespace WDE.Common.Factories.Http
{
    [NonUniqueProvider]
    public interface IUserAgentPart
    {
        string Part { get; }
    }
}