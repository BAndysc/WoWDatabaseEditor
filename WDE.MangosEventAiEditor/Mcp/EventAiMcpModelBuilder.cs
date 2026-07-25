using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Mcp;
using WDE.Common.Utils;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Editor.ViewModels;
using WDE.EventAiEditor.Models;

namespace WDE.MangosEventAiEditor.Mcp
{
    /// <summary>
    /// Shared helpers for the EventAI MCP tools: loading an <see cref="EventAiScript"/> from the database,
    /// converting between the model and its JSON representation and formatting inspection results.
    /// </summary>
    internal static class EventAiMcpModelBuilder
    {
        /// <summary>
        /// Finds an open EventAI editor document for the given entry/guid, so tools can operate
        /// on the live (possibly unsaved) in-memory model instead of the database state.
        /// Returns null when the document is not open.
        /// </summary>
        internal static EventAiEditorViewModel? FindOpenDocument(IDocumentManager documentManager, int entryOrGuid)
        {
            foreach (var document in documentManager.OpenedDocuments)
            {
                if (document is EventAiEditorViewModel vm &&
                    vm.SolutionItem is IEventAiSolutionItem eventAiItem &&
                    eventAiItem.EntryOrGuid == entryOrGuid)
                    return vm;
            }

            return null;
        }

        /// <summary>
        /// Returns the live script of an open document, throwing a clean error while the document is still loading
        /// (its history handler is not attached yet, so edits would not be undoable).
        /// </summary>
        internal static EventAiScript GetOpenDocumentScript(EventAiEditorViewModel document)
        {
            if (document.IsLoading)
                throw new McpToolException("The EventAI document for this entry is open but still loading, retry in a moment.");
            return document.Script;
        }

        internal static async Task<EventAiScript> LoadScriptFromDatabase(int entryOrGuid,
            IEventAiFactory factory,
            IEventAiDataManager dataManager,
            IEventAiDatabaseProvider databaseProvider,
            IEventAiImporter importer)
        {
            var item = new EventAiSolutionItem(entryOrGuid);
            EventAiScript script = new(item, factory, dataManager, new EmptyMessageBoxService());
            var lines = (await databaseProvider.GetScriptFor(entryOrGuid)).ToList();
            await importer.Import(script, true, lines);
            return script;
        }

        internal static EventAiScriptJson ToJson(EventAiScript script, IEventAiDataManager dataManager)
        {
            var events = new List<EventAiEventJson>();
            foreach (var ev in script.Events)
            {
                var eventJson = new EventAiEventJson
                {
                    Id = ev.Id,
                    Name = dataManager.Contains(EventOrAction.Event, ev.Id)
                        ? dataManager.GetRawData(EventOrAction.Event, ev.Id).Name
                        : null,
                    Readable = ev.Readable.RemoveTags(),
                    Chance = ev.Chance.Value,
                    PhaseMask = ev.Phases.Value,
                    Flags = ev.Flags.Value,
                    Params = ParamsToJson(ev, EventAiEvent.EventParamsCount),
                    Actions = new List<EventAiActionJson>()
                };

                foreach (var action in ev.Actions)
                {
                    eventJson.Actions.Add(new EventAiActionJson
                    {
                        Id = action.Id,
                        Name = dataManager.Contains(EventOrAction.Action, action.Id)
                            ? dataManager.GetRawData(EventOrAction.Action, action.Id).Name
                            : null,
                        Readable = action.Readable.RemoveTags(),
                        Comment = string.IsNullOrEmpty(action.Comment) ? null : action.Comment,
                        Params = ParamsToJson(action, EventAiAction.ActionParametersCount)
                    });
                }

                events.Add(eventJson);
            }

            return new EventAiScriptJson { Events = events };
        }

        private static List<EventAiParamJson>? ParamsToJson(EventAiBaseElement element, int count)
        {
            List<EventAiParamJson>? result = null;
            for (int i = 0; i < count; ++i)
            {
                var holder = element.GetParameter(i);
                // include used parameters and any unused parameter that still carries a value
                // (so the output round-trips exactly through eventai_update)
                if (!holder.IsUsed && holder.Value == 0)
                    continue;

                result ??= new List<EventAiParamJson>();
                var readable = holder.ToString();
                result.Add(new EventAiParamJson
                {
                    Index = i,
                    Name = holder.Name,
                    Value = holder.Value,
                    ReadableValue = readable == holder.Value.ToString() ? null : readable
                });
            }

            return result;
        }

