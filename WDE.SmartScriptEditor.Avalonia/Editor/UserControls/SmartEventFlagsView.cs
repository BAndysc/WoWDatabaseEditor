using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Styling;
using AvaloniaStyles;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    public class SmartEventFlagMapping
    {
        public long NonRepeatable { get; }
        public long DebugOnly { get; }
        public long WhileCharmed { get; }
        public long DontReset { get; }
        public long Difficulty0 { get; }
        public long Difficulty1 { get; }
        public long Difficulty2 { get; }
        public long Difficulty3 { get; }
        public long DifficultyLfr { get; }
        public long DifficultyFlex { get; }
        public long Template { get; }
        public long ConditionFailedTime { get; }
        public long ConditionFailedOnce { get; }
        public long UpdateTimer { get; }
        public long NonBreakableLink { get; }
        
        public bool HasValues { get; }
        
        public SmartEventFlagMapping(IEditorFeatures editorFeatures)
        {
            var param = editorFeatures.EventFlagsParameter;
            if (param.Items == null)
                return;

            HasValues = true;
            NonRepeatable = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Not repeatable"));
            DebugOnly = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Debug only"));
            WhileCharmed = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "While charmed"));
            DontReset = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Don't reset"));
            Difficulty0 = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Difficulty 0"));
            Difficulty1 = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Difficulty 1"));
            Difficulty2 = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Difficulty 2"));
            Difficulty3 = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Difficulty 3"));
            DifficultyLfr = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "LFR difficulty"));
            DifficultyFlex = GetValue(param.Items.FirstOrDefault(x => x.Value.Name == "Flex difficulty"));
            Template = GetValue(param.Items.FirstOrDefault(x => x.Value.Name.Contains("template")));
            ConditionFailedTime = GetValue(param.Items.FirstOrDefault(x => x.Value.Name.Contains("condition")&& x.Value.Name.Contains("time")));
            ConditionFailedOnce = GetValue(param.Items.FirstOrDefault(x => x.Value.Name.Contains("Condition")&& x.Value.Name.Contains("once")));
            UpdateTimer = GetValue(param.Items.FirstOrDefault(x => x.Value.Name.Contains("timer")));
            NonBreakableLink = GetValue(param.Items.FirstOrDefault(x => x.Value.Name.Contains("breakable")));
        }

        private long GetValue(KeyValuePair<long,SelectOption> pair)
        {
            if (pair.Key == 0)
                return -1;
            return pair.Key;
        }
    }
    
    public class SmartEventFlagsView : Control
    {
        public const double BoxSize = FontSize + Padding * 2;
        public const double FontSize = 9;
        public const double Padding = 2;
        public const double Spacing = 2;
        public const int ItemsInRow = 2;
        public const double TotalWidth = BoxSize * ItemsInRow + Spacing * (ItemsInRow - 1);
        
        private IDisposable? flagsDisposable;
        private SmartEventFlagMapping flagsMapping;
        private IBrush? foreground;

        public SmartEventFlagsView()
        {
            flagsMapping = ViewBind.ResolveViewModel<SmartEventFlagMapping>();
            if (Application.Current!.Styles.TryGetResource("SmartScripts.Event.Flag.Foreground", SystemTheme.EffectiveThemeIsDark ? ThemeVariant.Dark  : ThemeVariant.Light,
                    out var eventFlagForegroundColor)
                && eventFlagForegroundColor is IBrush eventFlagForegroundBrush)
            {
                foreground = eventFlagForegroundBrush;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            InvalidateMeasure();
            flagsDisposable?.Dispose();
            BindToDataContext();
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            if (flagsDisposable == null)
                BindToDataContext();
        }

        private struct RenderContext
        {
            public double x, y;
        }
        
        private void RenderIcon(DrawingContext context, ref RenderContext data, string symbol)
        {
            if (char.IsAscii(symbol[0]))
                context.DrawRectangle(new SolidColorBrush(Color.Parse("#1976d2")), null, new Rect(data.x, data.y, BoxSize, BoxSize), BoxSize / 2, BoxSize / 2);
            
            var tl = new TextLayout(symbol, new Typeface(GetValue(TextElement.FontFamilyProperty)), FontSize, foreground, TextAlignment.Center, maxWidth: BoxSize);
            tl.Draw(context, new Point(data.x, data.y + 1));
            
            data.x -= BoxSize + Spacing;
            if (data.x < 0)
            {
                data.x = TotalWidth - BoxSize;
                data.y += BoxSize + Spacing;
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            var icons = GetIconsCount();
            if (icons == 0)
                return new Size(0, 0);
            else if (icons == 1)
                return new Size(BoxSize, BoxSize);
            int rows = (int)Math.Ceiling(icons / 2f);
            return new Size(TotalWidth,  rows * BoxSize + (rows - 1) * Spacing);
        }

        private int GetIconsCount()
        {
            if (DataContext is not SmartEvent e)
                return 0;

            var eventFlagsNum = e.Flags.Value;
            var eventPhasesNum = e.Phases.Value;

            if (eventFlagsNum == 0 && eventPhasesNum == 0)
                return 0;

            int count = 0;
            
            if (eventFlagsNum > 0 && e.Flags.Parameter.HasItems)
            {
                foreach (var item in e.Flags.Parameter.Items!)
                {
                    if (item.Key == 0)
                        continue;

                    if ((eventFlagsNum & item.Key) > 0)
                        count++;
                }
            }

            if (eventPhasesNum > 0 && e.Phases.Parameter.HasItems)
            {
                int totalPhases = e.Phases.Parameter.Items!
                    .Count(item => (eventPhasesNum & item.Key) > 0);

                if (totalPhases > 3)
                    count++;
                else
                    count += totalPhases;
            }

            return count;
        }
        
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (DataContext is SmartEvent e)
            {
                RenderContext data = new();
                data.x = TotalWidth - BoxSize;
                if (GetIconsCount() == 1)
                    data.x = 0;
                
                var eventFlagsNum = e.Flags.Value;
                var eventPhasesNum = e.Phases.Value;

                if (eventFlagsNum > 0 && e.Flags.Parameter.HasItems)
                {
                    foreach (var item in e.Flags.Parameter.Items!)
                    {
                        if (item.Key == 0)
                            continue;

                        if ((eventFlagsNum & item.Key) > 0)
                        {
                            RenderIcon(context, ref data, FlagToIconTextSymbol(item.Key));
                        }
                    }
                }
                    
                if (eventPhasesNum > 0 && e.Phases.Parameter.HasItems)
                {
                    int totalPhases = e.Phases.Parameter.Items!
                        .Count(item => (eventPhasesNum & item.Key) > 0);

                    var currentPhases = e.Phases.Parameter.Items!
                        .Where(item => item.Key != 0 && (eventPhasesNum & item.Key) > 0)
                        .Select(item => PhaseMaskToPhase((int) item.Key));
                        
                    if (totalPhases > 3)
                    {
                        RenderIcon(context, ref data, "...");
                    }
                    else
                    {
                        foreach (var phase in currentPhases)
                        {
                            RenderIcon(context, ref data, phase.ToString());
                        }
                    }
                }
            }
        }

        private void BindToDataContext()
        {
            if (DataContext is SmartEvent se)
            {
                var flagsObservable = se.Flags.ToObservable(t => t.Value);
                var phasesObservable = se.Phases.ToObservable(t => t.Value);
                var combined = flagsObservable.CombineLatest(phasesObservable, (flags, phases) => (flags, phases));
                flagsDisposable = combined.SubscribeAction(tuple =>
                {
                    var tip = GenerateToolTip();
                    SetValue(ToolTip.TipProperty, tip);
                    InvalidateMeasure();
                    InvalidateVisual();
                });
            }
        }

        private static List<string> tempList = new();
        private string? GenerateToolTip()
        {
            if (DataContext is not SmartEvent e)
                return null;
            if (e.Flags.Value == 0 && e.Phases.Value == 0)
                return null;
            
            tempList.Clear();
            var eventFlagsNum = e.Flags.Value;
            var eventPhasesNum = e.Phases.Value;

            if (eventFlagsNum > 0 && e.Flags.Parameter.HasItems)
            {
                foreach (var item in e.Flags.Parameter.Items!)
                {
                    if (item.Key == 0)
                        continue;

                    if ((eventFlagsNum & item.Key) > 0)
                        tempList.Add(" • " + FlagToIconTextSymbol(item.Key) + " - flag " + item.Value.Name);
                }
            }
                    
            if (eventPhasesNum > 0 && e.Phases.Parameter.HasItems)
            {
                var currentPhases = e.Phases.Parameter.Items!
                    .Where(item => item.Key != 0 && (eventPhasesNum & item.Key) > 0)
                    .Select(item => PhaseMaskToPhase((int) item.Key));
                       
                foreach (var phase in currentPhases)
                {
                    tempList.Add(" • Phase " + phase);
                }
            }

            return string.Join("\n", tempList);
        }

        private string FlagToIconTextSymbol(long flag)
        {
            if (flag == flagsMapping.NonRepeatable)
                return "🔁";
            if (flag == flagsMapping.DebugOnly)
                return "🐛";
            if (flag == flagsMapping.WhileCharmed)
                return "c";
            if (flag == flagsMapping.DontReset)
                return "r";
            if (flag == flagsMapping.Difficulty0)
                return "A";
            if (flag == flagsMapping.Difficulty1)
                return "B";
            if (flag == flagsMapping.Difficulty2)
                return "C";
            if (flag == flagsMapping.Difficulty3)
                return "D";
            if (flag == flagsMapping.DifficultyLfr)
                return "E";
            if (flag == flagsMapping.DifficultyFlex)
                return "f";
            if (flag == flagsMapping.Template)
                return "🎫";
            if (flag == flagsMapping.ConditionFailedOnce)
                return "💢";
            if (flag == flagsMapping.ConditionFailedTime)
                return "💥";
            if (flag == flagsMapping.UpdateTimer)
                return "⏱️";
            if (flag == flagsMapping.NonBreakableLink)
                return "🔗";
            return "?";
        }

        private static int PhaseMaskToPhase(int phaseMask)
        {
            for (int i = 0; i < 12; ++i)
            {
                if (((1 << i) & phaseMask) > 0)
                    return i + 1;
            }

            return 0;
        }
        
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            flagsDisposable?.Dispose();
            flagsDisposable = null;
        }

        public struct IconViewModel
        {
            public bool IsPhaseFlag { get; set; }
            public string Text { get; set; }
            public string ToolTip { get; set; }
            
            public IconViewModel(long phase)
            {
                IsPhaseFlag = true;
                Text = phase.ToString();
                ToolTip = "Event will only activate in script phase " + phase;
            }

            public IconViewModel(IEnumerable<int> phases)
            {
                IsPhaseFlag = true;
                Text = "...";
                ToolTip = "Event will only activate in script phases: " + string.Join(", ", phases);
            }
            
            public IconViewModel(string text, string flagName, string? tooltip)
            {
                Text = text;
                ToolTip = tooltip != null ? $"Event flag {flagName}: {tooltip}" : $"Event flag {flagName}";
                IsPhaseFlag = false;
            }
        }
    }

    public class SmartEventFlagPhaseDataSelector : IDataTemplate
    {
        [TemplateContent]
        public object? PhaseView { get; set; }
        
        [TemplateContent]
        public object? FlagView { get; set; }
        
        public Control? Build(object? param)
        {
            if (param is SmartEventFlagsView.IconViewModel {IsPhaseFlag: true})
                return TemplateContent.Load(PhaseView)?.Result;
            return TemplateContent.Load(FlagView)?.Result;
        }

        public bool Match(object? data)
        {
            return true;
        }
    }
}
