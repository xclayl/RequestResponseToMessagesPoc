using System.Collections.Concurrent;
using AsyncThreadStatic.Caching;
using Microsoft.AspNetCore.Mvc;

namespace AsyncThreadStatic.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class PerfController : ControllerBase
{

    // [ThreadStatic]
    private static ConcurrentDictionary<string, string>? _cache;


    // GET
    [HttpGet(Name = "GetPerf")]
    public async ValueTask<IEnumerable<string>> Get(CancellationToken t, [FromQuery] string q)
    {
        var cacheStore = MyCacheStore.Get();

        var list = new List<string>();

        await cacheStore.With(t, (list, q), GetFromCache);

        if (list.Any())
            return list;

        await Task.Delay(500, t);
        var newVal = $"{DateTimeOffset.Now}";
        
        await cacheStore.With(t, (list, q, newVal), PopulateCache);

        return list;
    }
    
    // GET
    [HttpGet("[action]")]
    public async ValueTask<IEnumerable<string>> Get2(CancellationToken t, [FromQuery] string q)
    {
        if (_cache == null)
            _cache = new();
        
        var cache = _cache;
        

        var list = new List<string>();

        if (cache.TryGetValue(q, out var val))
        {
            list.Add(val);
            return list;
        }


        await Task.Delay(500, t);
        
        
        var newVal = $"{DateTimeOffset.Now}";
       
        


        if (cache.TryAdd(q, newVal))
        {
            list.Add(newVal);
            return list;
        }
        
        
        if (cache.TryGetValue(q, out var val2))
        {
            list.Add(val2);
            return list;
        }
        
        list.Add(newVal);
        return list;
    }

    
    private static void GetFromCache((List<string> list, string data) p, Cache cache)
    {
        if (cache.TryGetValue(p.data, out var val))
        {
            p.list.Add(val);
        }
    }
    
    
    private static void PopulateCache((List<string> list, string key, string val) p, Cache cache)
    {
        GetFromCache((p.list, p.key), cache);
        
        if (p.list.Any())
            return;

        var val = $"{DateTimeOffset.Now}";
        cache.Add(p.key, val);
        p.list.Add(val);
    }
}