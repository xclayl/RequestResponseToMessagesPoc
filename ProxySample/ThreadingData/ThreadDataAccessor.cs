namespace ProxySample.ThreadingData;

public readonly struct ThreadDataAccessor
{
    
    [ThreadStatic]
    private static int _counter;
    private readonly int _myThreadId;
    
    private readonly IReadOnlyList<ThreadDataTaskScheduler> _taskSchedulers;
    private readonly IReadOnlyList<ThreadDatum> _threadData;
    
    
    
    public ThreadDataAccessor(int threadCount, IReadOnlyList<ThreadDataTaskScheduler> taskSchedulers, IReadOnlyList<ThreadDatum> threadData)
    {
        _myThreadId = _counter++ % threadCount;
        if (_counter > 1_000_000_000)
            _counter = 0;
        _threadData = threadData;
        _taskSchedulers = taskSchedulers;
    }
    public ThreadDataAccessor(IReadOnlyList<ThreadDataTaskScheduler> taskSchedulers, IReadOnlyList<ThreadDatum> threadData, int threadId)
    {
        _myThreadId = threadId;
        _threadData = threadData;
        _taskSchedulers = taskSchedulers;
    }

    private class Box<T> where T:struct
    {
        public T Val;
    }
    
    public async ValueTask<TR> WithResult<TR>(CancellationToken t, Func<ThreadDatum, TR> action) where TR:struct
    {
        var threadData = _threadData;
        var myThreadId = _myThreadId;

        var val = await _taskSchedulers[_myThreadId].TryQueueTask(true, t,  () =>
        {
            TR v = action(threadData[myThreadId]);
            return new Box<TR>() { Val = v };
        }) as Box<TR>;
        if (val == null)
        {
            throw new Exception("This should never happen");
        }

        return val.Val;
    }
    public async ValueTask<TR?> TryWithResult<TR>(CancellationToken t, Func<ThreadDatum, TR> action) where TR:struct
    {
        var threadData = _threadData;
        var myThreadId = _myThreadId;
        

        var val = await _taskSchedulers[_myThreadId].TryQueueTask(false, t, () =>
        {
            TR v = action(threadData[myThreadId]);
            return new Box<TR>() { Val = v };
        }) as Box<TR>;
        if (val == null)
        {
            return null;
        }

        return val.Val;
    }
   
    public async ValueTask With<T>(CancellationToken t, T state, Action<T, ThreadDatum> action) where T:struct 
    {
        var threadData = _threadData;
        var myThreadId = _myThreadId;
        
        
        var val = await _taskSchedulers[_myThreadId].TryQueueTask(true, t,  () =>
        {
            action(state, threadData[myThreadId]);
            return new Box<int>() { Val = 0 };
        }) as Box<int>;
        if (val == null)
        {
            throw new Exception("This should never happen");
        }


    }
    
    public async ValueTask With(CancellationToken t, Action<ThreadDatum> action)  
    {
        var threadData = _threadData;
        var myThreadId = _myThreadId;
        
        
        var val = await _taskSchedulers[_myThreadId].TryQueueTask(true, t,  () =>
        {
            action(threadData[myThreadId]);
            return new Box<int>() { Val = 0 };
        }) as Box<int>;
        if (val == null)
        {
            throw new Exception("This should never happen");
        }


    }
}