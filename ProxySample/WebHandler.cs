using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ProxySample.ThreadingData;

namespace ProxySample;

public static class WebHandler
{
    
    public static async Task<IResult> Post(HttpRequest req, Stream body)
    {
        return Results.Ok();
    }

    private struct Timings
    {
        public long Start;
        public long StartThread;
        public long End;
    }
    
    
    public static async Task<IResult> Get(HttpRequest req, CancellationToken t)
    {

        var timings = new Timings
        {
            Start = Environment.TickCount64
        };
        
        var dataStore = ThreadDataStore.Get();

        
        //return Results.Content("<html><body><h1>rejected</h1></body></html>", new MediaTypeHeaderValue("text/html"));

        var info = await dataStore.TryWithResult(t,  data =>
        {
            var startThread = Environment.TickCount64;
            bool overloaded;
            if (data.ConcurrentRequests >= data.MaxConcurrentRequests)
            {
                data.CompletedRequests++;
                data.RejectedRequests++;
                overloaded = true;
                return (overloaded, startThread);
            }

            data.ConcurrentRequests++;
            overloaded = false;
            return (overloaded, startThread);
        });

        if (info == null)
        {
            await dataStore.With(0, (rTimings, data) =>
            {
                data.CompletedRequests++;
                data.RejectedRequests++;
            });
            
            return Results.Content("<html><body><h1>rejected</h1></body></html>", new MediaTypeHeaderValue("text/html"));
        }
        
        timings.StartThread = info.Value.startThread;
        
        if (info.Value.overloaded)
            return Results.Content("<html><body><h1>rejected</h1></body></html>", new MediaTypeHeaderValue("text/html"));

        try
        {
            await Task.Delay(50, t); // do something clever

            
            return Results.Content("<html><body><h1>success</h1></body></html>", new MediaTypeHeaderValue("text/html")); 
        }
        finally
        {
            timings.End = Environment.TickCount64;
            
            await dataStore.With(timings, (rTimings, data) =>
            {
                data.CompletedRequests++;
                data.ConcurrentRequests--;
                var endInThread = Environment.TickCount64;
                data.SuccessfulRequests++;
                data.SucStartThreadSum += (rTimings.StartThread - rTimings.Start);
                data.SucEndSum += (rTimings.End - rTimings.Start);
                data.SucEndThreadSum += (endInThread - rTimings.Start);

            });
        }

        
    }


    private struct GetStatsData
    {
        public int CompletedRequests;
        public int RejectedRequests;
        public int SuccessfulRequests;
        public long SucStartThreadSum;
        public long SucEndSum;
        public long SucEndThreadSum;
    }
    
    public static async Task<IResult> GetStats(CancellationToken t)
    {
        var tasks = ThreadDataStore.Threads.Select(async th => await th.WithResult(t, data =>
        {
            return new GetStatsData
            {
                CompletedRequests = data.CompletedRequests,
                RejectedRequests = data.RejectedRequests,
                SuccessfulRequests = data.SuccessfulRequests,
                SucStartThreadSum = data.SucStartThreadSum,
                SucEndSum = data.SucEndSum,
                SucEndThreadSum = data.SucEndThreadSum,
            };
        })).ToArray();
        
        var d = await Task.WhenAll(tasks);

        var totalRequests = d.Sum(e => e.CompletedRequests);
        var totalRejected = d.Sum(e => e.RejectedRequests);
        var totalSuccessful = d.Sum(e => e.SuccessfulRequests);
        var sucStartThreadSum = d.Sum(e => e.SucStartThreadSum);
        var sucEndSum = d.Sum(e => e.SucEndSum);
        var sucEndThreadSum = d.Sum(e => e.SucEndThreadSum);

        var sucStartThreadAvg = 0M;
        var sucEndAvg = 0M;
        var sucEndThreadAvg = 0M;

        if (totalSuccessful > 0)
        {
            sucStartThreadAvg = sucStartThreadSum / (decimal)totalSuccessful;
            sucEndAvg = sucEndSum / (decimal)totalSuccessful;
            sucEndThreadAvg = sucEndThreadSum / (decimal)totalSuccessful;
        }
        
        return Results.Content($@"<html><body><h1>stats</h1>
            <div>Total {totalRequests}</div>
            <div>Rej {totalRejected}</div>
            <div>totalSuccessful {totalSuccessful}</div>
            <div>sucStartThreadAvg {sucStartThreadAvg}</div>
            <div>sucEndAvg {sucEndAvg}</div>
            <div>sucEndThreadAvg {sucEndThreadAvg}</div>
        </body></html>", new MediaTypeHeaderValue("text/html"));
    }
}