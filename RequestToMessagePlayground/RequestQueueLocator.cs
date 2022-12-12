namespace RequestToMessagePlayground;

public struct RequestQueueLocator
{
    public readonly short Thread;
    public readonly short Slot;

    public RequestQueueLocator(short thread, short slot)
    {
        Thread = thread;
        Slot = slot;
    }

    public static readonly RequestQueueLocator Invalid = new(-1, -1);

    public bool IsValid => Thread >= 0;

    public void Free()
    {
        if (IsValid)
            RequestQueue.Free(this);
    }
}