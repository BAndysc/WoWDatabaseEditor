using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaloniaStyles.Controls;

public class RecyclableViewList
{
    private readonly Panel owner;
    private readonly bool behind;
    private List<IControl> controls = new();
    private int counter = 0;
    private IDataTemplate? template;

    public RecyclableViewList(Panel owner, bool behind = false)
    {
        this.owner = owner;
        this.behind = behind;
    }
    
    public void Reset(IDataTemplate template)
    {
        this.template = template;
        counter = 0;
    }

    public IControl GetNext(object context)
    {
        IControl control;
        if (counter >= controls.Count)
        {
            control = template!.Build(null!);
            controls.Add(control);
            control.DataContext = context;
            if (behind)
                owner.Children.Insert(0, control);
            else
                owner.Children.Add(control);
            counter++;
            return control;
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
                return found;
            }
            else if (controls[i].DataContext == null)
                break;
        }
        
        
        control = controls[counter++];
        control.DataContext = context;
        control.InvalidateMeasure();
        control.InvalidateArrange();
        return control;
    }

    public void Finish()
    {
        for (int i = counter; i < controls.Count; ++i)
        {
            controls[i].Arrange(new Rect(0, 0, 1, 1));
            controls[i].DataContext = null;
        }

        template = null;
    }
}