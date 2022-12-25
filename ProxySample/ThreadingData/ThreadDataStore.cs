namespace ProxySample.ThreadingData;

public static class ThreadDataStore
{

    private static readonly int ThreadCount;
    private static readonly IReadOnlyList<ThreadDataTaskScheduler> TaskSchedulers;
    private static readonly IReadOnlyList<ThreadDatum> ThreadData;

    public static IEnumerable<ThreadDataAccessor> Threads =>
        Enumerable.Range(0, ThreadCount).Select(i => new ThreadDataAccessor(TaskSchedulers, ThreadData, i));

    static ThreadDataStore()
    {
        ThreadCount = Environment.ProcessorCount;
        Console.WriteLine($"{ThreadCount} threads");

        TaskSchedulers
            = Enumerable.Range(0, ThreadCount)
                .Select(i => new ThreadDataTaskScheduler(i))
                .ToList();
        ThreadData
            = Enumerable.Range(0, ThreadCount)
                .Select(i => new ThreadDatum())
                .ToList();
    }

    public static CancellationToken Shutdown = CancellationToken.None;

    public static ThreadDataAccessor Get()
    {
        return new ThreadDataAccessor(ThreadCount, TaskSchedulers, ThreadData);
    }
}