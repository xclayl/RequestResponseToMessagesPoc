namespace ProxySample.ThreadingData;

public class ThreadDatum
{
    public int ConcurrentRequests;
    public int MaxConcurrentRequests = 10;

    public int CompletedRequests;
    public int RejectedRequests;
    public int SuccessfulRequests;

    public long SucStartThreadSum;
    public long SucEndSum;
    public long SucEndThreadSum;
}