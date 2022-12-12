using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RequestToMessagePlayground;

public class RequestQueue
{
    private static readonly object Lock = new();
    private const short MaxThreads = 50;
    private static int PrevThreadId = -1;
    private static readonly RequestQueue?[] Queues = new RequestQueue[MaxThreads];

    
    [ThreadStatic] private static short MyThreadId;
    [ThreadStatic] private static bool MyThreadInitialised;

    private const short MaxSlotsPerThread = 500;
    private readonly IReadOnlyList<Channel<string>> _slots = Enumerable.Range(0, MaxSlotsPerThread).Select(i => Channel.CreateBounded<string>(100)).ToArray();
    private readonly Queue<short> _freeSlots = new Queue<short>(Enumerable.Range(0, MaxSlotsPerThread).Select(i => (short)i));
    private readonly ConcurrentBag<short> _toFreeSlots = new();

    public static RequestQueueLocator AcquireSlot()
    {
        if (!MyThreadInitialised)
        {
            Init();
        }

        var q = Queues[MyThreadId];

        while (q._toFreeSlots.TryTake(out short s))
        {
            q._freeSlots.Enqueue(s);
            // more cleanup?
        }
        
        if (q._freeSlots.TryDequeue(out var slot))
        {
            return new RequestQueueLocator(MyThreadId, slot);
        }

        return RequestQueueLocator.Invalid;
    }

    public static Channel<string> Get(RequestQueueLocator loc)
    {
        return Queues[loc.Thread]._slots[loc.Slot];
    }

    public static void Free(RequestQueueLocator loc)
    {
        if (MyThreadId == loc.Thread)
        {
            Queues[loc.Thread]._freeSlots.Enqueue(loc.Slot);
            // more cleanup?
        }
        else
            Queues[loc.Thread]._toFreeSlots.Add(loc.Slot);
    }

    private static void Init()
    {
        lock (Lock)
        {
            if (!MyThreadInitialised)
            {
                var threadId = Interlocked.Increment(ref PrevThreadId);
                if (threadId >= MaxThreads)
                    throw new ArgumentException("Too many threads");
                MyThreadId = (short)threadId;
                Queues[MyThreadId] = new RequestQueue();
                MyThreadInitialised = true;
            }
        }
    }
}