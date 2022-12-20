using WDE.Module.Attributes;

namespace WDE.Common.Services;

[NonUniqueProvider]
public interface IGlobalService
{
    
}

[UniqueProvider]
public interface IGlobalServiceRoot : System.IDisposable
{
}