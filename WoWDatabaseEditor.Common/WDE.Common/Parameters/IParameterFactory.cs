using System;
using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Parameters
{
    public enum QuickAccessMode
    {
        None,
        Limited,
        Full
    }
    
    [UniqueProvider]
    public interface IParameterFactory
    {
        IParameter<long> Factory(string? type);
        IParameter<string> FactoryString(string? type);
        IParameter<float> FactoryFloat(string? type);
        bool IsRegisteredLong(string? type);
        bool IsRegisteredString(string? type);
        bool IsRegisteredFloat(string? type);
        T Register<T>(string key, T parameter, QuickAccessMode quickAccessMode = QuickAccessMode.None, bool overrideExisting = false) where T : IParameter<long>;
        void Register(string key, IParameter<string> parameter);
        void Updated(IParameter parameter);

        void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);
        void RegisterDepending(string name, string dependsOn, Func<IParameter<long>, IParameter<string>> creator);
        void RegisterCombined(string name, string param1, string param2, Func<IParameter<long>, IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);
        void RegisterCombined(string name, string param1, string param2, string param3, Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);
        void RegisterCombined(string name, string param1, string param2, string param3, string param4, Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);
        void RegisterCombined(string name, string param1, string param2, string param3, string param4, string param5, Func<IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>, IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);
        void RegisterCombined(string name, string[] dependencies, Func<IParameter<long>[], IParameter<long>> creator, QuickAccessMode quickAccessMode = QuickAccessMode.None);

        IEnumerable<string> GetKeys();

        IObservable<IParameter> OnRegister(string key);
        IObservable<IParameter<long>> OnRegisterLong(string key);
        IObservable<IParameter<string>> OnRegisterString(string key);
        IObservable<IParameter> OnRegister();
        IObservable<string> OnRegisterKey();
    }
}