using System;
using System.Collections.Generic;
using System.Linq;

public class LockedList<T>
{
    private readonly List<T> _list = new();
    private readonly object _lock = new();

    // Count property
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }
    }

    // Add an item
    public void Add(T item)
    {
        lock (_lock)
        {
            _list.Add(item);
        }
    }

    // Add a range of items
    public void AddRange(IEnumerable<T> items)
    {
        lock (_lock)
        {
            _list.AddRange(items);
        }
    }

    // Remove an item
    public bool Remove(T item)
    {
        lock (_lock)
        {
            return _list.Remove(item);
        }
    }

    // Remove at a specific index
    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            _list.RemoveAt(index);
        }
    }

    // Clear the list
    public void Clear()
    {
        lock (_lock)
        {
            _list.Clear();
        }
    }

    // Contains
    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _list.Contains(item);
        }
    }

    // Find the index of an item
    public int IndexOf(T item)
    {
        lock (_lock)
        {
            return _list.IndexOf(item);
        }
    }

    // Accessor with index
    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                return _list[index];
            }
        }
        set
        {
            lock (_lock)
            {
                _list[index] = value;
            }
        }
    }

    // Safe enumeration
    public IEnumerable<T> SafeEnumerate()
    {
        lock (_lock)
        {
            return _list.ToList(); // Return a snapshot
        }
    }

    // LINQ Support: Where
    public IEnumerable<T> Where(Func<T, bool> predicate)
    {
        lock (_lock)
        {
            return _list.Where(predicate).ToList();
        }
    }

    // LINQ Support: FirstOrDefault
    public T FirstOrDefault(Func<T, bool> predicate)
    {
        lock (_lock)
        {
            return _list.FirstOrDefault(predicate);
        }
    }

    // LINQ Support: LastOrDefault
    public T LastOrDefault(Func<T, bool> predicate)
    {
        lock (_lock)
        {
            return _list.LastOrDefault(predicate);
        }
    }

    // LINQ Support: Skip
    public IEnumerable<T> Skip(int count)
    {
        lock (_lock)
        {
            return _list.Skip(count).ToList();
        }
    }

    // LINQ Support: Take
    public IEnumerable<T> Take(int count)
    {
        lock (_lock)
        {
            return _list.Take(count).ToList();
        }
    }

    // LINQ Support: OrderBy
    public IEnumerable<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        lock (_lock)
        {
            return _list.OrderBy(keySelector).ToList();
        }
    }

    // LINQ Support: ToList
    public List<T> ToList()
    {
        lock (_lock)
        {
            return _list.ToList();
        }
    }
}
