namespace WDE.MVVM.Observable
{
    public readonly struct CollectionEvent<T>
    {
        public readonly CollectionEventType Type;
        public readonly T Item;
        public readonly int Index;

        public CollectionEvent(CollectionEventType type, T item, int index)
        {
            Type = type;
            Item = item;
            Index = index;
        }
    }
    
    public enum CollectionEventType
    {
        Add,
        Remove
    }
}