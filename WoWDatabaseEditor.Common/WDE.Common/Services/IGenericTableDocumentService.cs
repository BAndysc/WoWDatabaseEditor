using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IGenericTableDocumentService
    {
        bool TryCreate(string definition, string[] columns, uint[] keys, object[][] data);
    }
}