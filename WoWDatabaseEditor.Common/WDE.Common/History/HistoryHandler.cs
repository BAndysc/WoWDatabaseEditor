using System;
using System.Collections.Generic;

namespace WDE.Common.History
{
    public abstract class HistoryHandler
    {
        private readonly List<IHistoryAction> bulkEditing = new();
        private bool inBulkEditing;
        public event EventHandler<IHistoryAction> ActionPush = delegate { };

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
    }
}