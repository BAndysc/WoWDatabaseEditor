using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDataJsonProvider
    {
        IEnumerable<string> GetDefinitionSources();
    }
}