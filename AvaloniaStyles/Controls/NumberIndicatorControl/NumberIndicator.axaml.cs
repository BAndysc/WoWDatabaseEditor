using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaloniaStyles.Controls
{
    public class NumberIndicator : TemplatedControl
    {
        private ObservableCollection<Indicator> items;
        internal static readonly DirectProperty<NumberIndicator, ObservableCollection<Indicator>> ItemsProperty = AvaloniaProperty.RegisterDirect<NumberIndicator, ObservableCollection<Indicator>>("Items", o => o.Items, (o, v) => o.Items = v);
        
        private uint stepsCount;
        public static readonly DirectProperty<NumberIndicator, uint> StepsCountProperty = AvaloniaProperty.RegisterDirect<NumberIndicator, uint>("StepsCount", o => o.StepsCount, (o, v) => o.StepsCount = v);
        
        private uint currentStep;
        public static readonly DirectProperty<NumberIndicator, uint> CurrentStepProperty = AvaloniaProperty.RegisterDirect<NumberIndicator, uint>("CurrentStep", o => o.CurrentStep, (o, v) => o.CurrentStep = v);

        static NumberIndicator()
        {
            CurrentStepProperty.Changed.AddClassHandler<NumberIndicator>((o, e) =>
                {
                    if (e.OldValue is uint old)
                    {
                        if (old < o.items.Count)
                            o.items[(int)old].IsActive = false;
                    }

                    if (e.NewValue is uint nnew)
                        if (nnew < o.items.Count)
                            o.items[(int)nnew].IsActive = true; 
                });
            
            StepsCountProperty.Changed.AddClassHandler<NumberIndicator>((o, _) =>
            {
                while (o.items.Count > o.stepsCount)
                    o.items.RemoveAt(o.items.Count - 1);

                while (o.items.Count <  o.stepsCount)
                    o.items.Add(new Indicator((uint)o.items.Count + 1));
                
                if (o.currentStep < o.items.Count)
                    o.items[(int)o.currentStep].IsActive = true;
            });
        }
        
        public NumberIndicator()
        {
            this.items = new();
        }

        internal ObservableCollection<Indicator> Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }

        public uint StepsCount
        {
            get => stepsCount;
            set => SetAndRaise(StepsCountProperty, ref stepsCount, value);
        }

        public uint CurrentStep
        {
            get => currentStep;
            set => SetAndRaise(CurrentStepProperty, ref currentStep, value);
        }
    }

    internal class Indicator : INotifyPropertyChanged
    {
        public uint Number { get; }
        private bool isActive;

        public Indicator(uint number)
        {
            Number = number;
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}