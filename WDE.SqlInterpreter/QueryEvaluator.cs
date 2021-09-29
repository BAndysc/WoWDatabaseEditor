using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using WDE.Module.Attributes;
using WDE.SqlInterpreter.Models;

namespace WDE.SqlInterpreter
{
    [AutoRegister]
    [SingleInstance]
    internal class QueryEvaluator : IQueryEvaluator
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

        private static SqlVisitor? CreateVisitor(string query)
        {
            try
            {
                var lexer = new MySqlLexer(new AntlrInputStream(query));
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