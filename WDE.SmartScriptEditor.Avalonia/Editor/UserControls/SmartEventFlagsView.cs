using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
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
        }

        private long GetValue(KeyValuePair<long,SelectOption> pair)
        {
            if (pair.Key == 0)
                return -1;
            return pair.Key;
        }
    }
    
    public class SmartEventFlagsView : TemplatedControl
    {
        private IDisposable? flagsDisposable;
        private AvaloniaList<IconViewModel> flags = new();
        public static readonly DirectProperty<SmartEventFlagsView, AvaloniaList<IconViewModel>> FlagsProperty = AvaloniaProperty.RegisterDirect<SmartEventFlagsView, AvaloniaList<IconViewModel>>("Flags", o => o.Flags, (o, v) => o.Flags = v);
        
        private SmartEventFlagMapping flagsMapping;

        public SmartEventFlagsView()
        {
            flagsMapping = ViewBind.ResolveViewModel<SmartEventFlagMapping>();
        }
        
        public AvaloniaList<IconViewModel> Flags
        {
            get => flags;
            set => SetAndRaise(FlagsProperty, ref flags, value);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            if (DataContext is SmartEvent se)
            {
                var flagsObservable = se.Flags.ToObservable(t => t.Value);
                var phasesObservable = se.Phases.ToObservable(t => t.Value);
                var combined = flagsObservable.CombineLatest(phasesObservable, (flags, phases) => (flags, phases));
                flagsDisposable = combined.SubscribeAction(tuple =>
                {
                    flags.Clear();
                    var (eventFlagsNum, eventPhasesNum) = tuple;

                    if (eventFlagsNum > 0 && se.Flags.Parameter.HasItems)
                    {
                        foreach (var item in se.Flags.Parameter.Items!)
                        {
                            if (item.Key == 0)
                                continue;
                            
                            if ((eventFlagsNum & item.Key) > 0)
                                flags.Add(new IconViewModel(FlagToIconTextSymbol(item.Key), item.Value.Name, item.Value.Description));
                        }
                    }
                    
                    if (eventPhasesNum > 0 && se.Phases.Parameter.HasItems)
                    {
                        int totalPhases = se.Phases.Parameter.Items!
                            .Count(item => (eventPhasesNum & item.Key) > 0);

                        var currentPhases = se.Phases.Parameter.Items!
                            .Where(item => item.Key != 0 && (eventPhasesNum & item.Key) > 0)
                            .Select(item => PhaseMaskToPhase((int) item.Key));
                        
                        if (totalPhases > 3)
                        {
                            flags.Add(new IconViewModel(currentPhases));
                        }
                        else
                        {
                            foreach (var phase in currentPhases)
                                flags.Add(new IconViewModel(phase));
                        }
                    }
                });
            }
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
        
        public IControl Build(object param)
        {
            if (param is SmartEventFlagsView.IconViewModel {IsPhaseFlag: true})
                return TemplateContent.Load(PhaseView).Control;
            return TemplateContent.Load(FlagView).Control;
        }

        public bool Match(object data)
        {
            return true;
        }
    }
}