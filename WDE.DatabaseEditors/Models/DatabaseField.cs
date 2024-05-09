using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseField<T> : IDatabaseField where T : IComparable<T>
    {
        private readonly ColumnFullName columnName;

        public DatabaseField(ColumnFullName columnName, ValueHolder<T> current, ValueHolder<T>? original = null)
        {
            this.columnName = columnName;
            Current = current;
            Original = original ?? new ValueHolder<T>(current.Value, current.IsNull);
            Current.ValueChanged += OnValueChanged;
        }

        private void OnValueChanged(T? old, T? nnew, bool wasNull, bool isNull)
        {
            ValueChanged?.Invoke(columnName, val =>
            {
                if (val is ValueHolder<T> tVal)
                {
                    if (wasNull)
                        tVal.SetNull();
                    else
                        tVal.Value = old;
                }
            }, val =>
            {
                if (val is ValueHolder<T> tVal)
                {
                    if (isNull)
                        tVal.SetNull();
                    else
                        tVal.Value = nnew;
                }
            });
            OnChanged?.Invoke(new DatabaseFieldHistoryAction<T>(this, columnName, old, nnew, wasNull, isNull));
            OnPropertyChanged(nameof(IsModified));
        }

        public ValueHolder<T> Current { get; }
        public ValueHolder<T> Original { get; }

        public object? OriginalValue
        {
            get => Original.Value;
            set
            {
                if (value == null)
                    Original.Value = default;
                else
                {
                    if (value is T t)
                    {
                        Original.Value = t;
                    }
                    else if (Original is ValueHolder<long> originalLong)
                    {
                        try
                        {
                            originalLong.Value = Convert.ToInt64(value);
                        }
                        catch (Exception)
                        {
                            // cannot restore
                        }
                    }
                    else if (Original is ValueHolder<float> originalFloat)
                    {
                        try
                        {
                            originalFloat.Value = (float)Convert.ToDouble(value);
                        }
                        catch (Exception)
                        {
                            // cannot restore
                        }
                    }
                }
            }
        }
        public ColumnFullName FieldName => columnName;
        public bool IsModified => Current.IsNull != Original.IsNull || Comparer<T>.Default.Compare(Current.Value, Original.Value) != 0;
        public event Action<IHistoryAction>? OnChanged;
        public string ToQueryString()
        {
            if (Current.IsNull)
                return "NULL";
            if (typeof(T) == typeof(long))
                return Current.Value!.ToString()!;
            if (typeof(T) == typeof(float))
                return (Current.Value as float?)!.Value.ToString(CultureInfo.InvariantCulture)!;
            if (typeof(T) == typeof(string))
            {
                var value = Current.Value as string;
                return value!.ToSqlEscapeString();
            }

            throw new Exception("Unexpected value of type " + typeof(T));
        }

        public IValueHolder CurrentValue => Current;

        public IDatabaseField Clone()
        {
            var copy = new DatabaseField<T>(columnName, Current.Clone(), Original.Clone());
            return copy;
        }

        public object? Object => Current.IsNull ? null : Current.Value;
        // <ColumnName, <Undo>, <Redo>
        public event Action<ColumnFullName, Action<IValueHolder>, Action<IValueHolder>>? ValueChanged;

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public override string? ToString()
        {
            return Current.ToString();
        }

        protected bool Equals(DatabaseField<T> other)
        {
            return columnName == other.columnName && (Current.Value == null && other.Current.Value == null || Current.Value!.Equals(other.Current.Value));
        }

        public int CompareTo(IDatabaseField? b)
        {
            if (b is not DatabaseField<T> other)
                return 0;

            if (Current.Value == null)
                return -1;
            
            return Current.Value.CompareTo(other.Current.Value);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatabaseField<T>) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(columnName, Current.Value);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}