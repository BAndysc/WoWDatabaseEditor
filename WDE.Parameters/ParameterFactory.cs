using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Prism.Events;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.Parameters
{
    [AutoRegister]
    [SingleInstance]
    public class ParameterFactory : IParameterFactory
    {
        private readonly Dictionary<string, ParameterSpecModel> data = new();
        private readonly Dictionary<string, IParameter<long>> parameters = new();
        private readonly Dictionary<string, IParameter<string>> stringParameters = new();
        
        public IParameter<long> Factory(string type)
        {
            if (parameters.TryGetValue(type, out var parameter))
                return parameter;
            return Parameter.Instance;
        }

        public IParameter<string> FactoryString(string type)
        {
            if (stringParameters.TryGetValue(type, out var parameter))
                return parameter;
            return StringParameter.Instance;
        }

        public bool IsRegisteredLong(string type)
        {
            return parameters.ContainsKey(type);
        }

        public bool IsRegisteredString(string type)
        {
            return stringParameters.ContainsKey(type);
        }

        public void Register(string key, IParameter<long> parameter)
        {
            parameters.Add(key, parameter);
            if (pendingObservables.TryGetValue(key, out var pending))
                pending.Publish(parameter);
            if (pendingLongObservables.TryGetValue(key, out var pending2))
                pending2.Publish(parameter);
            registration.OnNext(parameter);
        }

        public void Register(string key, IParameter<string> parameter)
        {
            stringParameters.Add(key, parameter);
            if (pendingObservables.TryGetValue(key, out var pending))
                pending.Publish(parameter);
            if (pendingStringObservables.TryGetValue(key, out var pending2))
                pending2.Publish(parameter);
            registration.OnNext(parameter);
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

        private readonly Subject<IParameter> registration = new();
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
            Func<IParameter<long>, IParameter<long>, IParameter<long>> creator)
        {
            OnRegisterLong(param1).CombineLatest(OnRegisterLong(param2)).Subscribe(pair =>
            {
                Register(name, creator(pair.First, pair.Second));
            });
        }

        public void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<long>> creator)
        {
            OnRegisterLong(dependsOn).Subscribe(item =>
            {
                Register(name, creator(item));
            });
        }
        
        public void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<string>> creator)
        {
            OnRegisterLong(dependsOn).Subscribe(item =>
            {
                Register(name, creator(item));
            });
        }

        public void Updated(IParameter parameter)
        {
            registration.Publish(parameter);
        }
    }
}