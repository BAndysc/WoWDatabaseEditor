using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.Parameters.Parameters;
using WDE.Parameters.QuickAccess;

namespace WDE.Parameters
{
    [AutoRegister]
    [SingleInstance]
    public class ParameterFactory : IParameterFactory
    {
        private readonly IQuickAccessRegisteredParameters quickAccessRegisteredParameters;
        private readonly Lazy<ITableEditorPickerService> tableEditorPickerService;
        private readonly Dictionary<string, ParameterSpecModel> data;
        private readonly Dictionary<string, IParameter<long>> parameters;
        private readonly Dictionary<string, IParameter<string>> stringParameters;

        internal ParameterFactory(IQuickAccessRegisteredParameters quickAccessRegisteredParameters,
            Lazy<ITableEditorPickerService> tableEditorPickerService)
        {
            this.quickAccessRegisteredParameters = quickAccessRegisteredParameters;
            this.tableEditorPickerService = tableEditorPickerService;
            data = new(StringComparer.OrdinalIgnoreCase);
            parameters = new(StringComparer.OrdinalIgnoreCase);
            stringParameters = new(StringComparer.OrdinalIgnoreCase);
            Register("Parameter", new Parameter());
        }

        public IParameter<long> Factory(string? type)
        {
            if (type == null)
                return Parameter.Instance;
            
            if (parameters.TryGetValue(type, out var parameter))
                return parameter;

            if (type.StartsWith("TableReference("))
            {
                var indexOf = type.IndexOf("#", StringComparison.Ordinal);
                var indexOfEnd = type.IndexOf(")", indexOf, StringComparison.Ordinal);
                var referenceDatabaseAndTable = type.Substring("TableReference(".Length, indexOf - "TableReference(".Length);
                var referenceTable = DatabaseTable.Parse(referenceDatabaseAndTable);
                var referenceColumn = type.Substring(indexOf + 1, indexOfEnd - indexOf - 1);
                parameter = new ForeignReferenceParameter(tableEditorPickerService, referenceTable, referenceColumn);
                Register(type, parameter);
                return parameter;
            }
            
            return Parameter.Instance;
        }

        public IParameter<string> FactoryString(string? type)
        {
            if (type == null)
                return StringParameter.Instance;
            
            if (stringParameters.TryGetValue(type, out var parameter))
                return parameter;
            
            return StringParameter.Instance;
        }

        public IParameter<float> FactoryFloat(string? type)
        {
            return new FloatParameter();
        }

        public bool IsRegisteredLong(string? type)
        {
            if (type == null)
                return false;
            
            if (type.StartsWith("TableReference("))
                Factory(type);
            
            return parameters.ContainsKey(type);
        }

        public bool IsRegisteredString(string? type)
        {
            if (type == null)
                return false;

            return stringParameters.ContainsKey(type);
        }

        public bool IsRegisteredFloat(string? type)
        {
            return type == "FloatParameterFloat";
        }

        public T Register<T>(string key, T parameter, QuickAccessMode quickAccessMode = QuickAccessMode.None, bool overrideExisting = false) where T : IParameter<long>
        {
            if (overrideExisting)
                parameters[key] = parameter;
            else
                parameters.Add(key, parameter);

            if (pendingObservables.TryGetValue(key, out var pending))
                pending.Publish(parameter);
            if (pendingLongObservables.TryGetValue(key, out var pending2))
                pending2.Publish(parameter);
            registration.OnNext(parameter);
            quickAccessRegisteredParameters.Register(quickAccessMode, key, key.Replace("Parameter", "").ToTitleCase());
            keyRegistration.OnNext(key);
            return parameter;
        }

        public void Register(string key, IParameter<string> parameter)
        {
            stringParameters.Add(key, parameter);
            if (pendingObservables.TryGetValue(key, out var pending))
                pending.Publish(parameter);
            if (pendingStringObservables.TryGetValue(key, out var pending2))
                pending2.Publish(parameter);
            registration.OnNext(parameter);
            keyRegistration.OnNext(key);
        }

        public IEnumerable<string> GetKeys() => data.Keys.Union(parameters.Keys);

        public IObservable<IParameter> OnRegister(string key)
        {
            if (IsRegisteredLong(key))
                return new SingleValuePublisher<IParameter>(Factory(key));
            
            if (IsRegisteredString(key))
                return new SingleValuePublisher<IParameter>(FactoryString(key));

            if (pendingObservables.TryGetValue(key, out var observable))
                return observable;

            return pendingObservables[key] = new();
        }
        
        public IObservable<IParameter<long>> OnRegisterLong(string key)
        {
            if (IsRegisteredLong(key))
                return new SingleValuePublisher<IParameter<long>>(Factory(key));

            if (pendingLongObservables.TryGetValue(key, out var observable))
                return observable;

            return pendingLongObservables[key] = new();
        }
        
