using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Parameters;

namespace WDE.Common.Providers
{
    public interface IItemFromListProvider
    {
        int? GetItemFromList(Dictionary<int, SelectOption> items, bool flag);
    }
}
