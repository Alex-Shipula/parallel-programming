namespace ParallelLab1;

public sealed class DynamicArrayStorageImplementation<T> : ICustomStorage<T> where T : IComparable<T>
{
    private readonly Func<T, bool>? _acceptFilter;
    private T[] _items;
    private int _count;
    private long _rejected;

    public event Action<T>? ItemAdded;
    public event Action<int>? CapacityExceeded;

    public DynamicArrayStorageImplementation(int initialCapacity = 4, Func<T, bool>? acceptFilter = null)
    {
        if (initialCapacity < 1) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _acceptFilter = acceptFilter;
        _items = new T[initialCapacity];
    }

    public int Count => _count;
    public long Rejected => _rejected;

    public void Add(T item)
    {
        if (_acceptFilter != null && !_acceptFilter(item))
        {
            _rejected++;
            return;
        }

        if (_count == _items.Length)
        {
            int newCapacity = _items.Length * 2;
            Array.Resize(ref _items, newCapacity);
            CapacityExceeded?.Invoke(newCapacity);
        }

        _items[_count] = item;
        _count++;
        ItemAdded?.Invoke(item);
    }

    public bool TryRemove(out T item)
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
        Array.Clear(_items, 0, _items.Length);
        _count = 0;
        _rejected = 0;
    }

    public bool TryGetExtremes(out T min, out T max)
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