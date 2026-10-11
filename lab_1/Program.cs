using ParallelLab1;

var storage = new DynamicArrayStorage<int>();
storage.CapacityExceeded += cap => Console.WriteLine($"Нова місткість: {cap}");
for (int i = 1; i <= 10; i++) storage.Add(i);
Console.WriteLine($"Count = {storage.Count}");

namespace ParallelLab1
{
	public sealed class DynamicArrayStorage<T>
	{
		private T[] _items = new T[4];

		public int Count { get; private set; }
		public event Action<int>? CapacityExceeded;

		public void Add(T item)
		{
			if (Count == _items.Length)
			{
				Array.Resize(ref _items, _items.Length * 2);
				CapacityExceeded?.Invoke(_items.Length);
			}

			_items[Count++] = item;
		}
	}
}