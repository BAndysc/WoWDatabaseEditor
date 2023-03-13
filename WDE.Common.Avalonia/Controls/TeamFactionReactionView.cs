using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WDE.Common.Avalonia.Utils;
using WDE.Common.DBC;
using WDE.Common.DBC.Structs;

namespace WDE.Common.Avalonia.Controls;

public class TeamFactionReactionView : Panel
{
    public static readonly StyledProperty<uint> FactionTemplateIdProperty = AvaloniaProperty.Register<TeamFactionReactionView, uint>(nameof(FactionTemplateId));

    public uint FactionTemplateId
    {
        get => GetValue(FactionTemplateIdProperty);
        set => SetValue(FactionTemplateIdProperty, value);
    }

    private TextBlock? ally, horde;
    private static IFactionTemplateStore factionTemplateStore;

    private static IBrush NeutralBrush = new SolidColorBrush(Colors.Orange);
    private static IBrush FriendlyBrush = new SolidColorBrush(Colors.LimeGreen);
    private static IBrush HostileBrush = new SolidColorBrush(Colors.Red);
    
    static TeamFactionReactionView()
    {
        factionTemplateStore = ViewBind.ResolveViewModel<IFactionTemplateStore>();
        FactionTemplateIdProperty.Changed.AddClassHandler<TeamFactionReactionView>((view, _) => view.UpdateColors());
    }

    private void UpdateColors()
    {
        if (ally == null || horde == null)
            return;
        
        ally.Foreground = horde.Foreground = NeutralBrush;
        
        var template = factionTemplateStore.GetFactionTemplate(FactionTemplateId);
        if (!template.HasValue)
            return;

        if (template.Value.IsFriendlyTo(in FactionTemplate.Horde))
            horde.Foreground = FriendlyBrush;
        else if (template.Value.IsHostileTo(in FactionTemplate.Horde))
            horde.Foreground = HostileBrush;
        
        if (template.Value.IsFriendlyTo(in FactionTemplate.Alliance))
            ally.Foreground = FriendlyBrush;
        else if (template.Value.IsHostileTo(in FactionTemplate.Alliance))
            ally.Foreground = HostileBrush;
    }

    public override void ApplyTemplate()
    {
        base.ApplyTemplate();
        if (ally == null)
        {
            ally = new TextBlock()
            {
                Text = "A",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Children.Add(ally);
        }

        if (horde == null)
        {
            horde = new TextBlock()
            {
                Text = "H",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Children.Add(horde);
        }
        InvalidateMeasure();
        InvalidateArrange();
        UpdateColors();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = 4;
        var left = new Rect(0, 0, finalSize.Width / 2 - spacing, finalSize.Height);
        var right = new Rect(finalSize.Width / 2 + spacing, 0, finalSize.Width / 2 - spacing, finalSize.Height);
        
        ally?.Arrange(left);
        horde?.Arrange(right);

        return finalSize;
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        ally?.Measure(availableSize);
        horde?.Measure(availableSize);
        var allySizing = ally?.DesiredSize ?? default;
        var hordeSizing = horde?.DesiredSize ?? default;
        return allySizing + hordeSizing + new Size(8, 0);
    }
}