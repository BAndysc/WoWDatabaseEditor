using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FuzzySharp;
using FuzzySharp.PreProcess;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.Parameters.Models;

namespace WDE.Parameters.QuickAccess;

[UniqueProvider]
internal interface IQuickAccessRegisteredParameters
{
    void Register(QuickAccessMode mode, string key, string name);
    IEnumerable<(string key, string name)> RegisteredLimited { get; }
    IEnumerable<(string key, string name)> RegisteredAll { get; }
    IEnumerable<(string key, string name)> RegisteredFull { get; }
}

[SingleInstance]
[AutoRegister]
internal class QuickAccessRegisteredParameters : IQuickAccessRegisteredParameters
{
    private List<(string key, string name)> registeredLimited = new();
    private List<(string key, string name)> registeredAll = new();
    private List<(string key, string name)> registeredFull = new();
    
    public void Register(QuickAccessMode mode, string key, string name)
    {
        if (mode == QuickAccessMode.None)
            return;
        
        registeredAll.Add((key, name));
        
        if (mode == QuickAccessMode.Full)
            registeredFull.Add((key, name));
        else
            registeredLimited.Add((key, name));
    }

    public IEnumerable<(string key, string name)> RegisteredLimited => registeredLimited;
    public IEnumerable<(string key, string name)> RegisteredAll => registeredAll;
    public IEnumerable<(string key, string name)> RegisteredFull => registeredFull;
}

[AutoRegister]
public class ParametersSearch : IQuickAccessSearchProvider
{
    private readonly IParameterFactory parameterFactory;
    private readonly IQuickAccessRegisteredParameters parameters;
    private readonly IQuickCommands quickCommands;

    internal ParametersSearch(IParameterFactory parameterFactory, 
        IQuickAccessRegisteredParameters parameters,
        IQuickCommands quickCommands)
    {
        this.parameterFactory = parameterFactory;
        this.parameters = parameters;
        this.quickCommands = quickCommands;
    }
    
    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        text = text.ToLower();

        await Process(parameters.RegisteredFull, text, produce, cancellationToken, (a, b) =>  Fuzz.WeightedRatio(a, b.ToLower(), PreprocessMode.None));
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        await Process(parameters.RegisteredLimited, text, produce, cancellationToken, (a, b) => b.Contains(a, StringComparison.OrdinalIgnoreCase) ? 100 : 0);
    }

    private async Task Process(IEnumerable<(string key, string name)> parameters, string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken, Func<string, string, int> scorer)
    {
        List<(int, long, QuickAccessItem)> results = new();
        foreach (var model in parameters)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var parameter = parameterFactory.Factory(model.key);
            
            if (parameter.Items == null)
                continue;

            var nameScore = scorer(text, model.name.ToLower());
            bool nameMatches = nameScore > 70;

            int total = 0;
            foreach (var item in parameter.Items)
            {
                var fullName = parameter.Prefix + item.Value.Name;

                var itemScore = scorer(text, fullName);
                
                if (nameMatches || itemScore > 70)
                {
                    var quickItem = new QuickAccessItem(new ImageUri("Icons/icon_copy.png"), fullName,  item.Key.ToString(), model.name, quickCommands.CopyCommand, item.Key, (byte)itemScore);
                    results.Add((-Math.Max(nameScore, itemScore), item.Key, quickItem));
                    total++;

                    if (total >= 100)
                    {
                        results.Add((nameScore, nameScore, quickCommands.AndMoreItem));
                        break;
                    }
                }
            }
            
            if (cancellationToken.IsCancellationRequested)
                return;
        }

        results.Sort(Comparer<(int, long, QuickAccessItem)>.Create((a, b) =>
        {
            if (a.Item1 == b.Item1)
                return a.Item2.CompareTo(b.Item2);
            return a.Item1.CompareTo(b.Item1);
        }));
        
        foreach (var r in results)
        {
            produce(r.Item3);
            if (cancellationToken.IsCancellationRequested)
                return;
        }
    }
}