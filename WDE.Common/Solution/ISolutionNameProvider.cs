using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Solution
{
    public interface ISolutionNameProvider
    {

    }

    public interface ISolutionNameProvider<T> : ISolutionNameProvider where T : ISolutionItem 
    {
        string GetName(T item);
    }
}
