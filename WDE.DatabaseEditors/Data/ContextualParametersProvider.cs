using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Modules;
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
public class ContextualParametersProvider : IContextualParametersProvider, IGlobalAsyncInitializer
{
    private readonly IContextualParametersJsonProvider jsons;
    private readonly IParameterPickerService pickerService;
    private readonly IParameterFactory parameterFactory;

    public ContextualParametersProvider(IContextualParametersJsonProvider jsons,
        IParameterPickerService pickerService,
        IParameterFactory parameterFactory)
    {
        this.jsons = jsons;
        this.pickerService = pickerService;
        this.parameterFactory = parameterFactory;
    }

    public async Task Initialize()
    {
        foreach (var json in await jsons.GetParameters())
        {
            var deserialized = JsonConvert.DeserializeObject<ContextualParameterJson>(json.content);

            if (deserialized == null)
            {
                LOG.LogError("Cannot deserialize " + json.file);
                continue;
            }

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
                    IParameter<long> parameter;

                    var anyIsAsync = deserialized.SimpleSwitch.Values.Values.Select(parameterFactory.Factory)
                        .Any(param => param is IAsyncParameter<long> || param is IAsyncContextualParameter<long, DatabaseEntity>);

                    if (anyIsAsync)
                        parameter = new AsyncDatabaseContextualParameter(parameterFactory, pickerService, deserialized.SimpleSwitch);
                    else
                        parameter = new DatabaseContextualParameter(parameterFactory, pickerService, deserialized.SimpleSwitch);

                    parameterFactory.Register(deserialized.Name, parameter);
                });
            }
            else if (deserialized.SimpleStringSwitch != null)
            {
                var required = deserialized.SimpleStringSwitch.Values.Values.ToList();
                Debug.Assert(required.Count > 0);

                var observable = parameterFactory.OnRegister(required[0]).Select(_ => 1);

                for (var i = 1; i < required.Count; i++)
                {
                    observable = observable.CombineLatest(parameterFactory.OnRegister(required[i]), (a, _) => a + 1);
                }

                ObservableExtensions.SubscribeOnce(observable, val =>
                {
                    Debug.Assert(val == required.Count);
                    parameterFactory.Register(deserialized.Name, new DatabaseStringContextualParameter(parameterFactory, pickerService, deserialized.SimpleStringSwitch));
                });
            }
            else
                throw new Exception("Unknown type of parameter");
        }
    }
}

public class DatabaseContextualParameter : BaseContextualParameter<long, DatabaseEntity>, ICustomPickerContextualParameter<long>, ITableAffectedByParameter
{
    protected readonly IParameterPickerService pickerService;
    protected Dictionary<long, IParameter<long>> parameters = new();
    protected Dictionary<IParameter<long>, List<long>> reverseParameters = new();
    protected ColumnFullName column;
    protected IParameter<long> defaultParameter;

    public ColumnFullName DependantColumn => column;

    public IReadOnlyList<long>? DependantColumnValuesForParameter(IParameter<long> param)
    {
        if (reverseParameters.TryGetValue(param, out var list))
            return list;
        return null;
    }

