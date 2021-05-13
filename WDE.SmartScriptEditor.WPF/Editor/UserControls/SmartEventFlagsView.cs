using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    public class SmartEventFlagsView : Control
    {
        private System.IDisposable? flagsDisposable;

        public static DependencyProperty FlagsProperty =
            DependencyProperty.Register(nameof(Flags), typeof(ObservableCollection<IconViewModel>), typeof(SmartEventFlagsView));

        static SmartEventFlagsView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SmartEventFlagsView), new FrameworkPropertyMetadata(typeof(SmartEventFlagsView)));
        }

        public SmartEventFlagsView()
        {
            Flags = new();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdatedDataContext();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            flagsDisposable?.Dispose();
            flagsDisposable = null;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatedDataContext();
        }

        public ObservableCollection<IconViewModel> Flags
        {
            get => (ObservableCollection<IconViewModel>) GetValue(FlagsProperty);
            set => SetValue(FlagsProperty, value);
        }

        protected void UpdatedDataContext()
        {
            flagsDisposable?.Dispose();
            flagsDisposable = null;
            if (DataContext is SmartEvent se)
            {
                var flagsObservable = se.Flags.ToObservable(t => t.Value);
                var phasesObservable = se.Phases.ToObservable(t => t.Value);
                var combined = flagsObservable.CombineLatest(phasesObservable, (flags, phases) => (flags, phases));
                flagsDisposable = combined.SubscribeAction(tuple =>
                {
                    Flags.Clear();
                    var (eventFlagsNum, eventPhasesNum) = tuple;

                    if (eventFlagsNum > 0 && se.Flags.Parameter.HasItems)
                    {
                        foreach (var item in se.Flags.Parameter.Items!)
                        {
                            if (item.Key == 0)
                                continue;
                            
                            if ((eventFlagsNum & item.Key) > 0)
                                Flags.Add(new IconViewModel(FlagToIconTextSymbol(item.Key), item.Value.Name, item.Value.Description));
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
                            Flags.Add(new IconViewModel(currentPhases));
                        }
                        else
                        {
                            foreach (var phase in currentPhases)
                                Flags.Add(new IconViewModel(phase));
                        }
                    }
                });
            }
        }

        private static string FlagToIconTextSymbol(long flag)
        {
            SmartEventFlag f = (SmartEventFlag) flag;
            if (f == SmartEventFlag.NotRepeatable)
                return "I";
            if (f == SmartEventFlag.DebugOnly)
                return "d";
            if (f == SmartEventFlag.WhileCharmed)
                return "c";
            if (f == SmartEventFlag.DontReset)
                return "r";
            if (f == SmartEventFlag.Difficulty0)
                return "A";
            if (f == SmartEventFlag.Difficulty1)
                return "B";
            if (f == SmartEventFlag.Difficulty2)
                return "C";
            if (f == SmartEventFlag.Difficulty3)
                return "D";
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
}