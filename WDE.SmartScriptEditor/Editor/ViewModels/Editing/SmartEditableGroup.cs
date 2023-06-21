using System.Collections.Generic;
using System.Linq;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing;

public class SmartEditableGroup : System.IDisposable
{
    private readonly IBulkEditSource? bulkEditSource;
    private List<(MultiParameterValueHolder<long>, string)> parameters = new();
    private List<(MultiParameterValueHolder<float>, string)> floatParameters = new();
    private List<(MultiParameterValueHolder<string>, string)> stringParameters = new();
    private List<EditableActionData> actions = new();

    public IReadOnlyList<(MultiParameterValueHolder<long> parameter, string name)> Parameters => parameters;
    public IReadOnlyList<(MultiParameterValueHolder<float> parameter, string name)> FloatParameters => floatParameters;
    public IReadOnlyList<(MultiParameterValueHolder<string> parameter, string name)> StringParameters => stringParameters;
    public IReadOnlyList<EditableActionData> Actions => actions;

    public SmartEditableGroup(IBulkEditSource? bulkEditSource)
    {
        this.bulkEditSource = bulkEditSource;
    }

    public List<MultiParameterValueHolder<long>> Add(string group, IReadOnlyList<SmartBaseElement> elements, IReadOnlyList<SmartBaseElement> originals)
    {
        List<MultiParameterValueHolder<long>> longParameters = new();
        for (int i = 0; i < elements[0].ParametersCount; ++i)
        {
            var parameterValues = elements.Select(x => x.GetParameter(i)).ToList();
            var originalParameters = originals.Select(x => x.GetParameter(i)).ToList();
            var holder = (CreateValueHolder(elements[0], parameterValues, originalParameters), group);
            parameters.Add(holder);
            longParameters.Add(holder.Item1);
        }
        
        for (int i = 0; i < elements[0].FloatParametersCount; ++i)
        {
            var parameterValues = elements.Select(x => x.GetFloatParameter(i)).ToList();
            var originalParameters = originals.Select(x => x.GetFloatParameter(i)).ToList();
            var holder = (CreateValueHolder(elements[0],parameterValues, originalParameters), group);
            floatParameters.Add(holder);
        }
        
        for (int i = 0; i < elements[0].StringParametersCount; ++i)
        {
            var parameterValues = elements.Select(x => x.GetStringParameter(i)).ToList();
            var originalParameters = originals.Select(x => x.GetStringParameter(i)).ToList();
            var holder = (CreateValueHolder(elements[0], parameterValues, originalParameters), group);
            stringParameters.Add(holder);
        }

        return longParameters;
    }

    private MultiParameterValueHolder<long> CreateValueHolder(SmartBaseElement context, IReadOnlyList<ParameterValueHolder<long>> parameters,
        IReadOnlyList<ParameterValueHolder<long>> originals)
    {
        return new MultiParameterValueHolder<long>(Parameter.Instance, 0, parameters, originals, bulkEditSource, context);
    }

    private MultiParameterValueHolder<string> CreateValueHolder(SmartBaseElement context,IReadOnlyList<ParameterValueHolder<string>> parameters,
        IReadOnlyList<ParameterValueHolder<string>> originals)
    {
        return new MultiParameterValueHolder<string>(StringParameter.Instance, "", parameters, originals, bulkEditSource, context);
    }

    private MultiParameterValueHolder<float> CreateValueHolder(SmartBaseElement context,IReadOnlyList<ParameterValueHolder<float>> parameters,
        IReadOnlyList<ParameterValueHolder<float>> originals)
    {
        return new MultiParameterValueHolder<float>(FloatParameter.Instance, 0, parameters, originals, bulkEditSource, context);
    }

    public void Add<R>(string group, 
        IEnumerable<R> elements, 
        IEnumerable<R> original, 
        System.Func<R, ParameterValueHolder<long>> extractor,
        SmartBaseElement context)
    {
        parameters.Add((CreateValueHolder(context, elements.Select(extractor).ToList(), original.Select(extractor).ToList()), group));
    }
    
    public void Add<R>(string group, 
        IEnumerable<R> elements, 
        IEnumerable<R> original, 
        System.Func<R, ParameterValueHolder<string>> extractor,
        SmartBaseElement context)
    {
        stringParameters.Add((CreateValueHolder(context, elements.Select(extractor).ToList(), original.Select(extractor).ToList()), group));
    }

    public void Apply()
    {
        foreach (var (parameter, _) in parameters)
            parameter.ApplyToOriginals();
        foreach (var (parameter, _) in floatParameters)
            parameter.ApplyToOriginals();
        foreach (var (parameter, _) in stringParameters)
            parameter.ApplyToOriginals();
    }

    public void Dispose()
    {
        foreach (var (parameter, _) in parameters)
            parameter.Dispose();
        foreach (var (parameter, _) in floatParameters)
            parameter.Dispose();
        foreach (var (parameter, _) in stringParameters)
            parameter.Dispose();
    }

    public EditableActionData Add(EditableActionData action)
    {
        actions.Add(action);
        return action;
    }
}