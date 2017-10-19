using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.History
{
    public abstract class HistoryHandler
    {
        public event EventHandler<IHistoryAction> ActionPush = delegate { };

        protected void PushAction(IHistoryAction action)
        {
            ActionPush(this, action);
        }
    }
}
