using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using WDE.Common.Services.QueryParser.Models;
using WDE.Module.Attributes;

namespace WDE.SqlInterpreter
{
    [AutoRegister]
    [SingleInstance]
    public class QueryEvaluator : IQueryEvaluator
    {
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

        private static SqlVisitor? CreateVisitor(string query)
        {
            try
            {
                var lexer = new MySqlLexer(new CaseChangingCharStream(new AntlrInputStream(query), true));
                var tokens = new CommonTokenStream(lexer);
                var parser = new MySqlParser(tokens);
                parser.BuildParseTree = true;
                parser.RemoveErrorListeners();

                var visitor = new SqlVisitor();
                visitor.Visit(parser.root());
                return visitor;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}