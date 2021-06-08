using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionItemDeserializerRegistry : ISolutionItemDeserializerRegistry
    {
        private readonly List<ISolutionItemDeserializer> deserializers;

        public SolutionItemDeserializerRegistry(IEnumerable<ISolutionItemDeserializer> deserializers)
        {
            this.deserializers = deserializers.ToList();
        }

        public bool TryDeserialize(ISmartScriptProjectItem item, out ISolutionItem? solutionItem)
        {
            foreach (var deserializer in deserializers)
            {
                if (deserializer.TryDeserialize(item, out solutionItem))
                    return true;
            }

            solutionItem = null;
            return false;
        }
    }
}