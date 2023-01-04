using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

namespace CrashReport;

public class AvaloniaViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param == null)
            return new TextBlock(){Text = "Null model"};
     
        var typeName = param.GetType().FullName!;
        var viewName = typeName.Replace("ViewModel", "View");
        var viewType = Type.GetType(viewName);
        if (viewType != null)
        {
            var control = Activator.CreateInstance(viewType) as Control;
            control!.DataContext = param;
            return control;
        }
        return new TextBlock(){Text = "Cannot resolve view for " + param.GetType()};
    }

    public bool Match(object? data)
    {
        return data is not Control && data != null && data is not string;
    }
}