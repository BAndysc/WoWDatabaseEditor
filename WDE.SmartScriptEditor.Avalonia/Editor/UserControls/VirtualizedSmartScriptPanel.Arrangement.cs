using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using AvaloniaStyles.Controls;
using WDE.SmartScriptEditor.Avalonia.Extensions;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public partial class VirtualizedSmartScriptPanel
{
    private RecyclableViewList eventViews;
    private RecyclableViewList actionViews;
    private RecyclableViewList commentsViews;
    private RecyclableViewList conditionViews;
    private RecyclableViewList variableViews;
    private RecyclableViewList groupViews;
    private RecyclableViewList newActionViews;
    private RecyclableViewList newConditionViews;
    private RecyclableViewList textBlockViews;
    
    private void ArrangeCondition(in SizingContext context, SmartCondition condition, bool inGroup, double y, out double conditionHeight)
    {
        conditionHeight = condition.CachedHeight ?? 0;
        var conditionRect = (inGroup ? context.grupedConditionRect : context.conditionRect).WithVertical(y, conditionHeight);
        condition.Position = conditionRect.ToPositionSize();
        
        if (context.visibleRect.Intersects(conditionRect))
        {
            var view = conditionViews.GetNext(condition);
            view.Measure(new Size(conditionRect.Width, float.PositiveInfinity));
            view.Arrange(conditionRect);
        }
    }
    
    private Control? ArrangeAction(in SizingContext context, SmartAction action, double y, out double actionHeight)
    {
        actionHeight = action.CachedHeight ?? 0;
        var actionRect = context.actionRect.WithVertical(y, actionHeight);
        action.Position = actionRect.ToPositionSize();
        
        if (context.visibleRect.Intersects(actionRect))
        {
            Control view;
            if (action.Id == SmartConstants.ActionComment)
            {
                view = commentsViews.GetNext(action);
            }
            else
            {
                view = actionViews.GetNext(action);
            }
            view.Measure(new Size(actionRect.Width, float.PositiveInfinity));
            view.Arrange(actionRect);
            return view;
        }

        return null;
    }
    
    private void ArrangeEvent(in SizingContext context, SmartEvent e, bool inGroup, double startY, out double totalHeight)
    {
        double y = startY;
        bool useCompactView = compactView && e != script!.Events[^1];
        double actionsHeight = useCompactView ? 0 : AddActionHeight;
        double conditionsHeight = useCompactView ? 0 : AddConditionHeight;
        
        foreach (var action in e.Actions)
        {
            if (action.Id == SmartConstants.ActionComment && HideComments)
                continue;
            var view = ArrangeAction(in context, action, y, out var actionHeight);
            y += actionHeight + ActionSpacing;
            actionsHeight += actionHeight + ActionSpacing;
            if (view != null)
            {
                view.Classes.Set("last", action == e.Actions[^1]);
            }
        }

        if ((!useCompactView || actionsHeight + AddActionHeight < e.Position.Height) && !AnyDragging && mouseY >= e.Position.Y && mouseY <= e.Position.Bottom)
        {
            var newActionView = newActionViews.GetNext(new NewActionViewModel(){Event = e});
            newActionView.Arrange(context.actionRect.WithVertical(y, AddActionHeight));
        }

        y = startY + (e.CachedHeight ?? 0);
        if (!hideConditions)
        {
            foreach (var condition in e.Conditions)
            {
                ArrangeCondition(in context, condition, inGroup, y, out var conditionHeight);
                y += conditionHeight + ConditionSpacing;
                conditionsHeight += conditionHeight + ConditionSpacing;
            }   
        }
        else
        {
            if (e.Conditions.Count > 0)
            {
                var text = (TextBlock)textBlockViews.GetNext(e.Conditions);
                text.Text = e.Conditions.Count == 1 ? "1 condition" : $"{e.Conditions.Count} conditions";
                text.Padding = new Thickness(10,2,10,2);
                text.Opacity = 0.6f;
                text.Arrange((inGroup ? context.grupedConditionRect : context.conditionRect).WithVertical(y, AddConditionHeight));
                text.IsHitTestVisible = false;
                y += AddConditionHeight;
                conditionsHeight += AddConditionHeight;
            }
        }
        if ((!useCompactView || e.CachedHeight!.Value + conditionsHeight + AddConditionHeight < e.Position.Height) && !AnyDragging && mouseY >= e.Position.Y && mouseY <= e.Position.Bottom)
        {
            var newConditionView = newConditionViews.GetNext(new NewConditionViewModel(){Event = e});
            newConditionView.Arrange((inGroup ? context.grupedConditionRect : context.conditionRect).WithVertical(y, AddConditionHeight));
        }

        totalHeight = Math.Max((e.CachedHeight ?? 0) + conditionsHeight, actionsHeight);
        var eventRect = (inGroup ? context.groupedEventRect : context.eventRect).WithVertical(startY, totalHeight);
        e.Position = eventRect.ToPositionSize();
        e.EventPosition = eventRect.WithHeight(e.CachedHeight ?? 0).Deflate(2).ToPositionSize();
        
        if (context.visibleRect.Intersects(eventRect))
        {
            var eventView = eventViews.GetNext(e);
            eventView.Measure(new Size(eventRect.Width, float.PositiveInfinity));
            eventView.Arrange(eventRect);
        }
    }

    private void ArrangeVariable(in SizingContext context, GlobalVariable variable, bool first, bool last, double y, out double height)
    {
        height = variable.CachedHeight ?? 0;
        var rect = context.variableRect.WithVertical(y, height);
        variable.Position = rect.ToPositionSize();
        if (context.visibleRect.Intersects(rect))
        {
            var groupView = variableViews.GetNext(variable);
            groupView.Classes.Set("first", first);
            groupView.Classes.Set("last", last);
            groupView.Measure(new Size(context.variableRect.Width, float.PositiveInfinity));
            groupView.Arrange(rect);
        }
    }

    private void ArrangeGroup(in SizingContext context, SmartGroup group, double y, out double height)
    {
        height = group.CachedHeight ?? 0;
        var rect = context.groupRect.WithVertical(y, height);
        group.Position = rect.ToPositionSize();
        if (context.visibleRect.Intersects(rect))
        {
            var groupView = groupViews.GetNext(group);
            groupView.Measure(new Size(context.groupRect.Width, float.PositiveInfinity));
            groupView.Arrange(rect);
        }
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (script == null)
            return default;

        childrenContainer.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

        var context = new SizingContext(finalSize.Width, Padding, EventPaddingLeft, VisibleRect);
        
        double y = PaddingTop;
        variableViews.Reset(VariableItemTemplate);
        eventViews.Reset(EventItemTemplate);
        actionViews.Reset(ActionItemTemplate);
        commentsViews.Reset(ActionItemTemplate);
        conditionViews.Reset(ConditionItemTemplate);
        groupViews.Reset(GroupItemTemplate);
        newActionViews.Reset(NewActionItemTemplate);
        newConditionViews.Reset(NewConditionItemTemplate);
        textBlockViews.Reset(new FuncDataTemplate(typeof(object), (_, _) => new TextBlock(), true));

        for (var i = 0; i < script.GlobalVariables.Count; i++)
        {
            var variable = script.GlobalVariables[i];
            ArrangeVariable(in context, variable, i == 0, i == script.GlobalVariables.Count - 1, y, out var variableHeight);
            y += variableHeight + VariableSpacing;
        }

        if (script.GlobalVariables.Count > 0)
            y += EventSpacing;
        
        foreach (var tuple in ScriptIterator)
        {
            switch (tuple)
            {
                case (var group, null, _, _, _):
                    ArrangeGroup(in context, group!, y, out var groupHeight);
                    y += groupHeight + EventSpacing;
                    break;
                case (null, var e, var inGroup, var groupExpanded, var eventIndex):
                    if (!groupExpanded)
                        continue;
                    
                    ArrangeEvent(in context, e, inGroup, y, out var eventHeight);
                    y += eventHeight + EventSpacing;
                    break;
            }
        }
        
        conditionViews.Finish();
        actionViews.Finish();
        commentsViews.Finish();
        eventViews.Finish();
        groupViews.Finish();
        newActionViews.Finish();
        newConditionViews.Finish();
        variableViews.Finish();
        textBlockViews.Finish();

        UpdateOverElement();
        return finalSize;
    }
}