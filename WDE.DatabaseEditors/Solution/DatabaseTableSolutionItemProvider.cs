using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemProviderProvider : ISolutionItemProviderProvider
    {
        private readonly ITableDefinitionProvider definitionProvider;
        private readonly ITableOpenService tableOpenService;
        private readonly IDatabaseProvider databaseProvider;

        public DatabaseTableSolutionItemProviderProvider(ITableDefinitionProvider definitionProvider,
            ITableOpenService tableOpenService,
            IDatabaseProvider databaseProvider)
        {
            this.definitionProvider = definitionProvider;
            this.tableOpenService = tableOpenService;
            this.databaseProvider = databaseProvider;
        }
        
        public IEnumerable<ISolutionItemProvider> Provide()
        {
            foreach (var definition in definitionProvider.Definitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, 
                    databaseProvider,
                    tableOpenService,
                    true);
            }
            foreach (var definition in definitionProvider.IncompatibleDefinitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, 
                    databaseProvider,
                    tableOpenService,
                    false);
            }
        }
    }

    internal class DatabaseTableSolutionItemProvider : ISolutionItemProvider, IRelatedSolutionItemCreator, INumberSolutionItemProvider
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly ITableOpenService tableOpenService;
        private readonly DatabaseTableDefinitionJson definition;
        private readonly ImageUri itemIcon;
        private bool isCompatible;
        
        internal DatabaseTableSolutionItemProvider(DatabaseTableDefinitionJson definition,
            IDatabaseProvider databaseProvider,
            ITableOpenService tableOpenService,
            bool isCompatible)
        {
            this.databaseProvider = databaseProvider;
            this.tableOpenService = tableOpenService;
            this.definition = definition;
            this.itemIcon = new ImageUri($"Icons/document_big.png");
            this.isCompatible = isCompatible;
        }

        public string GetName() => definition.Name;

        public ImageUri GetImage()
        {
            var icon = definition.IconPath?.Replace(".png", "_big.png");
            icon ??= "Icons/document_big.png";
            return new ImageUri(icon);
        }

        public string GetDescription() => definition.Description;
        
        public string GetGroupName() => definition.GroupName ?? "Database tables";

        public bool IsCompatibleWithCore(ICoreVersion core) => isCompatible;

        public Task<ISolutionItem?> CreateSolutionItem()
        {
            return tableOpenService.TryCreate(definition);
        }

        public Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related)
        {
            if (definition.Picker == "GossipMenuParameter" &&
                related.Type == RelatedSolutionItem.RelatedType.CreatureEntry)
            {
                var template = databaseProvider.GetCreatureTemplate((uint)related.Entry);
                if (template == null || template.GossipMenuId == 0)
                    return Task.FromResult<ISolutionItem?>(null);
                return tableOpenService.Create(definition, new DatabaseKey(template.GossipMenuId));
            }
            return tableOpenService.Create(definition, new DatabaseKey(related.Entry));
        }

        public bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related)
        {
            if (related.Type == RelatedSolutionItem.RelatedType.CreatureEntry &&
                definition.Picker == "GossipMenuParameter")
            {
                var template = databaseProvider.GetCreatureTemplate((uint)related.Entry);
                return template != null && template.GossipMenuId != 0;
            }
            
            return related.Type == RelatedSolutionItem.RelatedType.CreatureEntry &&
                   definition.Picker == "CreatureParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.GameobjectEntry &&
                   definition.Picker == "GameobjectParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.GossipMenu &&
                   definition.Picker == "GossipMenuParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.QuestEntry &&
                   definition.Picker == "QuestParameter";
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number)
        {
            return tableOpenService.Create(definition, new DatabaseKey(number));
        }

        public string ParameterName => definition.Picker;
    }
}