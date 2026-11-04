using System.Text;
using ParallelLab2;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await RunPriorityBypassScenarioAsync(cts.Token);
            Console.WriteLine();
            await RunProducersScenarioAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Роботу скасовано користувачем.");
        }
    }

    private static async Task RunPriorityBypassScenarioAsync(CancellationToken ct)
    {
        Console.WriteLine("=== СЦЕНАРІЙ 1: високопріоритетна задача обходить чергу низькопріоритетних ===");

        var dispatcher = new PriorityTaskDispatcher(workerCount: 3, highCapacity: 5, normalCapacity: 5, lowCapacity: 10);
        dispatcher.Start(ct);

        for (int i = 1; i <= 9; i++)
        {
            await dispatcher.EnqueueAsync(CreateTask($"Розсилка #{i}", TaskPriority.Low, "Producer-Mailing", 800, false), ct);
        }

        await Task.Delay(300, ct);
        ConsoleLog.Write(">>> Надходить термінове повідомлення з пріоритетом High, черга Low ще не розібрана");
        await dispatcher.EnqueueAsync(CreateTask("Термінове повідомлення", TaskPriority.High, "Producer-Urgent", 200, false), ct);

        dispatcher.Complete();
        await dispatcher.Completion;

        PrintStatistics(dispatcher);
    }

    private static async Task RunProducersScenarioAsync(CancellationToken ct)
    {
        Console.WriteLine("=== СЦЕНАРІЙ 2: кілька продюсерів, обмежені черги та Graceful Shutdown ===");

        var dispatcher = new PriorityTaskDispatcher(workerCount: 3, highCapacity: 3, normalCapacity: 3, lowCapacity: 3);
        dispatcher.Start(ct);

        Task[] producers =
        {
            RunProducerAsync(dispatcher, "Producer-A", 12, 50, new[] { TaskPriority.Low, TaskPriority.Low, TaskPriority.Low, TaskPriority.Normal }, 1, false, ct),
            RunProducerAsync(dispatcher, "Producer-B", 9, 150, new[] { TaskPriority.Normal, TaskPriority.Low, TaskPriority.High }, 2, false, ct),
            RunProducerAsync(dispatcher, "Producer-C", 6, 400, new[] { TaskPriority.High, TaskPriority.Normal }, 3, true, ct)
        };

        await Task.WhenAll(producers);
        ConsoleLog.Write(">>> Усі продюсери завершили надходження, викликається Complete()");

        dispatcher.Complete();
        await dispatcher.Completion;

        PrintStatistics(dispatcher);
    }

    private static async Task RunProducerAsync(
        PriorityTaskDispatcher dispatcher,
        string producerName,
        int count,
        int delayMs,
        TaskPriority[] priorityPattern,
        int seed,
        bool withFailure,
        CancellationToken ct)
    {
        var random = new Random(seed);

        for (int i = 1; i <= count; i++)
        {
            TaskPriority priority = priorityPattern[(i - 1) % priorityPattern.Length];
            int workMs = random.Next(150, 401);
            bool shouldFail = withFailure && i == 3;

            await dispatcher.EnqueueAsync(CreateTask($"{producerName} повідомлення {i}", priority, producerName, workMs, shouldFail), ct);
            await Task.Delay(delayMs, ct);
        }
    }

    private static DispatcherTask CreateTask(string name, TaskPriority priority, string producer, int workMs, bool shouldFail)
    {
        return new DispatcherTask(name, priority, producer, async ct =>
        {
            await Task.Delay(workMs, ct);
            if (shouldFail)
            {
                throw new InvalidOperationException("поштовий сервер недоступний");
            }
        });
    }

    private static void PrintStatistics(PriorityTaskDispatcher dispatcher)
    {
        Console.WriteLine();
        Console.WriteLine($"Додано задач:   {dispatcher.Enqueued}");
        Console.WriteLine($"Успішно:        {dispatcher.Succeeded}");
        Console.WriteLine($"З помилкою:     {dispatcher.Failed}");
        Console.WriteLine($"Скасовано:      {dispatcher.Cancelled}");
        Console.WriteLine(dispatcher.Enqueued == dispatcher.Succeeded + dispatcher.Failed + dispatcher.Cancelled
            ? "РЕЗУЛЬТАТ: усі додані задачі опрацьовано, втрат немає."
            : "РЕЗУЛЬТАТ: кількість не збігається!");
    }
}