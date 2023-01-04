using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.MangosEventAiEditor.Exporter
{
    [AutoRegister]
    [SingleInstance]
    public class MangosEventAiExporter : IEventAiExporter
    {
        private readonly IEventAiFactory eventAiFactory;
        private readonly IEventAiDataManager eventAiDataManager;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly ISolutionItemNameRegistry nameRegistry;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IMessageBoxService messageBoxService;

        public MangosEventAiExporter(IEventAiFactory eventAiFactory,
            IEventAiDataManager eventAiDataManager,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameRegistry,
            IDatabaseProvider databaseProvider,
            IMessageBoxService messageBoxService)
        {
            this.eventAiFactory = eventAiFactory;
            this.eventAiDataManager = eventAiDataManager;
            this.currentCoreVersion = currentCoreVersion;
            this.nameRegistry = nameRegistry;
            this.databaseProvider = databaseProvider;
            this.messageBoxService = messageBoxService;
        }
        
        public IEventAiLine[] ToDatabaseCompatibleEventAi(EventAiScript script)
        {
            if (script.Events.Count == 0)
                return Array.Empty<IEventAiLine>();

            var lines = new List<IEventAiLine>();
            uint id = (uint)Math.Abs(script.EntryOrGuid) * 100 + 1;
            
            foreach (var ev in script.Events)
            {
                AbstractEventAiLine newLine(EventAiEvent ev)
                {
                    var line = new AbstractEventAiLine();
                    line.CreatureIdOrGuid = script.EntryOrGuid;
                    line.Id = id++;
                    line.EventType = (byte)ev.Id;
                    line.EventChance = (uint)ev.Chance.Value;
                    line.EventFlags = (uint)ev.Flags.Value;
                    line.Comment = ev.Readable.RemoveTags() + " - ";
                    for (int i = 0; i < EventAiEvent.EventParamsCount; ++i)
                        line.SetEventParam(i, (int)ev.GetParameter(i).Value);
                    return line;
                }

                AbstractEventAiLine lastLine = newLine(ev);
                lines.Add(lastLine);
                int lastLineActions = 0;

                foreach (var action in ev.Actions)
                {
                    if (lastLineActions >= 3)
                    {
                        lastLine = newLine(ev);
                        lines.Add(lastLine);
                        lastLineActions = 0;
                    }

                    lastLine.SetActionType(lastLineActions, action.Id);
                    for (int i = 0; i < EventAiAction.ActionParametersCount; ++i)
                    {
                        lastLine.SetActionParam(lastLineActions, i, (int)action.GetParameter(i).Value);
                    }

                    if (action.Id != EventAiConstants.ActionDoNothing)
                        lastLine.Comment += action.Readable.RemoveTags() + ", ";

                    lastLineActions++;
                }
            }

            return lines.ToArray();
        }

        public async Task<IQuery> GenerateSql(IEventAiSolutionItem item, EventAiScript script)
        {
            return await new ExporterHelper(script, databaseProvider, item, this, currentCoreVersion, nameRegistry).GetSql();
        }
    }
}