        public IObservable<IParameter<string>> OnRegisterString(string key)
        {
            if (IsRegisteredString(key))
                return new SingleValuePublisher<IParameter<string>>(FactoryString(key));

            if (pendingStringObservables.TryGetValue(key, out var observable))
                return observable;

            return pendingStringObservables[key] = new();
        }

        public IObservable<IParameter> OnRegister() => registration;
        public IObservable<string> OnRegisterKey() => keyRegistration;

        private readonly Subject<IParameter> registration = new();
        private readonly Subject<string> keyRegistration = new();
        private readonly Dictionary<string, OnDemandSingleValuePublisher<IParameter>> pendingObservables = new();
        private readonly Dictionary<string, OnDemandSingleValuePublisher<IParameter<long>>> pendingLongObservables = new();
        private readonly Dictionary<string, OnDemandSingleValuePublisher<IParameter<string>>> pendingStringObservables = new();

        public ParameterSpecModel GetDefinition(string key)
        {
            if (parameters.TryGetValue(key, out var param))
            {
                return new ParameterSpecModel
                {
                    IsFlag = param is FlagParameter,
                    Key = key,
                    Name = key,
                    Values = param.Items
                };
            }

            return data[key];
        }
        
        ////
        public void RegisterCombined(string name, string param1, string param2,
            Func<IParameter<long>, IParameter<long>, IParameter<long>> creator,
            QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            OnRegisterLong(param1).CombineLatest(OnRegisterLong(param2)).SubscribeOnce(pair =>
            {
                Register(name, creator(pair.First, pair.Second), quickAccessMode);
            });
        }

        public void RegisterCombined(string name, string param1, string param2, string param3,
            Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator,
            QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            OnRegisterLong(param1)
                .CombineLatest(OnRegisterLong(param2), OnRegisterLong(param3))
                .SubscribeOnce(pair =>
                {
                    Register(name, creator(pair.First, pair.Second, pair.Third), quickAccessMode);
                });
        }
        
        public void RegisterCombined(string name, string param1, string param2, string param3, string param4,
            Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator,
            QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            OnRegisterLong(param1)
                .CombineLatest(OnRegisterLong(param2), OnRegisterLong(param3), OnRegisterLong(param4))
                .SubscribeOnce(pair =>
            {
                Register(name, creator(pair.First, pair.Second, pair.Third, pair.Fourth), quickAccessMode);
            });
        }
        
        public void RegisterCombined(string name, string param1, string param2, string param3, string param4, string param5,
            Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator,
            QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            OnRegisterLong(param1)
                .CombineLatest(OnRegisterLong(param2), OnRegisterLong(param3), OnRegisterLong(param4), OnRegisterLong(param5))
                .SubscribeOnce(pair =>
                {
                    Register(name, creator(pair.First, pair.Second, pair.Third, pair.Fourth, pair.Fifth), quickAccessMode);
                });
        }

        public void RegisterCombined(string name, string[] dependencies, Func<IParameter<long>[], IParameter<long>> creator,
            QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            new CombineLatest<IParameter<long>>(
                    dependencies.Select(OnRegisterLong).ToArray())
                .SubscribeOnce(prams => Register(name, creator(prams), quickAccessMode));
        }

        public class CombineLatest<T> : IObservable<T[]>
        {
            private readonly IObservable<T>[] sources;

            public CombineLatest(IObservable<T>[] sources)
            {
                this.sources = sources;
            }

            private class Sub : IDisposable
            {
                private readonly IObserver<T[]> observer;
                private readonly T[] values;
                private BitArray hasValue;
                private List<System.IDisposable> subs;

                public Sub(IObservable<T>[] sources, IObserver<T[]> observer)
                {
                    this.observer = observer;
                    values = new T[sources.Length];
                    hasValue = new BitArray(sources.Length);
                    subs = sources.Select((s, index) => s.SubscribeAction(x =>
                    {
                        values[index] = x;
                        if (hasValue[index])
                            return;
                        hasValue[index] = true;
                        if (hasValue.HasAllSet())
                            this.observer.OnNext(values);
                    })).ToList();
                }

                public void Dispose()
                {
                    foreach (var x in subs)
                    {
                        x.Dispose();
                    }
                    subs.Clear();
                }
            }

            public IDisposable Subscribe(IObserver<T[]> observer)
            {
                return new Sub(sources, observer);
            }
        }

        public void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None)
        {
            OnRegisterLong(dependsOn).SubscribeOnce(item =>
            {
                Register(name, creator(item), quickAccessMode);
            });
        }
        
        public void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<string>> creator)
        {
            OnRegisterLong(dependsOn).SubscribeOnce(item =>
            {
                Register(name, creator(item));
            });
        }

        public void Updated(IParameter parameter)
        {
            registration.OnNext(parameter);
        }
    }
}