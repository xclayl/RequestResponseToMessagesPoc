using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace ProxySample.ThreadingData;

public class ThreadDataTaskScheduler //: TaskScheduler
{
    private readonly int _myThreadId;
    private const short MaxQueuedThreads = 10;
    private readonly BufferBlock<(short, Func<object>)> Channel = new(new DataflowBlockOptions()
    {
        BoundedCapacity = MaxQueuedThreads 
    });


    private readonly IReadOnlyList<BufferBlock<object?>> _slots = Enumerable.Range(0, MaxQueuedThreads).Select(i => new BufferBlock<object?>(new DataflowBlockOptions()
    {
        BoundedCapacity = 3
    })).ToArray();
    private readonly ConcurrentBag<short> _freeSlots = new ConcurrentBag<short>(Enumerable.Range(0, MaxQueuedThreads).Select(i => (short)i));
    // private readonly ConcurrentBag<short> _toFreeSlots = new();

    public ThreadDataTaskScheduler(int myThreadId)
    {
        _myThreadId = myThreadId;
        var thread = new Thread(RunLoop);
        thread.Start();
    }

    private void RunLoop()
    {
        try
        {
            while (true)
            {
                var work = Channel.Receive(ThreadDataStore.Shutdown);

                try
                {
                    object o = null;
                    try
                    {
                        // Console.WriteLine($"-- Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");
                        o = work.Item2();
                        // Console.WriteLine($"-- Finished Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");

                    }
                    // catch (Exception e)
                    // {
                    //     Console.WriteLine(e.Message);
                    //     Console.WriteLine(e.StackTrace);
                    // }
                    finally
                    {
                        _slots[work.Item1].Post(o);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"-- Thread {_myThreadId} {Thread.CurrentThread.ManagedThreadId} exited");
    }

    // protected IEnumerable<Action>? GetScheduledTasks()
    // {
    //     if (Channel.TryReceiveAll(out var list))
    //     {
    //         foreach (var item in list)
    //         {
    //             Channel.Post(item);
    //         }
    //
    //         return list;
    //     }
    //
    //     return null;
    // }

    public async ValueTask<object?> TryQueueTask(bool mustSucceed, Func<object> task)
    {
        // Console.WriteLine($"-- QueueTask");

        
        
        if (!mustSucceed)
        {
            if (Channel.Count > 2)
            {
                return null;
            }
        }
        
        if (_freeSlots.TryTake(out var slot))
        {
            try
            {
                if (Channel.Post((slot, task)))
                {
                    return await _slots[slot].ReceiveAsync();
                }
            }
            finally
            {
                _freeSlots.Add(slot);
            }
            // await Console.Out.WriteLineAsync("not posted to channel");
        }
        // else
        // {
        //     // await Console.Out.WriteLineAsync("not taken free slot");
        // }

        while (mustSucceed)
        {
            if (_freeSlots.TryTake(out slot))
            {
                try
                {
                    if (Channel.Post((slot, task)))
                    {
                        return await _slots[slot].ReceiveAsync();
                    }
                }
                finally
                {
                    _freeSlots.Add(slot);
                }
            }

            await Task.Delay(5);
        }

        

        return null;
    }

    // protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    // {
    //     return false;
    // }

}