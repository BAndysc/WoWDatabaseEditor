using System;

namespace WDE.Common.Disposables
{
    public class ActionDisposable : IDisposable
    {
        private readonly Action dispose;

        public ActionDisposable(Action dispose)
        {
            this.dispose = dispose;
        }
        
        public void Dispose()
        {
            dispose.Invoke();
        }
    }
}