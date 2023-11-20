using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.QueryParser;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;
using WDE.SqlInterpreter;

namespace WDE.DatabaseEditors.Services
{
    [AutoRegister]
    [SingleInstance]
    public class QueryParserService : IQueryParserService
    {
        private readonly IQueryEvaluator queryEvaluator;
        private readonly Func<Context> contextGenerator;

        public QueryParserService(
            IQueryEvaluator queryEvaluator,
            Func<Context> contextGenerator)
        {
            this.queryEvaluator = queryEvaluator;
            this.contextGenerator = contextGenerator;
        }

        public Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query)
        {
            var context = contextGenerator();
            return context.GenerateItemsForQuery(query);
        }

        [AutoRegister]
        public class Context : IQueryParsingContext
        {
            private readonly IQueryEvaluator queryEvaluator;
            private readonly IList<IQueryParserProvider> parsers;
            private HashSet<DatabaseTable> missingTables = new();
            private List<ISolutionItem> found = new();
            private List<string> errors = new();

            public Context(IQueryEvaluator queryEvaluator,
                IEnumerable<IQueryParserProvider> parsers)
            {
                this.queryEvaluator = queryEvaluator;
                this.parsers = parsers.ToList();
            }

            public async Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query)
            {
                foreach (var q in queryEvaluator.Extract(query))
                {
                    bool parsed = false;
                    foreach (var parser in parsers)
                    {
                        if (q is UpdateQuery updateQuery)
                        {
                            if (await parser.ParseUpdate(updateQuery, this))
                            {
                                parsed = true;
                                break;
                            }
                        }
                        else if (q is DeleteQuery deleteQuery)
                        {
                            if (await parser.ParseDelete(deleteQuery, this))
                            {
                                parsed = true;
                                break;
                            }
                        }
                        else if (q is InsertQuery insertQuery)
                        {
                            if (await parser.ParseInsert(insertQuery, this))
                            {
                                parsed = true;
                                break;
                            }
                        }
                    }

                    if (!parsed)
                    {                            
                        missingTables.Add(q.TableName);
                    }
                }

                foreach (var parser in parsers)
                {
                    parser.Finish(this);
                }

                foreach (var missing in missingTables)
                    errors.Add($"Table `{missing}` is not supported in WDE, no item added to the session.");
        
                return (found, errors);
            }
            
            public void AddError(string error)
            {
                errors.Add(error);
            }

            public void ProduceItem(ISolutionItem item)
            {
                found.Add(item);
            }
        }
    }
}