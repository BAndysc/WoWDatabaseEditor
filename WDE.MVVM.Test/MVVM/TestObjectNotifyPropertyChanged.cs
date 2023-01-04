using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WDE.MVVM.Test.MVVM
{
    [ExcludeFromCodeCoverage]
    internal sealed class TestObjectNotifyPropertyChanged : INotifyPropertyChanged
    {
        public readonly int Field = 0;
        private int number;
        private float floatNumber;
        private string? stringValue;

        public int Number
        {
            get => number;
            set
            {
                if (value == number)
                    return;
                number = value;
                OnPropertyChanged();
            }
        }

        public float FloatNumber
        {
            get => floatNumber;
            set
            {
                if (value.Equals(floatNumber))
                    return;
                floatNumber = value;
                OnPropertyChanged();
            }
        }

        public string? StringValue
        {
            get => stringValue;
            set
            {
                if (value == stringValue)
                    return;
                stringValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int EventHandlers => PropertyChanged?.GetInvocationList().Length ?? 0;
    }
}