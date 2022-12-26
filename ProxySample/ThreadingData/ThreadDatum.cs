namespace ProxySample.ThreadingData;

public class ThreadDatum
{
    public int ConcurrentRequests;
    public int MaxConcurrentRequests = 50;

    public int CompletedRequests;
    public int QueueRejectionRequests;
    public int ConcurrentRejectionRequests;
    public int SuccessfulRequests;

    public long SucStartThreadSum;
    public long SucEndSum;
    public long SucEndThreadSum;
}