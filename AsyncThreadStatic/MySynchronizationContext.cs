using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace AsyncThreadStatic;

public class MySynchronizationContext : SynchronizationContext
{
    private static readonly IReadOnlyList<Thread> Threads;
    private static readonly IReadOnlyList<BufferBlock<(SendOrPostCallback d, object? state)>> ThreadChannels;
    public static CancellationToken Shutdown;

    private const int ThreadCount = 4;

    private static int ThreadPos;
    public readonly int MyThreadId;
    
    static MySynchronizationContext()
    {

        ThreadChannels = Enumerable.Range(0, ThreadCount)
            .Select(i => new BufferBlock<(SendOrPostCallback d, object? state)>())
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
                work.d(work.state);
                Console.WriteLine($"-- Finished Running on {threadId} {Thread.CurrentThread.ManagedThreadId}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"-- Thread {threadId} {Thread.CurrentThread.ManagedThreadId} exited");
    }

    public MySynchronizationContext()
    {
        MyThreadId = Interlocked.Increment(ref ThreadPos) % ThreadCount;
    }
    
    public override void Post(SendOrPostCallback d, object? state)
    {
        Console.WriteLine($"-- Posting");
        ThreadChannels[MyThreadId].Post((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        Console.WriteLine($"-- Sending");
        ThreadChannels[MyThreadId].Post((d, state));
    }

    public override SynchronizationContext CreateCopy() => this;
}