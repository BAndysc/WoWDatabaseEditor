using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace WDE.MVVM
{
    internal class NotifyPropertyToObservable<T, R> : IObservable<T> where R : INotifyPropertyChanged
    {
        private readonly R property;
        private readonly Func<R, T> getter;
        private readonly string propertyName;

        public NotifyPropertyToObservable(R property, Expression<Func<R, T>> getter)
        {
            this.property = property;
            this.getter = getter.Compile();

            if (!(getter.Body is MemberExpression me))
                throw new ArgumentException("Getter has to return property and nothing else");
            
            if ((me.Member.MemberType & MemberTypes.Property) == 0)
                throw new ArgumentException("Getter has to return property, not a field");

            propertyName = me.Member.Name;
        }

        public NotifyPropertyToObservable(R property, Expression<Func<T>> getter)
        {
            this.property = property;
            var compiled = getter.Compile();
            this.getter = _ => compiled();

            if (!(getter.Body is MemberExpression me))
                throw new ArgumentException("Getter has to return property and nothing else");
            
            if ((me.Member.MemberType & MemberTypes.Property) == 0)
                throw new ArgumentException("Getter has to return property, not a field");

            propertyName = me.Member.Name;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return new Subscription(observer, getter, propertyName, property);
        }

        class Subscription : IDisposable
        {
            private readonly IObserver<T> observer;
            private readonly Func<R, T> getter;
            private readonly string propertyChanged;
            private readonly R notifyPropertyChanged;

            public Subscription(IObserver<T> observer, Func<R, T> getter, string propertyChanged, R notifyPropertyChanged)
            {
                this.observer = observer;
                this.getter = getter;
                this.propertyChanged = propertyChanged;
                this.notifyPropertyChanged = notifyPropertyChanged;
                notifyPropertyChanged.PropertyChanged += NotifyPropertyChangedOnPropertyChanged;
                observer.OnNext(getter(notifyPropertyChanged));
            }

            private void NotifyPropertyChangedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyChanged)
                    observer.OnNext(getter(notifyPropertyChanged));
            }

            public void Dispose()
            {
                notifyPropertyChanged.PropertyChanged -= NotifyPropertyChangedOnPropertyChanged;
            }
        }
    }
}