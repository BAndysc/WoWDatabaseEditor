using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Projektanker.Icons.Avalonia;

namespace AvaloniaStyles.Controls;

public class RecyclableViewList
{
    private readonly Panel owner;
    private readonly bool behind;
    private List<Control> controls = new();
    private int counter = 0;
    private IDataTemplate? template;

    public RecyclableViewList(Panel owner, bool behind = false)
    {
        this.owner = owner;
        this.behind = behind;
    }
    
    public void Reset(IDataTemplate? template)
    {
        this.template = template;
        counter = 0;
    }

    public bool TryGetNext(object context, out Control control)
    {
        control = null!;
        if (template == null)
            return false;
        if (counter >= controls.Count)
        {
            control = template!.Build(null!)!;
            controls.Add(control);
            control.DataContext = context;
            if (behind)
                owner.Children.Insert(0, control);
            else
                owner.Children.Add(control);
            counter++;
            return true;
        }
        // find a remaining control that has context data context
        for (int i = counter; i < controls.Count; ++i)
        {
            if (ReferenceEquals(controls[i].DataContext, context))
            {
                var found = controls[i];
                var atCounter = controls[counter];

                controls[counter] = found;
                controls[i] = atCounter;

                ++counter;
                control = found;
                return true;
            }
            else if (controls[i].DataContext == null)
                break;
        }
        
        control = controls[counter++];
        control.DataContext = null;
        control.DataContext = context;
        control.IsVisible = true;
        control.InvalidateMeasure();
        control.InvalidateArrange();
        return true;
    }
    
    public Control GetNext(object context)
    {
        if (!TryGetNext(context, out var control))
            throw new Exception("Template is null!");

        return control!;
    }

    public void Finish()
    {
        for (int i = counter; i < controls.Count; ++i)
        {
            controls[i].IsVisible = false;
            controls[i].DataContext = null;
        }

        template = null;
    }
}
