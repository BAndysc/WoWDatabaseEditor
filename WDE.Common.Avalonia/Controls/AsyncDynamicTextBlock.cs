using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

/// <summary>
/// a text control that can compute an async value and display it 
/// </summary>
public class AsyncDynamicTextBlock : TextBlock
{
    protected override Type StyleKeyOverride => typeof(TextBlock);
    
    public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<AsyncDynamicTextBlock, object?>(nameof(Value));
    
    public static readonly StyledProperty<int> UpdateDelayProperty = AvaloniaProperty.Register<AsyncDynamicTextBlock,  int>(nameof(UpdateDelay));

    public static readonly StyledProperty<Func<object, CancellationToken, Task<string?>>> EvaluatorProperty = AvaloniaProperty.Register<AsyncDynamicTextBlock, Func<object, CancellationToken, Task<string?>>>(nameof(Evaluator));

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public int UpdateDelay
    {
        get => GetValue(UpdateDelayProperty);
        set => SetValue(UpdateDelayProperty, value);
    }

    public Func<object, CancellationToken, Task<string?>> Evaluator
    {
        get => GetValue(EvaluatorProperty);
        set => SetValue(EvaluatorProperty, value);
    }
    
    static AsyncDynamicTextBlock()
    {
        ValueProperty.Changed.AddClassHandler<AsyncDynamicTextBlock>((control, e) =>
        {
            control.UpdateText().ListenErrors();
        });
    }

    private CancellationTokenSource? pendingTask;

    private async Task UpdateText()
    {
        pendingTask?.Cancel();

        var token = pendingTask = new();

        Text = "...";
        
        await Task.Delay(UpdateDelay, token.Token);

        if (token.IsCancellationRequested)
            return;

        var value = Value;
        if (value != null)
        {
            var text = await Evaluator(value, token.Token);

            if (text != null && !token.IsCancellationRequested)
                Text = text;
        }
        else
        {
            Text = "";
        }
        
        if (token == pendingTask)
            pendingTask = null;
    }
}