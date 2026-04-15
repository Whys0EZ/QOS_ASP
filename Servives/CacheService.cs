using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class CacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private string BuildKey(string key, string module = "data")
    {
        return $"QOS:{module}:{key}";
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> getData,
        int minutes = 10,
        string module = "data")
    {
        var cacheKey = BuildKey(key, module);

        try
        {
            var cacheData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cacheData))
            {
                _logger.LogInformation($"[CACHE HIT] {cacheKey}");
                return JsonSerializer.Deserialize<T>(cacheData)!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[CACHE ERROR - GET] {cacheKey}");
        }

        _logger.LogInformation($"[CACHE MISS] {cacheKey}");

        var data = await getData();

        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(data),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
                });

            _logger.LogInformation($"[CACHE SET] {cacheKey}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[CACHE ERROR - SET] {cacheKey}");
        }

        return data;
    }

    public async Task RemoveAsync(string key, string module = "data")
    {
        var cacheKey = BuildKey(key, module);

        try
        {
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation($"[CACHE REMOVE] {cacheKey}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[CACHE ERROR - REMOVE] {cacheKey}");
        }
    }
}