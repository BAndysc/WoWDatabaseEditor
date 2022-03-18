using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseEntity : INotifyPropertyChanged
    {
        private DatabaseKey key;
        
        public Dictionary<string, IDatabaseField> Cells { get; }

        private IReadOnlyList<ICondition>? conditions;

        public IReadOnlyList<ICondition>? Conditions
        {
            get => conditions;
            set
            {
                var old = conditions;
                conditions = value;
                OnConditionsChanged?.Invoke(this, old, value);
                OnPropertyChanged();
            }
        }

        public IEnumerable<IDatabaseField> Fields => Cells.Values;

        public event System.Action<IHistoryAction>? OnAction;
        
        public event System.Action<DatabaseEntity, IReadOnlyList<ICondition>?, IReadOnlyList<ICondition>?>? OnConditionsChanged;
        
        public bool ExistInDatabase { get; set; }

        public DatabaseKey GenerateKey(DatabaseTableDefinitionJson definition)
        {
            return Phantom ? new DatabaseKey(definition.PrimaryKey.Select(GetTypedValueOrThrow<long>)) : Key;
        }
        
        public DatabaseKey Key
        {
            get
            {
                if (!key.IsPhantomKey)
                    return key;
                throw new Exception("Phantom key can't be generated");
            }
        }

        public bool Phantom => key.IsPhantomKey;
        
        public DatabaseEntity(bool existInDatabase, 
            DatabaseKey key,
            Dictionary<string, IDatabaseField> cells,
            IReadOnlyList<ICondition>? conditions)
        {
            ExistInDatabase = existInDatabase;
            this.key = key;
            Cells = cells;
            Conditions = conditions;
            foreach (var databaseField in Cells)
            {
                databaseField.Value.OnChanged += action =>
                {
                    if (action is IDatabaseFieldHistoryAction a)
                        OnAction?.Invoke(new DatabaseFieldWithKeyHistoryAction(a, key));
                    else
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

        public DatabaseEntity Clone(DatabaseKey? newKey = null, bool? existInDatabase = null)
        {
            var fields = new Dictionary<string, IDatabaseField>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var field in Cells)
                fields[field.Key] = field.Value.Clone();
            
            return new DatabaseEntity(existInDatabase ?? ExistInDatabase, newKey ?? Key, fields, Conditions == null ? null : CloneConditions(Conditions));
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