using System;

namespace WDE.MVVM
{
    public interface INotifyPropertyValueChanged
    {
        event PropertyValueChangedEventHandler PropertyValueChanged;   
    }
    
    public delegate void PropertyValueChangedEventHandler(object sender, PropertyValueChangedEventArgs e);

    public class PropertyFriendlyNameAttribute : Attribute
    {
        public string FriendlyName { get; }

        public PropertyFriendlyNameAttribute(string friendlyName)
        {
            FriendlyName = friendlyName;
        }
    }
    
    public abstract class PropertyValueChangedEventArgs : EventArgs
    {
        public PropertyValueChangedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }
        
        public abstract System.Type Type { get; }

        public abstract TypedPropertyValueChangedEventArgs<T>? ToTyped<T>();
        
        public abstract object? Old { get; }
        
        public abstract object? New { get; }

        public virtual string PropertyName { get; }
    }

    public class TypedPropertyValueChangedEventArgs<T> : PropertyValueChangedEventArgs
    {
        public TypedPropertyValueChangedEventArgs(string propertyName, T? old, T? @new) : base(propertyName)
        {
            TypedOld = old;
            TypedNew = @new;
        }

        public T? TypedOld { get; }
        public T? TypedNew { get; }

        public override Type Type => typeof(T);

        public override TypedPropertyValueChangedEventArgs<T1>? ToTyped<T1>()
        {
            if (typeof(T1) == Type)
                return (TypedPropertyValueChangedEventArgs<T1>)(object)this;
            return null;
        }

        public override object? Old => TypedOld;
        public override object? New => TypedNew;
    }
}