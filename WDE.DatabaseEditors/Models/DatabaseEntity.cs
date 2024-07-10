using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Utils;

namespace WDE.DatabaseEditors.Models
{
    public sealed class DatabaseEntity : INotifyPropertyChanged
    {
        private DatabaseKey key;
        
        public Dictionary<ColumnFullName, IDatabaseField> Cells { get; }

        private IReadOnlyList<ICondition>? conditions;
        
        public bool ConditionsModified { get; set; }

        public IReadOnlyList<ICondition>? Conditions
        {
            get => conditions;
            set
            {
                ConditionsModified = true;
                var old = conditions;
                conditions = value;
                OnConditionsChanged?.Invoke(this, old, value);
                OnPropertyChanged();
            }
        }

        public IEnumerable<IDatabaseField> Fields => Cells.Values;

        public event System.Action<IHistoryAction>? OnAction;
        public event Action<DatabaseEntity, ColumnFullName, Action<IValueHolder>, Action<IValueHolder>>? FieldValueChanged;
        
        public event System.Action<DatabaseEntity, IReadOnlyList<ICondition>?, IReadOnlyList<ICondition>?>? OnConditionsChanged;
        
        public bool ExistInDatabase { get; set; }

        public DatabaseKey GenerateKey(DatabaseTableDefinitionJson definition)
        {
            return Phantom ? ForceGenerateKey(definition) : Key;
        }
        
        public DatabaseKey ForceGenerateKey(DatabaseTableDefinitionJson definition)
        {
            return new DatabaseKey(definition.PrimaryKey.Select(GetLongMapping));
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
            Dictionary<ColumnFullName, IDatabaseField> cells,
            IReadOnlyList<ICondition>? conditions)
        {
            Debug.Assert(!(existInDatabase && key.IsPhantomKey)); // phantom keys can never exist in the database!!
            ExistInDatabase = existInDatabase;
            this.key = key;
            Cells = cells;
            this.conditions = conditions;
            foreach (var databaseField in Cells)
            {
                databaseField.Value.OnChanged += action =>
                {
                    if (action is IDatabaseFieldHistoryAction a)
                        OnAction?.Invoke(new DatabaseFieldWithKeyHistoryAction(a, key));
                    else
                        OnAction?.Invoke(action);
                };
                databaseField.Value.ValueChanged += ((columnName, undo, redo) =>
                {
                    FieldValueChanged?.Invoke(this, columnName, undo, redo);
                });
            }
        }

        // public IDatabaseField? GetCell(string columnName)
        // {
        //     return Cells.GetValueOrDefault(new ColumnFullName(null, columnName));
        // }

        public IDatabaseField? GetCell(string foreignTableName, string columnName)
        {
            return Cells.GetValueOrDefault(new ColumnFullName(foreignTableName, columnName));
        }

        public IDatabaseField? GetCell(ColumnFullName columnName)
        {
            return Cells.GetValueOrDefault(columnName);
        }

        public void SetTypedCellOrThrow<T>(ColumnFullName column, T? value) where T : IComparable<T>
        {
            SetTypedCellOrThrowUnsafe(EntityChangeFlags.ChangeCurrent, column, value);
        }

        public void SetTypedCellOrThrow<T>(string column, T? value) where T : IComparable<T>
        {
            SetTypedCellOrThrowUnsafe(EntityChangeFlags.ChangeCurrent, ColumnFullName.Parse(column), value);
        }

        public void SetTypedCellOrThrowUnsafe<T>(EntityChangeFlags flags, ColumnFullName column, T? value) where T : IComparable<T>
        {
            var cell = GetCell(column);
            if (cell == null)
                throw new Exception("No column named " + column);
            var typed = cell as DatabaseField<T>;
            if (typed == null)
                throw new Exception("No column named " + column + " with type " + typeof(T));
            if (flags.HasFlagFast(EntityChangeFlags.ChangeCurrent))
                typed.Current.Value = value;
            if (flags.HasFlagFast(EntityChangeFlags.ChangeOriginal))
                typed.Original.Value = value;
        }

        public T? GetTypedValueOrThrow<T>(string columnName) where T : IComparable<T> => GetTypedValueOrThrow<T>(ColumnFullName.Parse(columnName));

        public T? GetTypedValueOrThrow<T>(ColumnFullName columnName) where T : IComparable<T>
        {
            var cell = GetCell(columnName);
            if (cell == null)
                throw new Exception("No column named " + columnName);
            var typed = cell as DatabaseField<T>;
            if (typed == null)
                throw new Exception("No column named " + columnName + " with type " + typeof(T));
            return typed.Current.Value;
        }

        public long GetLongMapping(ColumnFullName columnName)
        {
            var cell = GetCell(columnName);
            if (cell == null)
                throw new Exception("No column named " + columnName);
            if (cell is DatabaseField<long> l)
                return l.Current.Value;
            if (cell is DatabaseField<string> s)
                return StringToLongMapping.Instance[s.Current.Value ?? ""];
            throw new Exception("No column named " + columnName + " with type " + typeof(long) + " or " + typeof(string));
        }

        public DatabaseEntity Clone(DatabaseKey? newKey = null, bool? existInDatabase = null)
        {
            var fields = new Dictionary<ColumnFullName, IDatabaseField>(ColumnFullNameIgnoreCaseComparer.Instance);
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
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Flags]
    public enum EntityChangeFlags
    {
        ChangeCurrent = 1,
        ChangeOriginal = 2,
        ChangeAll = ChangeCurrent | ChangeOriginal
    }
}