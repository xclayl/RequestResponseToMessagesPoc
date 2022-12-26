using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace ProxySample.ThreadingData;

public class ThreadDataTaskScheduler //: TaskScheduler
{
    private readonly int _myThreadId;
    private const short MaxQueuedThreads = 50;
    private readonly BufferBlock<(short, Func<object>)> Channel = new(new DataflowBlockOptions()
    {
        BoundedCapacity = MaxQueuedThreads 
    });


    private readonly IReadOnlyList<BufferBlock<(object?, Exception?)>> _slots = Enumerable.Range(0, MaxQueuedThreads).Select(i => new BufferBlock<(object?, Exception?)>(new DataflowBlockOptions()
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
                    // Console.WriteLine($"-- Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");
                    var o = work.Item2();
                    // Console.WriteLine($"-- Finished Running on {_myThreadId} {Thread.CurrentThread.ManagedThreadId}");

                    _slots[work.Item1].Post((o, null));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    
                    _slots[work.Item1].Post((null, e));
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"-- Thread {_myThreadId} {Thread.CurrentThread.ManagedThreadId} exited");
    }


    public async ValueTask<object?> TryQueueTask(bool mustSucceed, CancellationToken t, Func<object> task)
    {
        // Console.WriteLine($"-- QueueTask");

        
        
        if (!mustSucceed)
        {
            if (Channel.Count > 2)
            {
                return null;
            }
        }

        if (t.IsCancellationRequested)
        {
            if (mustSucceed)
                throw new OperationCanceledException();
            
            return null;
        }
        
        if (_freeSlots.TryTake(out var slot))
        {
            try
            {
                if (t.IsCancellationRequested)
                {
                    if (mustSucceed)
                        throw new OperationCanceledException();
            
                    return null;
                }


                _slots[slot].TryReceiveAll(out _);
                
                if (Channel.Post((slot, task)))
                {
                    var r = await _slots[slot].ReceiveAsync(t);
                    if (r.Item2 != null)
                        throw new AggregateException(r.Item2);
                    return r.Item1;
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

            if (t.IsCancellationRequested)
            {
                if (mustSucceed)
                    throw new OperationCanceledException();
            
                return null;
            }
            
            if (_freeSlots.TryTake(out slot))
            {
                try
                {
                    if (Channel.Post((slot, task)))
                    {
                        var r = await _slots[slot].ReceiveAsync();
                        if (r.Item2 != null)
                            throw new AggregateException(r.Item2);
                        return r.Item1;
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


}