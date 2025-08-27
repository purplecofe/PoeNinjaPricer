using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore;
using Newtonsoft.Json;
using PoeNinjaPricer.Models;

namespace PoeNinjaPricer.Services;

public class PriceCacheService
{
    private readonly string _cacheFilePath;
    private List<CurrencyPrice> _cachedPrices = new();
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private readonly object _lockObject = new();
    private readonly Func<bool> _isDebugEnabled;

    public DateTime LastUpdateTime => _lastUpdateTime;
    public bool HasValidCache => _cachedPrices.Count > 0 && _lastUpdateTime > DateTime.MinValue;

    public PriceCacheService(string pluginDirectory, Func<bool> isDebugEnabled = null)
    {
        _cacheFilePath = Path.Combine(pluginDirectory, "price_cache.json");
        _isDebugEnabled = isDebugEnabled;
        LoadCacheFromFile();
    }

    /// <summary>
    /// 獲取快取的價格資料
    /// </summary>
    public List<CurrencyPrice> GetCachedPrices()
    {
        lock (_lockObject)
        {
            return _cachedPrices.ToList(); // 返回副本避免併發修改
        }
    }

    /// <summary>
    /// 更新快取的價格資料
    /// </summary>
    public void UpdateCache(List<CurrencyPrice> prices)
    {
        if (prices == null || prices.Count == 0) return;

        lock (_lockObject)
        {
            _cachedPrices = prices.ToList();
            _lastUpdateTime = DateTime.Now;
            SaveCacheToFile();
        }

        if (_isDebugEnabled?.Invoke() == true)
            DebugWindow.LogMsg($"PoeNinjaPricer: Cache updated with {prices.Count} items at {_lastUpdateTime:HH:mm:ss}");
    }

    /// <summary>
    /// Check if cache has expired
    /// </summary>
    public bool IsCacheExpired(int expireMinutes)
    {
        return DateTime.Now - _lastUpdateTime > TimeSpan.FromMinutes(expireMinutes);
    }

    /// <summary>
    /// 搜尋快取中的通貨
    /// </summary>
    public List<CurrencyPrice> SearchPrices(string searchTerm, bool includeFragments = true)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetFilteredPrices(includeFragments);
        }

        lock (_lockObject)
        {
            return _cachedPrices
                .Where(p => p.Name.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()) &&
                           (includeFragments || p.Category != CurrencyCategory.Fragments))
                .OrderBy(p => p.Name)
                .ToList();
        }
    }

    /// <summary>
    /// 獲取過濾後的價格清單
    /// </summary>
    public List<CurrencyPrice> GetFilteredPrices(bool includeFragments = true)
    {
        lock (_lockObject)
        {
            return _cachedPrices
                .Where(p => includeFragments || p.Category != CurrencyCategory.Fragments)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToList();
        }
    }

    /// <summary>
    /// 清除快取
    /// </summary>
    public void ClearCache()
    {
        lock (_lockObject)
        {
            _cachedPrices.Clear();
            _lastUpdateTime = DateTime.MinValue;
            
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    File.Delete(_cacheFilePath);
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"PoeNinjaPricer: Failed to delete cache file - {ex.Message}");
            }
        }
    }

    private void SaveCacheToFile()
    {
        try
        {
            var cacheData = new CacheData
            {
                Prices = _cachedPrices,
                LastUpdateTime = _lastUpdateTime
            };

            var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
            
            // 確保目錄存在
            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Failed to save cache to file - {ex.Message}");
        }
    }

    private void LoadCacheFromFile()
    {
        try
        {
            if (!File.Exists(_cacheFilePath)) return;

            var json = File.ReadAllText(_cacheFilePath);
            var cacheData = JsonConvert.DeserializeObject<CacheData>(json);

            if (cacheData?.Prices != null)
            {
                _cachedPrices = cacheData.Prices;
                _lastUpdateTime = cacheData.LastUpdateTime;
                
                if (_isDebugEnabled?.Invoke() == true)
                    DebugWindow.LogMsg($"PoeNinjaPricer: Loaded {_cachedPrices.Count} items from cache (last update: {_lastUpdateTime:yyyy-MM-dd HH:mm:ss})");
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Failed to load cache from file - {ex.Message}");
            // 清除損壞的快取檔案
            try
            {
                File.Delete(_cacheFilePath);
            }
            catch
            {
                // 忽略刪除錯誤
            }
        }
    }

    private class CacheData
    {
        public List<CurrencyPrice> Prices { get; set; } = new();
        public DateTime LastUpdateTime { get; set; }
    }
}