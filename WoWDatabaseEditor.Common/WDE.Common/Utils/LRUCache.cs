using System.Collections.Generic;

namespace WDE.Common.Utils;

/// <summary>
/// Implements an least recently used cache of fixed size.
/// </summary>
public class LRUCache<TKey, TValue> where TValue : class where TKey : notnull
{
    private readonly int capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue?)>> map;
    private readonly LinkedList<(TKey, TValue?)> list;
    
    public LRUCache(int capacity)
    {
        this.capacity = capacity;
        map = new Dictionary<TKey, LinkedListNode<(TKey, TValue?)>>(capacity);
        list = new LinkedList<(TKey, TValue?)>();
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (map.TryGetValue(key, out var node))
        {
            value = node.Value.Item2;
            list.Remove(node);
            list.AddLast(node);
            return true;
        }

        value = null;
        return false;
    }
    
    public TValue? this[TKey key]
    {
        get
        {
            if (map.TryGetValue(key, out var node))
            {
                list.Remove(node);
                list.AddLast(node);
                return node.Value.Item2;
            }
            return default;
        }
        set
        {
            if (map.TryGetValue(key, out var node))
            {
                list.Remove(node);
                list.AddLast(node);
                node.Value = (key, value);
            }
            else
            {
                if (map.Count == capacity)
                {
                    map.Remove(list.First!.Value.Item1);
                    list.RemoveFirst();
                }
                var newNode = new LinkedListNode<(TKey, TValue?)>((key, value));
                map.Add(key, newNode);
                list.AddLast(newNode);
            }
        }
    }
}