using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.FindAnywhere;

[UniqueProvider]
public interface IFindAnywhereResultsViewModel : IDocument
{
    Task Search(IReadOnlyList<string> parameter, IReadOnlyList<long> value);
}