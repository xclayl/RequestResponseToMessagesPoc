using System.Threading.Tasks.Dataflow;

namespace AsyncThreadStatic.Caching;

public static class MyCacheStore
{
    public const int MaxThreads = 3;
    public static readonly IReadOnlyList<CacheTaskScheduler> TaskSchedulers
        = Enumerable.Range(0, MaxThreads)
            .Select(i => new CacheTaskScheduler(i))
            .ToList();
    public static readonly IReadOnlyList<Cache> ThreadCaches
        = Enumerable.Range(0, MaxThreads)
            .Select(i => new Cache())
            .ToList();

    public static CancellationToken Shutdown = CancellationToken.None;

    public static MyCacheAccessor Get()
    {
        return new MyCacheAccessor();
    }
}

public class CacheTaskScheduler : TaskScheduler
{
    private readonly int _myThreadId;
    private readonly BufferBlock<Task> Channel = new();
    private readonly Thread _thread;

    public CacheTaskScheduler(int myThreadId)
    {
        _myThreadId = myThreadId;
        _thread = new Thread(RunLoop);
        _thread.Start();
    }

    private void RunLoop()
    {
        try
        {
            while (true)
            {
                var work = Channel.Receive(MyCacheStore.Shutdown);
                // Console.WriteLine($"-- Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");
                TryExecuteTask(work);
                // Console.WriteLine($"-- Finished Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"-- Thread {_myThreadId} {Thread.CurrentThread.ManagedThreadId} exited");
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        if (Channel.TryReceiveAll(out var list))
        {
            foreach (var item in list)
            {
                Channel.Post(item);
            }

            return list;
        }

        return null;
    }

    protected override void QueueTask(Task task)
    {
        // Console.WriteLine($"-- QueueTask");
        Channel.Post(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }

}

public struct MyCacheAccessor
{
    
    private static int _counter;
    private int _myThreadId;
    
    public MyCacheAccessor()
    {
        _myThreadId = Interlocked.Increment(ref _counter) % MyCacheStore.MaxThreads;
    }

    public async ValueTask With<T>(CancellationToken t, T state, Action<T, Cache> action)
    {
        var myThreadId = _myThreadId;
        await Task.Factory.StartNew(() => action(state, MyCacheStore.ThreadCaches[myThreadId]),
            t, TaskCreationOptions.None, MyCacheStore.TaskSchedulers[_myThreadId]);
    }
}

public class Cache : Dictionary<string, string>
{
    
}