using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using WDE.Common;
using WDE.Common.Sessions;

namespace WDE.Sessions.Sessions
{
    internal class EditorSession : EditorSessionStub, IEditorSession
    {
        private List<(ISolutionItem, string)> queries = new();

        public EditorSession(string name, string fileName, DateTime created, DateTime lastmodified) : base(name, fileName, created, lastmodified)
        {
        }

        public void Insert(ISolutionItem item, string query)
        {
            var indexOf = queries.FindIndex(pair => pair.Item1.Equals(item));
            if (indexOf == -1)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    queries.Add((item, query));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, queries.Count - 1));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(query))
                {
                    queries.RemoveAt(indexOf);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, indexOf));
                }
                else
                {
                    queries[indexOf] = (item, query);
                }
            }
        }

        public IEnumerator<(ISolutionItem, string)> GetEnumerator()
        {
            return queries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ISolutionItem? Find(ISolutionItem item)
        {
            return queries.Select(pair => pair.Item1).FirstOrDefault(x => x.Equals(item));
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}