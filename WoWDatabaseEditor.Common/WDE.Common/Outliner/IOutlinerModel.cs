using System.Collections.Generic;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;

namespace WDE.Common.Outliner;

public interface IOutlinerModel : IEnumerable<(RelatedSolutionItem.RelatedType type, IFindAnywhereResult result)>
{
}