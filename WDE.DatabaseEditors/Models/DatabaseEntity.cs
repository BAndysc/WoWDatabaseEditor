using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseEntity : INotifyPropertyChanged
    {
        public Dictionary<string, IDatabaseField> Cells { get; }

        private IReadOnlyList<ICondition>? conditions;

        public IReadOnlyList<ICondition>? Conditions
        {
            get => conditions;
            set
            {
                var old = conditions;
                conditions = value;
                OnAction?.Invoke(new DatabaseEntityConditionsChangedHistoryAction(this, old, value));
                OnPropertyChanged();
            }
        }

        public IEnumerable<IDatabaseField> Fields => Cells.Values;

        public event System.Action<IHistoryAction>? OnAction;
        
        public bool ExistInDatabase { get; set; }
        
        public uint Key { get; }
        
        public DatabaseEntity(bool existInDatabase, uint key, Dictionary<string, IDatabaseField> cells, IReadOnlyList<ICondition>? conditions)
        {
            ExistInDatabase = existInDatabase;
            Key = key;
            Cells = cells;
            Conditions = conditions;
            foreach (var databaseField in Cells)
            {
                databaseField.Value.OnChanged += action =>
                {
                    OnAction?.Invoke(action);
                };
            }
        }

        public IDatabaseField? GetCell(string columnName)
        {
            if (Cells.TryGetValue(columnName, out var cell))
                return cell;
            return null;
        }

        public void SetTypedCellOrThrow<T>(string column, T? value) where T : IComparable<T>
        {
            var cell = GetCell(column);
            if (cell == null)
                throw new Exception("No column named " + column);
            var typed = cell as DatabaseField<T>;
            if (typed == null)
                throw new Exception("No column named " + column + " with type " + typeof(T));
            typed.Current.Value = value;
        }
        
        public T? GetTypedValueOrThrow<T>(string columnName) where T : IComparable<T>
        {
            var cell = GetCell(columnName);
            if (cell == null)
                throw new Exception("No column named " + columnName);
            var typed = cell as DatabaseField<T>;
            if (typed == null)
                throw new Exception("No column named " + columnName + " with type " + typeof(T));
            return typed.Current.Value;
        }

        public DatabaseEntity Clone()
        {
            var fields = new Dictionary<string, IDatabaseField>();
            foreach (var field in Cells)
                fields[field.Key] = field.Value.Clone();
            
            return new DatabaseEntity(ExistInDatabase, Key, fields, Conditions == null ? null : CloneConditions(Conditions));
        }

        private IReadOnlyList<ICondition> CloneConditions(IReadOnlyList<ICondition> conditions)
        {
            return conditions.Select(c => new AbstractCondition(c)).ToList<ICondition>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}