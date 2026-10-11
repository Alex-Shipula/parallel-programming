namespace ParallelLab1;

public sealed class DynamicArrayStorageImplementation<T> : ICustomStorage<T> where T : IComparable<T>
{
    private readonly object _syncRoot = new();
    private readonly bool _enableLock;
    private readonly Func<T, bool>? _acceptFilter;

    private T[] _items;
    private int _count;
    private long _rejected;

    public event Action<T>? ItemAdded;

    public event Action<int>? CapacityExceeded;

    public DynamicArrayStorageImplementation(bool enableLock, int initialCapacity = 4, Func<T, bool>? acceptFilter = null)
    {
        if (initialCapacity < 1) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _enableLock = enableLock;
        _acceptFilter = acceptFilter;
        _items = new T[initialCapacity];
    }

    public bool LockEnabled => _enableLock;

    public int Count
    {
        get
        {
            if (!_enableLock) return _count;
            lock (_syncRoot) return _count;
        }
    }

    public long Rejected
    {
        get
        {
            if (!_enableLock) return _rejected;
            lock (_syncRoot) return _rejected;
        }
    }

    public void Add(T item)
    {
        bool added;
        int newCapacity;

        if (_enableLock)
        {
            lock (_syncRoot)
            {
                added = AddCore(item, out newCapacity);
            }
        }
        else
        {
            added = AddCore(item, out newCapacity);
        }

        if (newCapacity > 0) CapacityExceeded?.Invoke(newCapacity);
        if (added) ItemAdded?.Invoke(item);
    }

    private bool AddCore(T item, out int newCapacity)
    {
        newCapacity = 0;

        if (_acceptFilter != null && !_acceptFilter(item))
        {
            _rejected++;
            return false;
        }

        if (_count == _items.Length)       
        {
            newCapacity = _items.Length * 2;
            Array.Resize(ref _items, newCapacity);
        }

        _items[_count] = item;
        _count++;     
        return true;
    }

    public bool TryRemove(out T item)
    {
        if (_enableLock)
        {
            lock (_syncRoot) return RemoveCore(out item);
        }
        return RemoveCore(out item);
    }

    private bool RemoveCore(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }
        _count--;
        item = _items[_count];
        _items[_count] = default!;
        return true;
    }

    public void Clear()
    {
        if (_enableLock)
        {
            lock (_syncRoot) ClearCore();
            return;
        }
        ClearCore();
    }

    private void ClearCore()
    {
        Array.Clear(_items, 0, _items.Length);
        _count = 0;
        _rejected = 0;
    }

    public bool TryGetExtremes(out T min, out T max)
    {
        if (_enableLock)
        {
            lock (_syncRoot) return ExtremesCore(out min, out max);
        }
        return ExtremesCore(out min, out max);
    }

    private bool ExtremesCore(out T min, out T max)
    {
        min = max = default!;
        int n = _count;
        if (n == 0) return false;

        min = max = _items[0];
        for (int i = 1; i < n; i++)
        {
            T v = _items[i];
            if (v.CompareTo(min) < 0) min = v;
            if (v.CompareTo(max) > 0) max = v;
        }
        return true;
    }
}