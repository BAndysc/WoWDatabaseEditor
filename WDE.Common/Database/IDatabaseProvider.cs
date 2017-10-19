using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Database
{
    public interface IDatabaseProvider
    {
        ICreatureTemplate GetCreatureTemplate(uint entry);
        IEnumerable<ICreatureTemplate> GetCreatureTemplates();
        
        IGameObjectTemplate GetGameObjectTemplate(uint entry);
        IEnumerable<IGameObjectTemplate> GetGameObjectTemplates();
        
        IQuestTemplate GetQuestTemplate(uint entry);
        IEnumerable<IQuestTemplate> GetQuestTemplates();

        IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type);

        void InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script);
    }
}
