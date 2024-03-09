using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls;

public class VirtualizedGridCheckBox : CheckBox
{
    private IMultiIndexContainer? checkedIndices;
    public static readonly DirectProperty<VirtualizedGridCheckBox, IMultiIndexContainer?> CheckedIndicesProperty = 
        AvaloniaProperty.RegisterDirect<VirtualizedGridCheckBox, IMultiIndexContainer?>(nameof(CheckedIndices), o => o.CheckedIndices, (o, v) => o.CheckedIndices = v);
    
    public IMultiIndexContainer? CheckedIndices
    {
        get => checkedIndices;
        set => SetAndRaise(CheckedIndicesProperty, ref checkedIndices, value);
    }

    private int index = -1;
    public static readonly DirectProperty<VirtualizedGridCheckBox, int> IndexProperty = 
        AvaloniaProperty.RegisterDirect<VirtualizedGridCheckBox, int>(nameof(Index), o => o.Index, (o, v) => o.Index = v);
    
    public int Index
    {
        get => index;
        set => SetAndRaise(IndexProperty, ref index, value);
    }

    protected override Type StyleKeyOverride => typeof(CheckBox);

    static VirtualizedGridCheckBox()
    {
        IndexProperty.Changed.AddClassHandler<VirtualizedGridCheckBox>((checkBox, e) =>
        {
            if (!checkBox.attachedToVisualTree)
                return;

            checkBox.UpdateState();
        });

        CheckedIndicesProperty.Changed.AddClassHandler<VirtualizedGridCheckBox>((checkBox, e) =>
        {
            if (!checkBox.attachedToVisualTree)
                return;
            
            checkBox.Unbind();
            checkBox.Bind();
        });
    }

    private void UpdateState()
    {
        if (checkedIndices is { })
            IsChecked = checkedIndices.Contains(index);
        else
            IsChecked = null;
    }

    private bool attachedToVisualTree;
    private IMultiIndexContainer? boundCheckedIndices;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attachedToVisualTree = true;
        Bind();
    }

    private void OnCheckedIndicesChanged()
    {
        UpdateState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        attachedToVisualTree = false;
        Unbind();
        base.OnDetachedFromVisualTree(e);
    }

    private void Bind()
    {
        UpdateState();
        Debug.Assert(boundCheckedIndices == null);
        if (checkedIndices is { })
        {
            checkedIndices.Changed += OnCheckedIndicesChanged;
            boundCheckedIndices = checkedIndices;
        }
    }

    private void Unbind()
    {
        UpdateState();
        if (boundCheckedIndices != null)
        {
            boundCheckedIndices.Changed -= OnCheckedIndicesChanged;
            boundCheckedIndices = null;
        }
    }

    protected override void Toggle()
    {
        if (CheckedIndices is not { } checkedIndices)
            return;
        
        if (checkedIndices.Contains(index))
            checkedIndices.Remove(index);
        else
            checkedIndices.Add(index);
    }
}