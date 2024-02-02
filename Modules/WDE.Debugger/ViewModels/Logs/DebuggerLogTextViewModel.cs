using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json.Linq;
using WDE.Common.Utils;

namespace WDE.Debugger.ViewModels.Logs;

internal class DebuggerLogTextViewModel : IParentType
{
    public DebuggerLogTextViewModel(JValue token)
    {
        Token = token;
        Text = token.Type == JTokenType.Null ? "(null)" : token.ToString(CultureInfo.InvariantCulture);
    }

    public bool CanBeExpanded => false;
    public JToken Token { get; }
    public string Text { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsExpanded { get; set; }
    public IReadOnlyList<IParentType> NestedParents => Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => Array.Empty<IChildType>();
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}