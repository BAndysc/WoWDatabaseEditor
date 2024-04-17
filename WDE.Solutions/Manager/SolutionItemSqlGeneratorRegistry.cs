using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemSqlGeneratorRegistry : ISolutionItemSqlGeneratorRegistry
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private readonly Lazy<ICachedDatabaseProvider> cacheDatabaseProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly Dictionary<Type, object> sqlProviders = new();

        public SolutionItemSqlGeneratorRegistry(IEnumerable<ISolutionItemSqlProvider> providers, 
            Lazy<IDocumentManager> documentManager,
            Lazy<ICachedDatabaseProvider> cacheDatabaseProvider,
            IEventAggregator eventAggregator)
        {
            this.documentManager = documentManager;
            this.cacheDatabaseProvider = cacheDatabaseProvider;
            this.eventAggregator = eventAggregator;
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemSqlProvider provider in providers)
                Register((dynamic) provider);
        }

        public Task<IQuery> GenerateSql(ISolutionItem item)
        {
            foreach (var document in documentManager.Value.OpenedDocuments)
            {
                if (document is not ISolutionItemDocument solutionItemDocument)
                    continue;

                if (ReferenceEquals(solutionItemDocument.SolutionItem, item) && !solutionItemDocument.IsLoading)
                    return solutionItemDocument.GenerateQuery();
            }

            return GenerateSql((dynamic) item);
        }

        public async Task<IList<(ISolutionItem, IQuery)>> GenerateSplitSql(ISolutionItem item)
        {
            foreach (var document in documentManager.Value.OpenedDocuments)
            {
                if (document is not ISolutionItemDocument solutionItemDocument)
                    continue;

                if (ReferenceEquals(solutionItemDocument.SolutionItem, item) && !solutionItemDocument.IsLoading)
                {
                    if (solutionItemDocument is ISplitSolutionItemQueryGenerator splitGenerator)
                        return await splitGenerator.GenerateSplitQuery();
                    return new List<(ISolutionItem, IQuery)>(){(item, await solutionItemDocument.GenerateQuery())};
                }
            }

            return new List<(ISolutionItem, IQuery)>(){(item, await GenerateSql((dynamic) item))};
        }

        private void Register<T>(ISolutionItemSqlProvider<T> provider) where T : ISolutionItem
        {
            sqlProviders.Add(typeof(T), provider);
        }

        private async Task<IQuery> GenerateSql<T>(T item) where T : ISolutionItem
        {
            if (sqlProviders.TryGetValue(item.GetType(), out var provider))
            {
                return await ((ISolutionItemSqlProvider<T>)provider).GenerateSql(item);
            }
            else
            {
                return Queries.Raw(DataDatabaseType.World, $"--- INTERNAL WoW Database Editor ERROR ---\n\n{item.GetType()} unknown SQL generator. Development info: You need to register class implementing ISolutionItemSqlProvider<T> interface");
            }
        }
    }
}