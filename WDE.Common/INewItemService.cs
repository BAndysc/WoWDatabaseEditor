using System;
using System.Collections.Generic;
using System.Text;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface INewItemService
    {
        ISolutionItem GetNewSolutionItem();
    }
}
