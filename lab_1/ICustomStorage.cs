namespace ParallelLab1;

public interface ICustomStorage<T>
{
    void Add(T item);
    bool TryRemove(out T item);
    int Count { get; }
    void Clear();
}