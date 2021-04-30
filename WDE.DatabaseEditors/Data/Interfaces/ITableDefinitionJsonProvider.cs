using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionJsonProvider
    {
        IEnumerable<(string file, string content)> GetDefinitionSources();
    }
}