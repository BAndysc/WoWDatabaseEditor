using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.History
{
    public abstract class HistoryHandler
    {
        public event EventHandler<IHistoryAction> ActionPush = delegate { };

        private List<IHistoryAction> bulkEditing = new List<IHistoryAction>();
        private bool inBulkEditing = false;
        
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
