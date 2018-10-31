using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.Common.Providers
{
    [UniqueProvider]
    public interface IItemFromListProvider
    {
        int? GetItemFromList(Dictionary<int, SelectOption> items, bool flag);
    }
}
