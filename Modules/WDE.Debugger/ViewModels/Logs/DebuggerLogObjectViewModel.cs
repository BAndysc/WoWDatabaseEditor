using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json.Linq;
using WDE.Common.Utils;

namespace WDE.Debugger.ViewModels.Logs;

internal class DebuggerLogObjectViewModel : IParentType
{
    public JToken Token { get; }
    public string Key { get; }
    public DebuggerLogTokenType Type { get; }
    public bool CanBeExpanded => Nested.Count > 0 || SimpleValues.Count > 0;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }

    public ObservableCollection<DebuggerLogObjectViewModel> Nested { get; } = new();
    public ObservableCollection<DebuggerLogValueViewModel> SimpleValues { get; } = new();

    public DebuggerLogObjectViewModel(JToken token, string key, DebuggerLogTokenType type)
    {
        Token = token;
        Key = key;
        Type = type;
        Nested.CollectionChanged += (sender, args) => NestedParentsChanged?.Invoke(this, args);
        SimpleValues.CollectionChanged += (sender, args) => ChildrenChanged?.Invoke(this, args);
    }

    private string? collapsedText;
    public string? CollapsedText
    {
        get => collapsedText;
        set
        {
            if (value == collapsedText)
                return;
            collapsedText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CollapsedText)));
        }
    }

    private bool isExpanded;
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (value == isExpanded)
                return;
            isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<IParentType> NestedParents => Nested;
    public IReadOnlyList<IChildType> Children => SimpleValues;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;

    public void Add(INodeType valueNode)
    {
        if (valueNode is DebuggerLogObjectViewModel node)
        {
            node.Parent = this;
            Nested.Add(node);
        }
        else if (valueNode is DebuggerLogValueViewModel simpleNode)
        {
            simpleNode.Parent = this;
            SimpleValues.Add(simpleNode);
        }
    }

    public void UpdateCollapsedText()
    {
        int items = 0;
        StringBuilder sb = new();
        if (Type == DebuggerLogTokenType.Array)
            sb.Append('[');
        else if (Type == DebuggerLogTokenType.Object)
            sb.Append('{');

        bool overflow = false;
        foreach (var node in SimpleValues)
        {
            if (items >= 4)
            {
                overflow = true;
                continue;
            }

            if (items > 0)
                sb.Append(", ");

            if (Type == DebuggerLogTokenType.Object)
                sb.Append(node.Key + ": " + node.Value);
            else if (Type == DebuggerLogTokenType.Array)
                sb.Append(node.Value);
            items++;
        }
        foreach (var node in Nested)
        {
            if (items >= 4)
            {
                overflow = true;
                continue;
            }

            if (items > 0)
                sb.Append(", ");

            string nestedText = node.Type == DebuggerLogTokenType.Array ? "[...]" : "{...}";

            if (Type == DebuggerLogTokenType.Object)
                sb.Append(node.Key + ": " + nestedText);
            else if (Type == DebuggerLogTokenType.Array)
                sb.Append(nestedText);
            items++;
        }

        if (overflow)
            sb.Append("...");

        if (Type == DebuggerLogTokenType.Array)
            sb.Append(']');
        else if (Type == DebuggerLogTokenType.Object)
            sb.Append('}');

        CollapsedText = sb.ToString();
    }
}