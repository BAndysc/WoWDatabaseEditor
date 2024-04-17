using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WDE.Common.Collections;

public class MultiDictionary<K, V> : ILookup<K, V>, IEnumerable<KeyValuePair<K, V>> where K : notnull where V : notnull
{
    private Dictionary<K, object> dictionary = new();
    private int totalCount = 0;

    private class Grouping : List<V>, IGrouping<K, V>
    {
        public K Key { get; }

        public Grouping(K key, List<V> list) : base(list)
        {
            Key = key;
        }
    }

    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
    {
        IEnumerable<KeyValuePair<K, V>> Enumerate()
        {
            foreach (var pair in dictionary)
            {
                if (pair.Value is List<V> list)
                {
                    foreach (var v in list)
                        yield return new KeyValuePair<K, V>(pair.Key, v);
                }
                else if (pair.Value is V value)
                {
                    yield return new KeyValuePair<K, V>(pair.Key, value);
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }
        }
        return Enumerate().GetEnumerator();
    }

    public IEnumerator<IGrouping<K, V>> GetEnumerator()
    {
        IEnumerable<IGrouping<K, V>> Enumerate()
        {
            foreach (var pair in dictionary)
            {
                if (pair.Value is List<V> list)
                {
                    yield return new Grouping(pair.Key, list);
                }
                else if (pair.Value is V value)
                {
                    yield return new Grouping(pair.Key, new List<V> { value });
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }
        }
        return Enumerate().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(K key) => dictionary.ContainsKey(key);

    public void Add(KeyValuePair<K, V> item)
    {
        if (dictionary.TryGetValue(item.Key, out var obj))
        {
            if (obj is List<V> list)
            {
                if (!list.Contains(item.Value))
                {
                    list.Add(item.Value);
                    totalCount++;
                }
            }
            else if (obj is V value)
            {
                if (!value.Equals(item.Value))
                {
                    dictionary[item.Key] = new List<V> { value, item.Value };
                    totalCount++;
                }
            }
            else
            {
                throw new System.InvalidOperationException();
            }
        }
        else
        {
            dictionary[item.Key] = item.Value;
            totalCount++;
        }
    }

    public void Clear()
    {
        dictionary.Clear();
        totalCount = 0;
    }

    public bool Contains(KeyValuePair<K, V> item)
    {
        if (dictionary.TryGetValue(item.Key, out var obj))
        {
            if (obj is List<V> list)
            {
                return list.Contains(item.Value);
            }
            else if (obj is V value)
            {
                return value.Equals(item.Value);
            }
            else
            {
                throw new System.InvalidOperationException();
            }
        }

        return false;
    }

    public bool Remove(KeyValuePair<K, V> item)
    {
        if (dictionary.TryGetValue(item.Key, out var obj))
        {
            if (obj is List<V> list)
            {
                var removed = list.Remove(item.Value);
                if (removed)
                    totalCount--;
                if (list.Count == 0)
                    dictionary.Remove(item.Key);
                return removed;
            }
            else if (obj is V value)
            {
                if (value.Equals(item.Value))
                {
                    dictionary.Remove(item.Key);
                    totalCount--;
                    return true;
                }
            }
            else
            {
                throw new System.InvalidOperationException();
            }
        }

        return false;
    }

    public int Count => totalCount;
    public bool IsReadOnly => false;

    public void Add(K key, V value) => Add(new KeyValuePair<K, V>(key, value));

    public bool ContainsKey(K key)
    {
        return dictionary.ContainsKey(key);
    }

    public bool Remove(K key)
    {
        if (dictionary.TryGetValue(key, out var obj))
        {
            if (obj is List<V> list)
            {
                totalCount -= list.Count;
                dictionary.Remove(key);
                return true;
            }
            else if (obj is V value)
            {
                totalCount--;
                dictionary.Remove(key);
                return true;
            }
            else
            {
                throw new System.InvalidOperationException();
            }
        }

        return false;
    }

    public ICollection<K> Keys => dictionary.Keys;

    public ICollection<V> Values
    {
        get
        {
            IEnumerable<V> Enumerate()
            {
                foreach (var obj in dictionary.Values)
                {
                    if (obj is List<V> list)
                    {
                        foreach (var v in list)
                            yield return v;
                    }
                    else if (obj is V value)
                    {
                        yield return value;
                    }
                    else
                    {
                        throw new System.InvalidOperationException();
                    }
                }
            }
            return Enumerate().ToList();
        }
    }

    public IEnumerable<V> this[K key]
    {
        get => dictionary.TryGetValue(key, out var obj) switch
        {
            true => obj switch
            {
                List<V> list => list,
                V value => new List<V> { value },
                _ => throw new System.InvalidOperationException()
            },
            false => Enumerable.Empty<V>()
        };
    }
}