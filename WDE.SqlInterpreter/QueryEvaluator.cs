using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser.Models;
using WDE.Module.Attributes;

namespace WDE.SqlInterpreter
{
    public class BaseQueryEvaluator : IQueryEvaluator
    {
        private readonly string? worldTableName;
        private readonly string? hotfixTableName;
        private readonly DataDatabaseType defaultType;

        public BaseQueryEvaluator(string? worldTableName, string? hotfixTableName, DataDatabaseType defaultType)
        {
            this.worldTableName = worldTableName;
            this.hotfixTableName = hotfixTableName;
            this.defaultType = defaultType;
        }
        
        public IReadOnlyList<IBaseQuery> Extract(string query)
        {
            var visitor = CreateVisitor(query);
            return visitor?.Queries ?? new List<IBaseQuery>();
        }

        public IEnumerable<InsertQuery> ExtractInserts(string query)
        {
            var visitor = CreateVisitor(query);
            return visitor?.Inserts ?? Enumerable.Empty<InsertQuery>();
        }

        public IEnumerable<UpdateQuery> ExtractUpdates(string query)
        {
            var visitor = CreateVisitor(query);
            return visitor?.Updates ?? Enumerable.Empty<UpdateQuery>();
        }

        public IEnumerable<DeleteQuery> ExtractDeletes(string query)
        {
            var visitor = CreateVisitor(query);
            return visitor?.Deletes ?? Enumerable.Empty<DeleteQuery>();
        }

        private SqlVisitor? CreateVisitor(string query)
        {
            try
            {
                var lexer = new MySqlLexer(new CaseChangingCharStream(new AntlrInputStream(query), true));
                var tokens = new CommonTokenStream(lexer);
                var parser = new MySqlParser(tokens);
                parser.BuildParseTree = true;
                parser.RemoveErrorListeners();

                var visitor = new SqlVisitor(worldTableName, hotfixTableName, defaultType);
                visitor.Visit(parser.root());
                return visitor;
            }
            catch (Exception e)
            {
                LOG.LogError(e);
                return null;
            }
        }
    }
}