using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WDE.SmartScriptEditor.Models
{
    public class GlobalVariable : INotifyPropertyChanged
    {
        private long key;
        public long Key
        {
            get => key;
            set
            {
                if (key == value)
                    return;
                var old = key;
                key = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Readable));
                KeyChanged?.Invoke(this, old, key);
            }
        }
        
        private GlobalVariableType variableType;
        public GlobalVariableType VariableType
        {
            get => variableType;
            set
            {
                if (variableType == value)
                    return;
                var old = variableType;
                variableType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Readable));
                VariableTypeChanged?.Invoke(this, old, value);
            }
        }
        
        private uint entry;
        public uint Entry
        {
            get => entry;
            set
            {
                if (entry == value)
                    return;
                var old = entry;
                entry = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Entry));
                EntryChanged?.Invoke(this, old, entry);
            }
        }
        
        private string name = "";
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;
                var old = name;
                name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Readable));
                NameChanged?.Invoke(this, old, value);
            }
        }
    
        private string? comment = "";
        public string? Comment
        {
            get => comment;
            set
            {
                if (comment == value)
                    return;
                var old = comment;
                comment = value;
                OnPropertyChanged();
                CommentChanged?.Invoke(this, old, value);
            }
        }
        
        private bool isSelected = false;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value == isSelected)
                    return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Readable
        {
            get
            {
                switch (VariableType)
                {
                    case GlobalVariableType.StoredTarget:
                        return $"Define storedTarget[{Key}] as {Name}";
                    case GlobalVariableType.DataVariable:
                        return $"Define objectData[{Key}] as {Name}";
                    case GlobalVariableType.TimedEvent:
                        return $"Define timedEvent[{Key}] as {Name}";
                    case GlobalVariableType.Action:
                        return $"Define doAction({Key}) as {Name}";
                    case GlobalVariableType.Function:
                        return $"Define function({Key}) as {Name}";
                    case GlobalVariableType.StoredPoint:
                        return $"Define storedPoint[{Key}] as {Name}";
                    case GlobalVariableType.DatabasePoint:
                        return $"Define databasePoint[{Key}] as {Name}";
                    case GlobalVariableType.Actor:
                        return $"Define actor[{Key}] as {Name}";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public event System.Action<GlobalVariable, string?, string?>? CommentChanged; 
        public event System.Action<GlobalVariable, long, long>? KeyChanged; 
        public event System.Action<GlobalVariable, uint, uint>? EntryChanged; 
        public event System.Action<GlobalVariable, string, string>? NameChanged; 
        public event System.Action<GlobalVariable, GlobalVariableType, GlobalVariableType>? VariableTypeChanged; 
        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum GlobalVariableType
    {
        StoredTarget,
        DataVariable,
        TimedEvent,
        Action,
        Function,
        StoredPoint,
        DatabasePoint,
        Actor
    }
}