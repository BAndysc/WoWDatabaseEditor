using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using JetBrains.Profiler.Api;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public partial class VirtualizedSmartScriptPanel
{
    public const double AddActionHeight = 25;
    public const double AddConditionHeight = 25;
    
    private MeasureUtil measureEvent;
    private MeasureUtil measureCondition;
    private MeasureUtil measureGroup;
    private MeasureUtil measureGlobalVariable;
    
    private Size MeasureVariableImpl(GlobalVariable e, double maxWidth) => measureGlobalVariable.Measure(e, maxWidth, () => VariableItemTemplate);
    private Size MeasureEvent(SmartEvent e, double maxWidth) => measureEvent.Measure(e, maxWidth, () => EventItemTemplate);
    private Size MeasureCondition(SmartCondition e, double maxWidth) => measureCondition.Measure(e, maxWidth, () => ConditionItemTemplate);
    private Size MeasureGroupImpl(SmartGroup e, double maxWidth) => measureGroup.Measure(e, maxWidth, () => GroupItemTemplate);

    private void MeasureGroup(in SizingContext context, SmartGroup g, out double height)
    {
        if (g.CachedHeight.HasValue)
        {
            height = g.CachedHeight.Value;
            return;
        }

        var size = MeasureGroupImpl(g, context.totalWidth);
        g.CachedHeight = height = size.Height;
    }

    private void MeasureVariable(in SizingContext context, GlobalVariable variable, out double height)
    {
        if (variable.CachedHeight.HasValue)
        {
            height = variable.CachedHeight.Value;
            return;
        }

        var size = MeasureVariableImpl(variable, context.totalWidth);
        variable.CachedHeight = height = size.Height;
    }

    private Size MeasureAction(SmartAction action, double maxWidth)
    {
        const float Padding = 2 + 1; // padding + border
        var size = FormattedTextBlock.MeasureText((float)maxWidth, action.Id == SmartConstants.ActionComment ? action.Comment : action.Readable, default, out _);
        // this mimics SmartActionView measurement from Generic.axaml, for performance reasons
        return size.Inflate(new Thickness(Padding + action.Indent * 24, Padding, Padding, Padding));
    }
    
    private void MeasureEventWithActionsAndConditions(in SizingContext context, SmartEvent e, bool inGroup, out double totalHeight)
    {
        if (!e.CachedHeight.HasValue)
        {
            var size = MeasureEvent(e, (inGroup ? context.groupedEventRect : context.eventRect).Width);
            e.CachedHeight = size.Height;    
        }

        bool useCompactView = compactView && e != script!.Events[^1];
        
        double actionsHeight = useCompactView ? 0 : AddActionHeight;
        foreach (var action in e.Actions)
        {
            if (action.Id == SmartConstants.ActionComment && HideComments)
                continue;
            if (!action.CachedHeight.HasValue)
                action.CachedHeight = MeasureAction(action, context.actionRect.Width).Height;
            actionsHeight += action.CachedHeight.Value + ActionSpacing;
        }

        double conditionsHeight = useCompactView ? 0 : AddConditionHeight;
        if (hideConditions)
        {
            if (e.Conditions.Count > 0)
                conditionsHeight += AddConditionHeight;
        }
        else
        {
            foreach (var condition in e.Conditions)
            {
                if (!condition.CachedHeight.HasValue)
                    condition.CachedHeight = MeasureCondition(condition, (inGroup ? context.grupedConditionRect : context.conditionRect).Width).Height;
                conditionsHeight += condition.CachedHeight.Value + ConditionSpacing;
            }   
        }

        totalHeight = Math.Max(actionsHeight, e.CachedHeight.Value + conditionsHeight);
    }
    
    private double lastWidth = 0;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (script == null)
            return default;
        
        SizingContext context = new SizingContext(availableSize.Width, Padding, EventPaddingLeft, VisibleRect);
        
        double totalHeight = Padding.Top + Padding.Bottom;

        // invalidate cache when width changes
        if (Math.Abs(lastWidth - availableSize.Width) > 0.1f)
        {
            script.Events.Each(x => x.CachedHeight = null);
            script.Events.SelectMany(e => e.Actions).Each(e => e.CachedHeight = null);
            script.Events.SelectMany(e => e.Conditions).Each(e => e.CachedHeight = null);
            lastWidth = availableSize.Width;
        }

        foreach (var variable in script.GlobalVariables)
        {
            MeasureVariable(in context, variable, out var height);
            totalHeight += height + VariableSpacing;
        }
        if (script.GlobalVariables.Count > 0)
            totalHeight += EventSpacing;
        
        foreach (var tuple in ScriptIterator)
        {
            switch (tuple)
            {
                case (var group, null, _, _, _):
                    MeasureGroup(in context, group!, out var groupHeight);
                    totalHeight += groupHeight + EventSpacing;
                    break;
                case (null, var e, bool inGroup, bool groupExpanded, _):
                    if (!groupExpanded)
                        continue;
                    MeasureEventWithActionsAndConditions(in context, e, inGroup, out var eventHeight);
                    totalHeight += eventHeight + EventSpacing;
                    break;
            }
        }
        return new Size(availableSize.Width, totalHeight);
    }

    private class MeasureUtil
    {
        private readonly Panel panel;
        private bool oddMeasurement = false;
        private Control? control;
        
        public MeasureUtil(Panel panel)
        {
            this.panel = panel;
        }

        public Size Measure(object dataContext, double maxWidth, Func<IDataTemplate> templateGetter)
        {
            if (control == null)
            {
                control = templateGetter().Build(null!)!;
                control.DataContext = dataContext;
                panel.Children.Add(control);
                control.ClipToBounds = true;
                control.Arrange(new Rect(-1, -1, 1, 1));
            }
            control.DataContext = dataContext;
            control.InvalidateMeasure();
            control.Measure(new Size(maxWidth, oddMeasurement ? 10000 : 9000));
            oddMeasurement = !oddMeasurement; // <-- ok, this is weird, but this is actually needed so that the measure is not cached by Avalonia, idk why
            var size = control.DesiredSize;
            control.DataContext = null;
            return size;
        }
    }
}