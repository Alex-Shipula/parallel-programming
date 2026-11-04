using System.Diagnostics;
using System.Threading.Channels;

namespace ParallelLab2;

public sealed class PriorityTaskDispatcher
{
    private readonly Channel<DispatcherTask> _high;
    private readonly Channel<DispatcherTask> _normal;
    private readonly Channel<DispatcherTask> _low;
    private readonly Channel<bool> _signals;
    private readonly int _workerCount;
    private Task[] _workers = Array.Empty<Task>();

    private int _enqueued;
    private int _succeeded;
    private int _failed;
    private int _cancelled;

    public PriorityTaskDispatcher(int workerCount, int highCapacity, int normalCapacity, int lowCapacity)
    {
        _workerCount = workerCount;
        _high = CreateChannel<DispatcherTask>(highCapacity);
        _normal = CreateChannel<DispatcherTask>(normalCapacity);
        _low = CreateChannel<DispatcherTask>(lowCapacity);
        _signals = CreateChannel<bool>(highCapacity + normalCapacity + lowCapacity);
    }

    public int Enqueued => _enqueued;
    public int Succeeded => _succeeded;
    public int Failed => _failed;
    public int Cancelled => _cancelled;

    public Task Completion => Task.WhenAll(_workers);

    private static Channel<T> CreateChannel<T>(int capacity)
    {
        return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public void Start(CancellationToken cancellationToken)
    {
        _workers = Enumerable.Range(1, _workerCount)
            .Select(id => Task.Run(() => WorkerLoopAsync(id, cancellationToken)))
            .ToArray();
    }

    public async ValueTask EnqueueAsync(DispatcherTask task, CancellationToken cancellationToken = default)
    {
        ChannelWriter<DispatcherTask> writer = GetChannel(task.Priority).Writer;

        if (!writer.TryWrite(task))
        {
            ConsoleLog.Write($"[Backpressure] черга {task.Priority} заповнена, {task.Producer} очікує вільного місця (задача #{task.Id})");
            await writer.WriteAsync(task, cancellationToken);
        }

        Interlocked.Increment(ref _enqueued);
        ConsoleLog.Write($"[Додано     ] #{task.Id} \"{task.Name}\" | пріоритет {task.Priority} | від {task.Producer}");

        await _signals.Writer.WriteAsync(true, cancellationToken);
    }

    public void Complete()
    {
        _high.Writer.Complete();
        _normal.Writer.Complete();
        _low.Writer.Complete();
        _signals.Writer.Complete();
    }

    private Channel<DispatcherTask> GetChannel(TaskPriority priority)
    {
        return priority switch
        {
            TaskPriority.High => _high,
            TaskPriority.Normal => _normal,
            _ => _low
        };
    }

    private bool TryTakeNext(out DispatcherTask task)
    {
        if (_high.Reader.TryRead(out task!)) return true;
        if (_normal.Reader.TryRead(out task!)) return true;
        return _low.Reader.TryRead(out task!);
    }

    private async Task WorkerLoopAsync(int workerId, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (bool _ in _signals.Reader.ReadAllAsync(cancellationToken))
            {
                if (!TryTakeNext(out DispatcherTask task)) continue;
                await ExecuteAsync(workerId, task, cancellationToken);
            }

            ConsoleLog.Write($"[Worker-{workerId}] черги закриті та порожні, воркер завершує роботу");
        }
        catch (OperationCanceledException)
        {
            ConsoleLog.Write($"[Worker-{workerId}] зупинено через CancellationToken");
        }
    }

    private async Task ExecuteAsync(int workerId, DispatcherTask task, CancellationToken cancellationToken)
    {
        ConsoleLog.Write($"[Worker-{workerId}] ВЗЯВ     #{task.Id} \"{task.Name}\" | {task.Priority}");
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            await task.Work(cancellationToken);
            Interlocked.Increment(ref _succeeded);
            ConsoleLog.Write($"[Worker-{workerId}] ГОТОВО    #{task.Id} \"{task.Name}\" | {task.Priority} | {sw.ElapsedMilliseconds} мс");
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _cancelled);
            ConsoleLog.Write($"[Worker-{workerId}] СКАСОВАНО #{task.Id} \"{task.Name}\" | {task.Priority}");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failed);
            ConsoleLog.Write($"[Worker-{workerId}] ПОМИЛКА   #{task.Id} \"{task.Name}\" | {task.Priority} | {ex.Message}");
        }
    }
}