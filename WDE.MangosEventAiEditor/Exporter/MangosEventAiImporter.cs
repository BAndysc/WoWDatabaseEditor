using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Exporter
{
    [AutoRegister]
    [SingleInstance]
    public class MangosEventAiImporter : IEventAiImporter
    {
        private readonly IEventAiFactory eventAiFactory;
        private readonly IEventAiDataManager eventAiDataManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseProvider databaseProvider;

        public MangosEventAiImporter(IEventAiFactory eventAiFactory,
            IEventAiDataManager eventAiDataManager,
            IMessageBoxService messageBoxService,
            IDatabaseProvider databaseProvider)
        {
            this.eventAiFactory = eventAiFactory;
            this.eventAiDataManager = eventAiDataManager;
            this.messageBoxService = messageBoxService;
            this.databaseProvider = databaseProvider;
        }
        
        public async Task Import(EventAiScript script, bool doNotTouchIfPossible, IList<IEventAiLine> lines)
        {
            foreach (var line in lines)
            {
                var @event = script.SafeEventFactory(line);

                if (@event == null)
                    continue;
                
                @event.Parent = script;
                script.Events.Add(@event);

                for (int i = 0; i < 3; ++i)
                {
                    var action = script.SafeActionFactory(line, i);
                    if (action == null)
                        continue;
                    
                    @event.Actions.Add(action);
                }
            }
        }
    }
}