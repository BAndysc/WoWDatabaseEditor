using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.Common.Managers;

namespace WDE.Common.Providers
{
    [NonUniqueProvider]
    public interface IDataDefinitionsProvider
    {
        string GetDataCategoryName();
        IEnumerable<IDataDefinitionEditor> GetDataDefinitionEditors();
    }
}
