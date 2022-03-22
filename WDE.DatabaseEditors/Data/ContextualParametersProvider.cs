using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using ObservableExtensions = WDE.MVVM.Observable.ObservableExtensions;

namespace WDE.DatabaseEditors.Data;

[UniqueProvider]
public interface IContextualParametersProvider
{
}

[AutoRegister]
[SingleInstance]
public class ContextualParametersProvider : IContextualParametersProvider 
{
    public ContextualParametersProvider(IContextualParametersJsonProvider jsons,
        IParameterPickerService pickerService,
        IParameterFactory parameterFactory)
    {
        foreach (var json in jsons.GetParameters())
        {
            var deserialized = JsonConvert.DeserializeObject<ContextualParameterJson>(json.content);

            if (deserialized.SimpleSwitch != null)
            {
                var required = deserialized.SimpleSwitch.Values.Values.ToList();
                Debug.Assert(required.Count > 0);

                var observable = parameterFactory.OnRegisterLong(required[0]).Select(_ => 1);
                
                for (var i = 1; i < required.Count; i++)
                {
                    observable = observable.CombineLatest(parameterFactory.OnRegisterLong(required[i]), (a, _) => a + 1);
                }

                ObservableExtensions.SubscribeOnce(observable, val =>
                {
                    Debug.Assert(val == required.Count);
                    parameterFactory.Register(deserialized.Name, new DatabaseContextualParameter(parameterFactory, pickerService, deserialized.SimpleSwitch));
                });
            }
            else
                throw new Exception("Unknown type of parameter");
        }
    }
}

public class DatabaseContextualParameter : IContextualParameter<long, DatabaseEntity>, ICustomPickerContextualParameter<long>
{
    private readonly IParameterPickerService pickerService;
    private Dictionary<long, IParameter<long>> parameters = new();
    private string column;
    private IParameter<long> defaultParameter;

    public DatabaseContextualParameter(IParameterFactory factory, 
        IParameterPickerService pickerService,
        ContextualParameterSimpleSwitchJson simpleSwitch)
    {
        column = simpleSwitch.Column;
        defaultParameter = simpleSwitch.Default == null ? Parameter.Instance : factory.Factory(simpleSwitch.Default);
        this.pickerService = pickerService;
        foreach (var param in simpleSwitch.Values)
        {
            if (!factory.IsRegisteredLong(param.Value))
                throw new Exception("Unknown parameter " + param.Value + " but expected to be known!");
            var p =  factory.Factory(param.Value);
            parameters[param.Key] = p;
        }
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        var parameter = defaultParameter;
        if (context is DatabaseEntity entity)
        {
            var cell = entity.GetTypedValueOrThrow<long>(column);
            if (parameters.TryGetValue(cell, out var _parameter))
                parameter = _parameter;
        }

        return pickerService.PickParameter(parameter, value);
    }

    public string? Prefix { get; set; }
    public bool HasItems => true;
    public string ToString(long value) => value.ToString();

    public Dictionary<long, SelectOption>? Items { get; set; }
    
    public string ToString(long value, DatabaseEntity entity)
    {
        var parameter = defaultParameter;
        var cell = entity.GetTypedValueOrThrow<long>(column);
        if (parameters.TryGetValue(cell, out var _parameter))
            parameter = _parameter;

        return parameter.ToString(value);
    }
}