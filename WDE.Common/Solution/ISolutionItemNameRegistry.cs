using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Solution
{
    public interface ISolutionItemNameRegistry
    {
        void Register<T>(ISolutionNameProvider<T> provider) where T : ISolutionItem;
        string GetName<T>(T item) where T : ISolutionItem;
    }
}
