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
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    public class EventAiEventFlagsView : TemplatedControl
    {
        private IDisposable? flagsDisposable;
        private AvaloniaList<IconViewModel> flags = new();
        public static readonly DirectProperty<EventAiEventFlagsView, AvaloniaList<IconViewModel>> FlagsProperty = AvaloniaProperty.RegisterDirect<EventAiEventFlagsView, AvaloniaList<IconViewModel>>("Flags", o => o.Flags, (o, v) => o.Flags = v);

        public AvaloniaList<IconViewModel> Flags
        {
            get => flags;
            set => SetAndRaise(FlagsProperty, ref flags, value);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            if (DataContext is EventAiEvent se)
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

        private static string FlagToIconTextSymbol(long flag)
        {
            EventAiFlag f = (EventAiFlag) flag;
            if (f == EventAiFlag.Repeatable)
                return "🔁";
            if (f == EventAiFlag.DebugOnly)
                return "🐛";
            if (f == EventAiFlag.CombatAction)
                return "🛡️";
            if (f == EventAiFlag.RandomAction)
                return "🎲";
            if (f == EventAiFlag.MeleeModeOnly)
                return "🗡";
            if (f == EventAiFlag.RangedModeOnly)
                return "🏹";
            if (f == EventAiFlag.Difficulty0)
                return "A";
            if (f == EventAiFlag.Difficulty1)
                return "B";
            if (f == EventAiFlag.Difficulty2)
                return "C";
            if (f == EventAiFlag.Difficulty3)
                return "D";
            return "?";
        }

        private static int PhaseMaskToPhase(int phaseMask)
        {
            for (int i = 0; i < 31; ++i)
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
                ToolTip = "Event will NOT activate in script phase " + phase;
            }

            public IconViewModel(IEnumerable<int> phases)
            {
                IsPhaseFlag = true;
                Text = "...";
                ToolTip = "Event will NOT activate in script phases: " + string.Join(", ", phases);
            }
            
            public IconViewModel(string text, string flagName, string? tooltip)
            {
                Text = text;
                ToolTip = tooltip != null ? $"Event flag {flagName}: {tooltip}" : $"Event flag {flagName}";
                IsPhaseFlag = false;
            }
        }
    }

    public class EventFlagPhaseDataSelector : IDataTemplate
    {
        [TemplateContent]
        public object? PhaseView { get; set; }
        
        [TemplateContent]
        public object? FlagView { get; set; }
        
        public Control? Build(object? param)
        {
            if (param is EventAiEventFlagsView.IconViewModel {IsPhaseFlag: true})
                return TemplateContent.Load(PhaseView)?.Result;
            return TemplateContent.Load(FlagView)?.Result;
        }

        public bool Match(object? data)
        {
            return true;
        }
    }
}