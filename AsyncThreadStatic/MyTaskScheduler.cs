using System.Linq.Expressions;
using System.Threading.Tasks.Dataflow;

namespace AsyncThreadStatic;

public class MyTaskScheduler : TaskScheduler
{
    private static readonly IReadOnlyList<Thread> Threads;
    private static readonly IReadOnlyList<BufferBlock<(Task, MyTaskScheduler)>> ThreadChannels;
    public static CancellationToken Shutdown;

    private const int ThreadCount = 4;

    private static int ThreadPos;
    public readonly int MyThreadId;
    
    
    static MyTaskScheduler()
    {

        ThreadChannels = Enumerable.Range(0, ThreadCount)
            .Select(i => new BufferBlock<(Task, MyTaskScheduler)>())
            .ToList();
        
        
        Threads = Enumerable.Range(0, ThreadCount)
            .Select(i =>
            {
                var t = new Thread(() => RunLoop(i));
                t.Start();
                return t;
            })
            .ToList();
    }

    
    private static void RunLoop(int threadId)
    {
        try
        {
            while (true)
            {
                var work = ThreadChannels[threadId].Receive(Shutdown);
                Console.WriteLine($"-- Running on {threadId} {Thread.CurrentThread.ManagedThreadId}");
                work.Item2.TryExecuteTask(work.Item1);
                Console.WriteLine($"-- Finished Running on {threadId} {Thread.CurrentThread.ManagedThreadId}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"-- Thread {threadId} {Thread.CurrentThread.ManagedThreadId} exited");
    }
    
    public MyTaskScheduler()
    {
        MyThreadId = Interlocked.Increment(ref ThreadPos) % ThreadCount;
    }
    
    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        if (ThreadChannels[MyThreadId].TryReceiveAll(out var list))
        {
            foreach (var item in list)
            {
                ThreadChannels[MyThreadId].Post(item);
            }

            return list.Select(l => l.Item1);
        }

        return null;
    }

    protected override void QueueTask(Task task)
    {   
        Console.WriteLine($"-- QueueTask");
        ThreadChannels[MyThreadId].Post((task, this));
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }
}