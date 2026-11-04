using System.Diagnostics;

namespace ParallelLab2;

public static class ConsoleLog
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    public static void Write(string message)
    {
        Console.WriteLine($"[{Clock.ElapsedMilliseconds,6} мс] {message}");
    }
}