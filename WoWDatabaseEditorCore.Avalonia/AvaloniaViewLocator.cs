using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using WDE.Common.Avalonia.Utils;

namespace WoWDatabaseEditorCore.Avalonia;

public class AvaloniaViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param == null)
            return new TextBlock(){Text = "Null model"};
     
        if (ViewBind.TryResolve(param, out var view))
        {
            var control = view as Control;
            control!.DataContext = param;
            return control;
        }
        return new TextBlock(){Text = "Cannot resolve view for " + param.GetType()};
    }

    public bool Match(object? data)
    {
        return data != null && ViewBind.CanResolve(data);
    }
}