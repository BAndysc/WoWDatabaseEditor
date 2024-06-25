using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionJsonProvider
    {
        Task<IEnumerable<(string file, string content)>> GetDefinitionSources();
        Task<IEnumerable<(string file, string content)>> GetDefinitionReferences();
        event Action? FilesChanged;
    }
}