using System;
using System.Collections.Generic;

namespace WDE.Common.History
{
    public abstract class HistoryHandler
    {
        private readonly List<IHistoryAction> bulkEditing = new();
        private bool inBulkEditing;
        public event EventHandler<IHistoryAction> ActionPush = delegate { };
        public event EventHandler<IHistoryAction> ActionDone = delegate { };

        protected IDisposable WithinBulk(string name)
        {
            StartBulkEdit();
            return new EndBulkEditing(this, name);
        }
        
        protected void StartBulkEdit()
        {
            inBulkEditing = true;
            bulkEditing.Clear();
        }

        protected void EndBulkEdit(string name)
        {
            if (inBulkEditing)
            {
                inBulkEditing = false;
                if (bulkEditing.Count > 0)
                    PushAction(new CompoundHistoryAction(name, bulkEditing.ToArray()));
            }
        }

        protected void PushAction(IHistoryAction action)
        {
            if (inBulkEditing)
                bulkEditing.Add(action);
            else
                ActionPush(this, action);
        }
        
        protected void DoAction(IHistoryAction action)
        {
            if (inBulkEditing)
                throw new Exception("Cannot execute action while bulk editing!");
            else
                ActionDone(this, action);
        }

        private readonly struct EndBulkEditing : System.IDisposable
        {
            private readonly HistoryHandler historyHandler;
            private readonly string name;

            public EndBulkEditing(HistoryHandler historyHandler, string name)
            {
                this.historyHandler = historyHandler;
                this.name = name;
            }

            public void Dispose()
            {
                historyHandler.EndBulkEdit(name);
            }
        }
    }
}