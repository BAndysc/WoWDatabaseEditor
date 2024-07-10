using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using WDE.Common.Disposables;
using WDE.MVVM.Observable;

namespace WDE.MVVM
{
    public abstract class ObservableBase : BindableBase, IDisposable, INotifyPropertyValueChanged
    {
        private List<IDisposable>? disposables;
        private List<CancellationTokenSource>? tasks;

        private ILogger? logger;
        protected ILogger LOG => logger ??= WDE.Common.LOG.Factory.CreateLogger(GetType());

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

            Link<R>(obj.ToObservable(getter), getSetter);
        }
        
        /// <summary>
        /// Subscribes into `this` property extracted via `source` (using INotifyPropertyChanged)
        /// every time the property is updated, OnPropertyChanged will be called on a property from getter
        ///
        /// AutoDisposes when this class is disposed 
        /// </summary>
        /// <param name="source">Property getter of this. Must be always only single property getter p => p.PropertyMember</param>
        /// <param name="getter">Accessor to property in this class to be RaisedNotify. Must be always only a single property getter p => p.PropertyMember. This property have to have a getter</param>
        /// <typeparam name="T">Type of property we will watch</typeparam>
        /// <typeparam name="R">Type of property we OnPropertyChanged</typeparam>
        protected void Watch<T, R>(Expression<Func<T>> source, Expression<Func<R>> getter)
        {
            Watch(this.ToObservable(source), getter);
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
                throw new Exception("getSetter must be a single getter p => p.PropertyMember");
            
            if ((me.Member.MemberType & MemberTypes.Property) == 0)
                throw new Exception("getSetter must be a single property getter p => p.PropertyMember");

            var propertyName = me.Member.Name;
            var property = GetType().GetProperty(propertyName);
            
            if (property == null || !property.CanWrite)
                throw new Exception($"Property {propertyName} not found or not writeable");

            var setter = property.GetSetMethod(true);
            
            AutoDispose(observable.SubscribeAction(next =>
                {
                    setter!.Invoke(this, new object?[] {next});
                    RaisePropertyChanged(propertyName);
                }));
        }

        /// <summary>
        /// Subscribes into given observable, each time observable produces a value,
        /// OnPropertyChanged is fired for given property in getter
        /// </summary>
        /// <param name="observable">Observable to subscribe to</param>
        /// <param name="getter">Accessor to property in this class to be updated. Must be always only a single property getter p => p.PropertyMember. This property have to have a getter</param>
        /// <exception cref="Exception">Throws an exception when getSetter is in wrong form</exception>
        protected void Watch<T, R>(IObservable<T> observable, Expression<Func<R>> getter)
        {
            if (!(getter.Body is MemberExpression me))
                throw new Exception();
            
            if ((me.Member.MemberType & MemberTypes.Property) == 0)
                throw new Exception();

            var propertyName = me.Member.Name;
            var property = GetType().GetProperty(propertyName);
            
            if (property == null || !property.CanRead)
                throw new Exception();
            
            AutoDispose(observable.SubscribeAction(next =>
            {
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
        
        protected bool SetPropertyWithOldValue<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            var old = storage;
            storage = value;
            RaisePropertyChanged(propertyName!);
            RaisePropertyValueChanged(propertyName!, old, value);

            return true;
        }

        protected void RaisePropertyValueChanged<T>(string propertyName, T old, T @new)
        {
            PropertyValueChanged?.Invoke(this, new TypedPropertyValueChangedEventArgs<T>(propertyName, old, @new));
        }

        /// <summary>
        /// Adds a disposable to the list to dispose on whole class dispose
        /// </summary>
        public T AutoDispose<T>(T disposable) where T : System.IDisposable
        {
            disposables ??= new List<IDisposable>();
            disposables.Add(disposable);
            return disposable;
        }

        public void AutoDispose(Action dispose)
        {
            AutoDispose(new ActionDisposable(dispose));
        }

        private bool disposed = false;

        protected bool IsDisposed => disposed;
        
        /**
         * for ViewModels that are allowed to dispose in the middle of their lifetime, use
         * DisposePartial instead of Dispose, because Dispose is assumed to be called only once
         */
        protected void DisposePartial()
        {
            if (disposables != null)
            {
                while (disposables.Count > 0)
                {
                    disposables[^1].Dispose();
                    disposables.RemoveAt(disposables.Count - 1);
                }
            }
        }

        protected async Task<T> ExecuteTask<T>(Func<CancellationToken, Task<T>> task)
        {
            tasks ??= new List<CancellationTokenSource>();
            var cts = new CancellationTokenSource();
            tasks.Add(cts);
            var taskToExecute = task(cts.Token);
            try
            {
                var result = await taskToExecute;
                return result;
            }
            finally
            {
                tasks.Remove(cts);
            }
        }
        
        public virtual void Dispose()
        {
            if (disposed)
                return;
            
            disposed = true;

            if (tasks != null)
            {
                while (tasks.Count > 0)
                {
                    var token = tasks[^1];
                    tasks.RemoveAt(tasks.Count - 1);
                    token.Cancel();
                }
            }

            if (disposables != null)
            {
                while (disposables.Count > 0)
                {
                    disposables[^1].Dispose();
                    disposables.RemoveAt(disposables.Count - 1);
                }
            }
        }

        public event PropertyValueChangedEventHandler? PropertyValueChanged;
    }
}