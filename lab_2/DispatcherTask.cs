namespace ParallelLab2;

public sealed class DispatcherTask
{
    private static int _nextId;

    public DispatcherTask(string name, TaskPriority priority, string producer, Func<CancellationToken, Task> work)
    {
        Id = Interlocked.Increment(ref _nextId);
        Name = name;
        Priority = priority;
        Producer = producer;
        CreatedAt = DateTime.Now;
        Work = work;
    }

    public int Id { get; }
    public string Name { get; }
    public TaskPriority Priority { get; }
    public string Producer { get; }
    public DateTime CreatedAt { get; }
    public Func<CancellationToken, Task> Work { get; }
}