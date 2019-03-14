using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionNameProvider
    {

    }

    [NonUniqueProvider]
    public interface ISolutionNameProvider<T> : ISolutionNameProvider where T : ISolutionItem 
    {
        string GetName(T item);
    }
}
