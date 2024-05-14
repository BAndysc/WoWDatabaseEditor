using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WDE.Common.History
{
    public class HistoryHandler
    {
        private readonly List<IHistoryAction> bulkEditing = new();
        private int inBulkEditingNestLevel;
        private bool inPause;
        public event EventHandler<IHistoryAction> ActionPush = delegate { };
        public event EventHandler<IHistoryAction> ActionDone = delegate { };
        public event EventHandler<IHistoryAction> ActionDoneWithoutHistory = delegate { };

        public IDisposable WithinBulk(string name)
        {
            StartBulkEdit();
            return new EndBulkEditing(this, name);
        }
        
        protected void StartBulkEdit()
        {
            if (inBulkEditingNestLevel++ == 0)
            {
                bulkEditing.Clear();
            }
        }

        protected void EndBulkEdit(string name)
        {
            if (--inBulkEditingNestLevel == 0)
            {
                if (bulkEditing.Count > 0)
                    PushAction(new CompoundHistoryAction(name, bulkEditing.ToArray()));
                bulkEditing.Clear();
            }
        }

        public IDisposable Pause()
        {
            StartPause();
            return new EndPauseDisposable(this);
        }
        
        protected void StartPause()
        {
            inPause = true;
        }

        protected void EndPause()
        {
            if (inPause)
            {
                inPause = false;
            }
        }

        public void PushAction(IHistoryAction action)
        {
            if (inPause)
                return;
            
            if (inBulkEditingNestLevel > 0)
                bulkEditing.Add(action);
            else
                ActionPush(this, action);
        }
        
        public void DoAction(IHistoryAction action)
        {
            if (inPause)
                return;

            if (inBulkEditingNestLevel > 0)
            {
                ActionDoneWithoutHistory(this, action);
                bulkEditing.Add(action);
            }
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

        private readonly struct EndPauseDisposable : System.IDisposable
        {
            private readonly HistoryHandler historyHandler;

            public EndPauseDisposable(HistoryHandler historyHandler)
            {
                this.historyHandler = historyHandler;
            }

            public void Dispose()
            {
                historyHandler.EndPause();
            }
        }
    }
}