using System;
using System.ComponentModel;
using WDE.MVVM.Observable;

namespace WDE.MVVM
{
    internal class NotifyPropertyToUnitObservable<R> : IObservable<Unit> where R : INotifyPropertyChanged
    {
        private readonly R property;

        public NotifyPropertyToUnitObservable(R property)
        {
            this.property = property;
        }

        public IDisposable Subscribe(IObserver<Unit> observer)
        {
            return new Subscription(observer, property);
        }

        class Subscription : IDisposable
        {
            private readonly IObserver<Unit> observer;
            private readonly R notifyPropertyChanged;

            public Subscription(IObserver<Unit> observer, R notifyPropertyChanged)
            {
                this.observer = observer;
                this.notifyPropertyChanged = notifyPropertyChanged;
                notifyPropertyChanged.PropertyChanged += NotifyPropertyChangedOnPropertyChanged;
                observer.OnNext(default);
            }

            private void NotifyPropertyChangedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                observer.OnNext(default);
            }

            public void Dispose()
            {
                notifyPropertyChanged.PropertyChanged -= NotifyPropertyChangedOnPropertyChanged;
            }
        }
    }
}