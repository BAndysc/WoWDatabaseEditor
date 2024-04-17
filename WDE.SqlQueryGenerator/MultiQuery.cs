using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    internal class MultiQuery : IMultiQuery
    {
        private List<IQuery?> queries = new();

        public DataDatabaseType Database { get; }

        public MultiQuery(DataDatabaseType database)
        {
            Database = database;
        }

        public ITable Table(DatabaseTable name)
        {
            if (name.Database != Database)
                throw new ArgumentException($"IMultiQuery may only store queries for a single database, this instance was build for {Database}, but trying to append {name.Database} database ({name.Table})");
            return new Table(name) { CurrentQuery = this };
        }
        
        public void Add(IQuery? query)
        {
            if (query != null)
            {
                if (query.Database != Database)
                    throw new ArgumentException($"IMultiQuery may only store queries for a single database, this instance was build for {Database}, but trying to append {query.Database} database");

                if (query is BlankQuery)
                    queries.Add(null);
                else
                    queries.Add(query);
            }
        }

        public void Prepend(IQuery? query)
        {
            if (query != null)
            {
                if (query.Database != Database)
                    throw new ArgumentException($"IMultiQuery may only store queries for a single database, this instance was build for {Database}, but trying to append {query.Database} database");

                if (query is BlankQuery)
                    queries.Insert(0, null);
                else
                    queries.Insert(0, query);
            }
        }

        public IQuery Close()
        {
            return new Query(new DummyMultiQuery(Database), string.Join(Environment.NewLine, queries.Select(q => (q?.QueryString ?? Environment.NewLine, q == null)).Where(q => q.Item2 || !string.IsNullOrWhiteSpace(q.Item1)).Select(q => q.Item1)));
        }
    }

    internal class DummyMultiQuery : IMultiQuery
    {
        public DummyMultiQuery(DataDatabaseType database)
        {
            Database = database;
        }

        public DataDatabaseType Database { get; }

        public void Add(IQuery? query)
        {
        }

        public void Prepend(IQuery? query)
        {
        }

        [ExcludeFromCodeCoverage]
        public IQuery Close()
        {
            throw new System.NotImplementedException();
        }

        [ExcludeFromCodeCoverage]
        public ITable Table(DatabaseTable name)
        {
            throw new System.NotImplementedException();
        }
    }
}
