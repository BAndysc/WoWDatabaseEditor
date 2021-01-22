using System;
using System.Diagnostics.CodeAnalysis;

namespace WDE.MVVM.Observable
{
    internal class DelegateObserver<T> : IObserver<T>
    {
        private readonly Action<T> onNext;

        public DelegateObserver(Action<T> onNext)
        {
            this.onNext = onNext;
        }
        
        [ExcludeFromCodeCoverage]
        public void OnCompleted() {}

        [ExcludeFromCodeCoverage]
        public void OnError(Exception error) {}

        public void OnNext(T value)
        {
            onNext.Invoke(value);
        }
    }
}