using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Blueprints.Data;

namespace WDE.Blueprints.Managers
{
    public interface IBlueprintDefinitionsRegistry
    {
        IList<NodeDefinition> GetAllDefinitions();
    }
}
