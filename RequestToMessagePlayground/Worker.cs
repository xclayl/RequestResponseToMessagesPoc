using System.Threading.Channels;

namespace RequestToMessagePlayground;

public class Worker
{
    private static readonly Channel<RequestQueueLocator> MyChannel = Channel.CreateBounded<RequestQueueLocator>(100);

    static Worker()
    {
        foreach (var i in Enumerable.Range(0, 5))
        {
            var w = new Worker();
            var t = new Thread(() => w.ConsumerLoop());
            t.Start();
        }
    }

    public static bool Enqueue(RequestQueueLocator loc)
    {
        return MyChannel.Writer.TryWrite(loc);
    }

    private Worker() {}

    private void ConsumerLoop()
    {
        while (true)
        {
            
            var loc = MyChannel.Reader.ReadAsync().AsTask().Result;

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));


            RequestQueue.Get(loc).Writer.WriteAsync("answer").AsTask().Wait();


        }

    }
}