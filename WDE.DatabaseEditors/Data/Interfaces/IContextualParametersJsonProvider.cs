using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces;

[UniqueProvider]
public interface IContextualParametersJsonProvider
{
    Task<IEnumerable<(string file, string content)>> GetParameters();
}