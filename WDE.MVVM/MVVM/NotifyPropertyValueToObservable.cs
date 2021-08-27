using System;

namespace WDE.MVVM
{
    internal class NotifyPropertyValueToObservable<T> : IObservable<PropertyValueChangedEventArgs> where T : INotifyPropertyValueChanged
    {
        private readonly T obj;

        public NotifyPropertyValueToObservable(T obj)
        {
            this.obj = obj;
        }

        public IDisposable Subscribe(IObserver<PropertyValueChangedEventArgs> observer)
        {
            return new Subscription(observer, obj);
        }

        class Subscription : IDisposable
        {
            private readonly IObserver<PropertyValueChangedEventArgs> observer;
            private readonly T notifyPropertyValueChanged;

            public Subscription(IObserver<PropertyValueChangedEventArgs> observer, T notifyPropertyValueChanged)
            {
                this.observer = observer;
                this.notifyPropertyValueChanged = notifyPropertyValueChanged;
                notifyPropertyValueChanged.PropertyValueChanged += NotifyPropertyValueChangedOnPropertyValueChanged;
            }

            private void NotifyPropertyValueChangedOnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
            {
                observer.OnNext(e);
            }

            public void Dispose()
            {
                notifyPropertyValueChanged.PropertyValueChanged -= NotifyPropertyValueChangedOnPropertyValueChanged;
            }
        }
    }
}