    public DatabaseContextualParameter(IParameterFactory factory, 
        IParameterPickerService pickerService,
        ContextualParameterSimpleSwitchJson simpleSwitch)
    {
        column = new ColumnFullName(null, simpleSwitch.Column);
        defaultParameter = simpleSwitch.Default == null ? Parameter.Instance : factory.Factory(simpleSwitch.Default);
        this.pickerService = pickerService;
        foreach (var param in simpleSwitch.Values)
        {
            if (!factory.IsRegisteredLong(param.Value))
                throw new Exception("Unknown parameter " + param.Value + " but expected to be known!");
            var p =  factory.Factory(param.Value);
            parameters[param.Key] = p;
            if (!reverseParameters.TryGetValue(p, out var list))
                list = reverseParameters[p] = new List<long>();
            list.Add(param.Key);
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

    public override string? Prefix { get; }
    public override bool HasItems => true;
    public override string ToString(long value) => value.ToString();

    public override Dictionary<long, SelectOption>? Items => null;
    
    public override string ToString(long value, DatabaseEntity entity)
    {
        var parameter = defaultParameter;
        var cell = entity.GetTypedValueOrThrow<long>(column);
        if (parameters.TryGetValue(cell, out var _parameter))
            parameter = _parameter;

        return parameter.ToString(value);
    }

    public ColumnFullName AffectedByColumn => column;
}

public class AsyncDatabaseContextualParameter : DatabaseContextualParameter, IAsyncContextualParameter<long, DatabaseEntity>
{
    public AsyncDatabaseContextualParameter(IParameterFactory factory, IParameterPickerService pickerService, ContextualParameterSimpleSwitchJson simpleSwitch) : base(factory, pickerService, simpleSwitch)
    {
    }
    
    public async Task<string> ToStringAsync(long value, CancellationToken token, DatabaseEntity entity)
    {
        var parameter = defaultParameter;
        var cell = entity.GetTypedValueOrThrow<long>(column);
        if (parameters.TryGetValue(cell, out var _parameter))
            parameter = _parameter;

        if (parameter is IAsyncContextualParameter<long, DatabaseEntity> asyncContextualParameter)
            return await asyncContextualParameter.ToStringAsync(value, token, entity);

        if (parameter is IAsyncParameter<long> asyncParameter)
            return await asyncParameter.ToStringAsync(value, token);
        
        return parameter.ToString(value);
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, object context)
    {
        if (context is DatabaseEntity entity)
            return await ToStringAsync(value, token, entity);
        return ToString(value);
    }
}

public interface ITableAffectedByParameter
{
    public ColumnFullName AffectedByColumn { get; }
    public bool AffectedByConditions => false;
}

public class DatabaseStringContextualParameter : BaseContextualParameter<string, DatabaseEntity>, ICustomPickerContextualParameter<string>, ITableAffectedByParameter
{
    private readonly IParameterPickerService pickerService;
    private Dictionary<string, IParameter<long>> longParameters = new();
    private Dictionary<string, IParameter<string>> stringParameters = new();
    private IParameter<long> defaultParameter;
    public ColumnFullName AffectedByColumn { get; }

    public DatabaseStringContextualParameter(IParameterFactory factory, 
        IParameterPickerService pickerService,
        ContextualParameterSimpleStringSwitchJson simpleSwitch)
    {
        AffectedByColumn = new ColumnFullName(null, simpleSwitch.Column);
        defaultParameter = simpleSwitch.Default == null ? Parameter.Instance : factory.Factory(simpleSwitch.Default);
        this.pickerService = pickerService;
        foreach (var param in simpleSwitch.Values)
        {
            if (factory.IsRegisteredLong(param.Value))
                longParameters[param.Key] = factory.Factory(param.Value);
            else if (factory.IsRegisteredString(param.Value))
                stringParameters[param.Key] = factory.FactoryString(param.Value);
            else
                throw new Exception("Unknown parameter " + param.Value + " but expected to be known!");
        }
    }

    public async Task<(string, bool)> PickValue(string value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var cell = entity.GetTypedValueOrThrow<string>(AffectedByColumn);
            if (cell == null)
                return ("", false);
            if (longParameters.TryGetValue(cell, out var parameter))
            {
                if (!long.TryParse(value, out var v))
                    v = 0;
                var result = await pickerService.PickParameter(parameter, v);
                return (result.value.ToString(), result.ok);
            }
            else if (stringParameters.TryGetValue(cell, out var sParameter))
            {
                var result = await pickerService.PickParameter(sParameter, value);
                return (result.value ?? "", result.ok);
            }
        }

        var res = await pickerService.PickParameter(StringParameter.Instance, value);
        return (res.value ?? "", res.ok);
    }

    public override string? Prefix { get; }
    public override bool HasItems => true;
    public string ToString(long value) => value.ToString();

    public override string ToString(string value) => value;

    public override Dictionary<string, SelectOption>? Items => null;
    
    public override string ToString(string value, DatabaseEntity entity)
    {
        var cell = entity.GetTypedValueOrThrow<string>(AffectedByColumn);
        if (cell == null)
            return value + " (invalid " + AffectedByColumn + " value)";
        if (longParameters.TryGetValue(cell, out var parameter))
        {
            if (long.TryParse(value, out var v))
                return parameter.ToString(v);
            return value + " (invalid: expected a number)";
        }
        else if (stringParameters.TryGetValue(cell, out var sParameter)) 
            return sParameter.ToString(value);

        return value;
    }
}