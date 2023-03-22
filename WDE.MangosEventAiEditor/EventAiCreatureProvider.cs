using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor
{
    [AutoRegisterToParentScope]
    public class EventAiCreatureProvider : EventAiSolutionItemProvider, IRelatedSolutionItemCreator, INumberSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryOrGuidProviderService> creatureEntryProvider;

        public EventAiCreatureProvider(Lazy<ICreatureEntryOrGuidProviderService> creatureEntryProvider) : base("Creature Ai Script",
            "Script any npc in game.",
            "document_creature_big")
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            int? entry = await creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new EventAiSolutionItem(entry.Value);
        }

        public override async Task<IReadOnlyCollection<ISolutionItem>> CreateMultipleSolutionItems()
        {
            var entries = await creatureEntryProvider.Value.GetEntriesFromService();
            return entries.Select(e => new EventAiSolutionItem(e)).ToList();
        }

        public Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related)
        {
            return Task.FromResult<ISolutionItem?>(
                new EventAiSolutionItem((int)related.Entry));
        }

        public bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related)
        {
            return related.Type == RelatedSolutionItem.RelatedType.CreatureEntry;
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number)
        {
            return Task.FromResult<ISolutionItem?>(new EventAiSolutionItem((int)number));
        }

        public string ParameterName => "CreatureParameter";
    }
}