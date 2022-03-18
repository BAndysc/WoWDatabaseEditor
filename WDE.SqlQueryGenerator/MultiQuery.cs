using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace WDE.SqlQueryGenerator
{
    internal class MultiQuery : IMultiQuery
    {
        private List<IQuery> queries = new();
        
        public MultiQuery()
        {
        }

        public ITable Table(string name)
        {
            return new Table(name) { CurrentQuery = this };
        }

        public void Add(IQuery query)
        {
            queries.Add(query);
        }

        public IQuery Close()
        {
            return new Query(new DummyMultiQuery(), string.Join(Environment.NewLine, queries.Select(q => q.QueryString).Where(q => !string.IsNullOrWhiteSpace(q))));
        }
    }

    internal class DummyMultiQuery : IMultiQuery
    {
        public void Add(IQuery query)
        {
        }

        [ExcludeFromCodeCoverage]
        public IQuery Close()
        {
            throw new System.NotImplementedException();
        }

        [ExcludeFromCodeCoverage]
        public ITable Table(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}