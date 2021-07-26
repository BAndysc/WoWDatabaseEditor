using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Prism.Mvvm;
using WDE.MVVM.Observable;

namespace WDE.MVVM
{
    public abstract class ObservableBase : BindableBase, IDisposable
    {
        private List<IDisposable> disposables = new();

        /// <summary>
        /// Subscribes into `obj` property extracted via `getter` (using INotifyPropertyChanged)
        /// every time the property is updated, OnPropertyChanged in this class will be fired
        ///
        /// AutoDisposes when this class is disposed 
        /// </summary>
        /// <param name="obj">INotifyPropertyChanged object, we will be linked to</param>
        /// <param name="getter">Property getter of obj. Must be always only single property getter p => p.PropertyMember</param>
        /// <param name="property">Name of property in this class</param>
        /// <typeparam name="T">Type of objects we will watch. Must implement INotifyPropertyChanged</typeparam>
        /// <typeparam name="R">Type of property we watch</typeparam>
        protected void Watch<T, R>(T? obj, Expression<Func<T, R>> getter, string property) where T : INotifyPropertyChanged
        {
            if (obj != null)
                AutoDispose(obj.Subscribe(getter, _ => RaisePropertyChanged(property)));
        }
        
        protected void Watch<T>(string property, params Expression<Func<T>>[] getters)
        {
            foreach (var getter in getters)
                AutoDispose(this.ToObservable(getter).SubscribeAction(_ => RaisePropertyChanged(property)));
        }

        /// <summary>
        /// Subscribes into `obj` property extracted via `getter` (using INotifyPropertyChanged)
        /// every time the property is updated, property in this class will be updated and OnPropertyChanged in this class will be fired
        ///
        /// AutoDisposes when this class is disposed 
        /// </summary>
        /// <param name="obj">INotifyPropertyChanged object, we will be linked to</param>
        /// <param name="getter">Property getter of obj. Must be always only single property getter p => p.PropertyMember</param>
        /// <param name="getSetter">Accessor to property in this class to be updated. Must be always only a single property getter p => p.PropertyMember. This property have to have a setter</param>
        /// <typeparam name="T">Type of objects we will watch. Must implement INotifyPropertyChanged</typeparam>
        /// <typeparam name="R">Type of property we watch</typeparam>
        protected void Link<T, R>(T? obj, Expression<Func<T, R>> getter, Expression<Func<R>> getSetter)  where T : INotifyPropertyChanged
        {
            if (obj == null)
                return;

            Link(obj.ToObservable(getter), getSetter);
        }
        
        /// <summary>
        /// Subscribes into given observable, each time observable produces a value,
        /// a property in this class is updated and OnPropertyChanged is fired
        /// </summary>
        /// <param name="observable">Observable to subscribe to</param>
        /// <param name="getSetter">Accessor to property in this class to be updated. Must be always only a single property getter p => p.PropertyMember. This property have to have a setter</param>
        /// <exception cref="Exception">Throws an exception when getSetter is in wrong form</exception>
        protected void Link<T>(IObservable<T> observable, Expression<Func<T>> getSetter)
        {
            if (!(getSetter.Body is MemberExpression me))
                throw new Exception();
            
            if ((me.Member.MemberType & MemberTypes.Property) == 0)
                throw new Exception();

            var propertyName = me.Member.Name;
            var property = GetType().GetProperty(propertyName);
            
            if (property == null || !property.CanWrite)
                throw new Exception();

            var setter = property.GetSetMethod(true);
            
            AutoDispose(observable.SubscribeAction(next =>
                {
                    setter!.Invoke(this, new object?[] {next});
                    RaisePropertyChanged(propertyName);
                }));
        }

        protected void On<T, R>(T obj, Expression<Func<T, R>> property, Action<R> action) where T : INotifyPropertyChanged
        {
            AutoDispose(obj.ToObservable(property).SubscribeAction(action));
        }
        
        protected void On<T>(Expression<Func<T>> property, Action<T> action)
        {
            AutoDispose(this.ToObservable(property).SubscribeAction(action));
        }
        
        /// <summary>
        /// Adds a disposable to the list to dispose on whole class dispose
        /// </summary>
        protected T AutoDispose<T>(T disposable) where T : System.IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }
        
        public void Dispose()
        {
            foreach (var d in disposables)
                d.Dispose();
            disposables.Clear();
        }
    }
}