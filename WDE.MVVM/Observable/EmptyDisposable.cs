using System;

namespace WDE.MVVM.Observable
{
    public struct EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}