using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ParallelLab1;

internal static class Program
{
    // Поточне сховище, яке «бачить» фоновий монітор
    private static volatile DynamicArrayStorageImplementation<int>? _current;

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        StartMonitor();

        Console.WriteLine("=== ДЕМОНСТРАЦІЯ 1: стан гонки БЕЗ lock ===");
        RunRaceDemo(enableLock: false);

        Console.WriteLine("\n=== ДЕМОНСТРАЦІЯ 2: те саме З lock ===");
        RunRaceDemo(enableLock: true);

        Console.WriteLine("\nГоловний потік завершується. Потік-монітор (IsBackground = true) " +
                          "буде примусово зупинений середовищем виконання, хоча в ньому while(true).");
    }


    //////////////////////////// Фоновий потік-монітор /////////////////////////////

    private static void StartMonitor()
    {
        var monitor = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(300);                      // 200–500 мс
                var s = _current;
                if (s is null) continue;

                try
                {
                    string ext = s.TryGetExtremes(out int min, out int max)
                        ? $"min={min}, max={max}"
                        : "порожньо";
                    Console.WriteLine($"   [Monitor] Count={s.Count,10:N0}  {ext}");
                }
                catch (Exception ex)   // без lock монітор теж може зловити пошкоджений стан
                {
                    Console.WriteLine($"   [Monitor] помилка читання: {ex.GetType().Name}");
                }
            }
        })
        {
            Name = "Monitor",
            IsBackground = true,                        // не заважає завершенню застосунку
            Priority = ThreadPriority.BelowNormal
        };
        monitor.Start();
    }

    //////////////////////// Демонстрація стану гонки: 3 продюсери по N елементів //////////////////////////////

    private static void RunRaceDemo(bool enableLock)
    {
        const int Producers = 3;
        const int PerProducer = 1_000_000;
        int expected = Producers * PerProducer;

        var storage = new DynamicArrayStorageImplementation<int>(enableLock, initialCapacity: 4);
        int specialItems = 0, resizes = 0;
        var errors = new ConcurrentDictionary<string, int>();

        // Підписка на події (делегати)
        storage.ItemAdded += x => { if (x >= 990) Interlocked.Increment(ref specialItems); };
        storage.CapacityExceeded += _ => Interlocked.Increment(ref resizes);

        _current = storage;

        var threads = new Thread[Producers];
        for (int i = 0; i < Producers; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int k = 0; k < PerProducer; k++)
                {
                    try
                    {
                        storage.Add(Random.Shared.Next(1000));
                    }
                    catch (Exception ex)
                    {
                        errors.AddOrUpdate(ex.GetType().Name, 1, (_, c) => c + 1);
                    }
                }
            })
            { Name = $"Producer-{i + 1}" };
        }

        var sw = Stopwatch.StartNew();
        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();          // чекаємо завершення всіх продюсерів
        sw.Stop();

        int actual = storage.Count;
        int errorsTotal = errors.Values.Sum();

        Console.WriteLine($"Режим:                {(enableLock ? "З lock" : "БЕЗ lock")}");
        Console.WriteLine($"Час:                  {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"Очікувано елементів:  {expected:N0}");
        Console.WriteLine($"Фактично в колекції:  {actual:N0}");
        Console.WriteLine($"Втрачено записів:     {expected - actual - errorsTotal:N0} (мовчки) + {errorsTotal:N0} (з винятком)");
        Console.WriteLine($"Винятки:              {(errors.IsEmpty ? "немає" : string.Join(", ", errors.Select(e => $"{e.Key} x{e.Value}")))}");
        Console.WriteLine($"Подій CapacityExceeded: {resizes}");
        Console.WriteLine($"Подій ItemAdded (>=990): {specialItems:N0}");
        Console.WriteLine(actual == expected && errorsTotal == 0
            ? "РЕЗУЛЬТАТ: цілісність збережена, кількість збігається."
            : "РЕЗУЛЬТАТ: ПОРУШЕННЯ ЦІЛІСНОСТІ — стан гонки!");

        Thread.Sleep(400);   // щоб монітор встиг вивести фінальний стан
    }
}