        internal static EventAiScript FromJson(int entryOrGuid,
            EventAiScriptJson json,
            IEventAiFactory factory,
            IEventAiDataManager dataManager)
        {
            var item = new EventAiSolutionItem(entryOrGuid);
            EventAiScript script = new(item, factory, dataManager, new EmptyMessageBoxService());

            foreach (var ev in BuildEvents(json, factory))
            {
                ev.Parent = script;
                script.Events.Add(ev);
            }

            return script;
        }

        /// <summary>
        /// Replaces the whole events list of a live script (an open document's model) with the given
        /// events, as a single undoable bulk edit. Events are removed/added one by one because the
        /// editor's history handler does not track collection resets.
        /// </summary>
        internal static void ReplaceScriptEvents(EventAiScript target, List<EventAiEvent> newEvents)
        {
            using (target.BulkEdit("Replace EventAI script (MCP)"))
            {
                while (target.Events.Count > 0)
                    target.Events.RemoveAt(target.Events.Count - 1);

                foreach (var ev in newEvents)
                {
                    ev.Parent = target;
                    target.Events.Add(ev);
                }
            }
        }

        /// <summary>Builds detached model events from the JSON representation, validating ids and parameters.</summary>
        internal static List<EventAiEvent> BuildEvents(EventAiScriptJson json, IEventAiFactory factory)
        {
            if (json.Events == null!)
                throw new McpToolException("The script object must contain an 'events' array.");

            var result = new List<EventAiEvent>();
            int eventIndex = 0;
            foreach (var eventJson in json.Events)
            {
                eventIndex++;
                EventAiEvent ev;
                try
                {
                    ev = factory.EventFactory(eventJson.Id);
                }
                catch (Exception)
                {
                    throw new McpToolException($"Event #{eventIndex}: unknown event type id {eventJson.Id}. Use eventai_data_search with type=Event to list valid ids.");
                }

                ev.Chance.Value = eventJson.Chance ?? 100;
                ev.Phases.Value = eventJson.PhaseMask ?? 0;
                ev.Flags.Value = eventJson.Flags ?? 0;
                ApplyParams(ev, eventJson.Params, EventAiEvent.EventParamsCount, $"event #{eventIndex} ({eventJson.Id})");

                if (eventJson.Actions != null)
                {
                    if (eventJson.Actions.Count > 3)
                        throw new McpToolException($"Event #{eventIndex}: EventAI supports at most 3 actions per event, got {eventJson.Actions.Count}.");

                    int actionIndex = 0;
                    foreach (var actionJson in eventJson.Actions)
                    {
                        actionIndex++;
                        EventAiAction action;
                        try
                        {
                            action = factory.ActionFactory(actionJson.Id);
                        }
                        catch (Exception)
                        {
                            throw new McpToolException($"Event #{eventIndex}, action #{actionIndex}: unknown action type id {actionJson.Id}. Use eventai_data_search with type=Action to list valid ids.");
                        }

                        if (actionJson.Comment != null)
                            action.Comment = actionJson.Comment;
                        ApplyParams(action, actionJson.Params, EventAiAction.ActionParametersCount, $"event #{eventIndex}, action #{actionIndex} ({actionJson.Id})");
                        ev.AddAction(action);
                    }
                }

                result.Add(ev);
            }

            return result;
        }

        private static void ApplyParams(EventAiBaseElement element, List<EventAiParamJson>? prams, int count, string context)
        {
            if (prams == null)
                return;

            foreach (var param in prams)
            {
                int index;
                if (param.Index.HasValue)
                    index = param.Index.Value;
                else if (param.Name != null)
                {
                    index = -1;
                    for (int i = 0; i < count; ++i)
                    {
                        if (string.Equals(element.GetParameter(i).Name, param.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                        throw new McpToolException($"In {context}: no parameter named '{param.Name}'. Valid names: " +
                                                   string.Join(", ", Enumerable.Range(0, count).Select(i => $"'{element.GetParameter(i).Name}'")));
                }
                else
                    throw new McpToolException($"In {context}: each parameter needs either an 'index' (0-based) or a 'name'.");

                if (index < 0 || index >= count)
                    throw new McpToolException($"In {context}: parameter index {index} out of range, valid range is 0-{count - 1}.");

                element.GetParameter(index).Value = param.Value;
            }
        }

        internal static List<EventAiProblemJson> ToProblems(IReadOnlyList<IInspectionResult> inspections)
        {
            var result = new List<EventAiProblemJson>();
            foreach (var inspection in inspections)
            {
                result.Add(new EventAiProblemJson
                {
                    Severity = inspection.Severity.ToString(),
                    Message = inspection.Message,
                    Line = inspection.Line
                });
            }

            return result;
        }

        internal class EmptyMessageBoxService : IMessageBoxService
        {
            public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox) => Task.FromResult<T?>(default);
        }
    }
}
