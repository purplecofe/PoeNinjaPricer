using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExileCore;
using Newtonsoft.Json;
using PoeNinjaPricer.Models;

namespace PoeNinjaPricer.Services;

public class PoeNinjaApiService : IDisposable
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string CurrencyBaseUrl = "https://poe.ninja/api/data/currencyoverview";
    private const string ItemBaseUrl = "https://poe.ninja/api/data/itemoverview";
    private readonly string _leagueName;
    private readonly Func<bool> _isDebugEnabled;

    public PoeNinjaApiService(string leagueName, Func<bool> isDebugEnabled = null)
    {
        _leagueName = leagueName;
        _isDebugEnabled = isDebugEnabled;
    }

    /// <summary>
    /// 獲取通貨價格資料
    /// </summary>
    public async Task<List<CurrencyPrice>> GetCurrencyPricesAsync()
    {
        var prices = new List<CurrencyPrice>();
        
        try
        {
            // 獲取基本通貨
            var currencyData = await GetCurrencyDataAsync("Currency");
            if (currencyData != null)
            {
                prices.AddRange(ConvertToCurrencyPrices(currencyData));
            }

            // 獲取碎片
            var fragmentData = await GetCurrencyDataAsync("Fragment");
            if (fragmentData != null)
            {
                prices.AddRange(ConvertToCurrencyPrices(fragmentData));
            }

            // 獲取聖甲蟲
            var scarabData = await GetScarabDataAsync();
            if (scarabData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(scarabData));
            }

            // 獲取油瓶
            var oilData = await GetOilDataAsync();
            if (oilData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(oilData));
            }

            // 獲取化石
            var fossilData = await GetFossilDataAsync();
            if (fossilData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(fossilData));
            }

            // 獲取精髓
            var essenceData = await GetEssenceDataAsync();
            if (essenceData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(essenceData));
            }

            // 獲取譫妄玉
            var deliriumData = await GetDeliriumOrbDataAsync();
            if (deliriumData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(deliriumData));
            }

            // 獲取罈物品
            var vialData = await GetVialDataAsync();
            if (vialData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(vialData));
            }

            // 嘗試獲取催化劑（如果有獨立端點的話）
            var catalystData = await GetCatalystDataAsync();
            if (catalystData != null)
            {
                prices.AddRange(ConvertItemToCurrencyPrices(catalystData));
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Failed to fetch currency prices - {ex.Message}");
            throw;
        }

        return prices;
    }

    private async Task<PoeNinjaResponse> GetCurrencyDataAsync(string type)
    {
        try
        {
            var url = $"{CurrencyBaseUrl}?league={Uri.EscapeDataString(_leagueName)}&type={type}";
            if (_isDebugEnabled?.Invoke() == true)
                DebugWindow.LogMsg($"PoeNinjaPricer: Fetching {type} data from {url}");

            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<PoeNinjaResponse>(response);

            if (data?.Lines?.Count > 0)
            {
                if (_isDebugEnabled?.Invoke() == true)
                    DebugWindow.LogMsg($"PoeNinjaPricer: Successfully fetched {data.Lines.Count} {type} entries");
                return data;
            }
            else
            {
                DebugWindow.LogError($"PoeNinjaPricer: No data received for {type}");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: HTTP error fetching {type} - {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: JSON parsing error for {type} - {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Request timeout for {type} - {ex.Message}");
            throw;
        }
    }

    private List<CurrencyPrice> ConvertToCurrencyPrices(PoeNinjaResponse data)
    {
        var prices = new List<CurrencyPrice>();
        var detailsDict = new Dictionary<int, CurrencyDetail>();

        // 建立詳細資料字典
        foreach (var detail in data.CurrencyDetails)
        {
            detailsDict[detail.Id] = detail;
        }

        // 轉換價格資料
        foreach (var line in data.Lines)
        {
            if (line.ChaosEquivalent <= 0) continue; // 跳過無效價格

            var price = new CurrencyPrice
            {
                Name = line.CurrencyTypeName,
                ChaosValue = Math.Round(line.ChaosEquivalent, 2),
                Change24h = Math.Round(line.ReceiveSparkLine.TotalChange, 2),
                Category = CurrencyClassifier.GetCategory(line.CurrencyTypeName)
            };

            // 更新 Divine Orb 匯率
            if (line.CurrencyTypeName.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase))
            {
                CurrencyPrice.UpdateDivineRate(price.ChaosValue);
                if (_isDebugEnabled?.Invoke() == true)
                    DebugWindow.LogMsg($"PoeNinjaPricer: Updated Divine Orb rate to {price.ChaosValue} chaos");
            }

            // 嘗試從詳細資料獲取圖示
            if (line.Receive?.GetCurrencyId > 0 && detailsDict.TryGetValue(line.Receive.GetCurrencyId, out var detail))
            {
                price.Icon = detail.Icon;
            }
            else if (line.Pay?.PayCurrencyId > 0 && detailsDict.TryGetValue(line.Pay.PayCurrencyId, out var payDetail))
            {
                price.Icon = payDetail.Icon;
            }

            prices.Add(price);
        }

        return prices;
    }

    /// <summary>
    /// 通用的物品資料獲取方法
    /// </summary>
    private async Task<ItemOverviewResponse> GetItemDataAsync(string itemType)
    {
        try
        {
            var url = $"{ItemBaseUrl}?league={Uri.EscapeDataString(_leagueName)}&type={itemType}";
            if (_isDebugEnabled?.Invoke() == true)
                DebugWindow.LogMsg($"PoeNinjaPricer: Fetching {itemType} data from {url}");

            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<ItemOverviewResponse>(response);

            if (data?.Lines?.Count > 0)
            {
                if (_isDebugEnabled?.Invoke() == true)
                    DebugWindow.LogMsg($"PoeNinjaPricer: Successfully fetched {data.Lines.Count} {itemType} entries");
                return data;
            }
            else
            {
                DebugWindow.LogError($"PoeNinjaPricer: No data received for {itemType}");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: HTTP error fetching {itemType} - {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: JSON parsing error for {itemType} - {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Request timeout for {itemType} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 獲取聖甲蟲資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetScarabDataAsync()
    {
        return await GetItemDataAsync("Scarab");
    }

    /// <summary>
    /// 獲取油瓶資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetOilDataAsync()
    {
        return await GetItemDataAsync("Oil");
    }

    /// <summary>
    /// 獲取化石資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetFossilDataAsync()
    {
        return await GetItemDataAsync("Fossil");
    }

    /// <summary>
    /// 獲取精髓資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetEssenceDataAsync()
    {
        return await GetItemDataAsync("Essence");
    }

    /// <summary>
    /// 獲取譫妄玉資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetDeliriumOrbDataAsync()
    {
        return await GetItemDataAsync("DeliriumOrb");
    }

    /// <summary>
    /// 獲取罈物品資料
    /// </summary>
    private async Task<ItemOverviewResponse> GetVialDataAsync()
    {
        return await GetItemDataAsync("Vial");
    }

    /// <summary>
    /// 獲取催化劑資料
    /// 注意：催化劑可能包含在Currency或Fragment類型中，而非獨立端點
    /// 這裡先嘗試作為獨立端點，如果失敗則從Fragment中獲取
    /// </summary>
    private async Task<ItemOverviewResponse> GetCatalystDataAsync()
    {
        try
        {
            // 先嘗試獨立的Catalyst端點
            return await GetItemDataAsync("Catalyst");
        }
        catch
        {
            // 如果失敗，可能催化劑包含在Fragment中，這裡返回null
            // Fragment已經在主方法中處理
            return null;
        }
    }

    /// <summary>
    /// 通用的物品資料轉換為價格資料方法
    /// </summary>
    private List<CurrencyPrice> ConvertItemToCurrencyPrices(ItemOverviewResponse data)
    {
        var prices = new List<CurrencyPrice>();

        foreach (var line in data.Lines)
        {
            if (line.ChaosValue <= 0) continue; // 跳過無效價格

            var price = new CurrencyPrice
            {
                Name = line.Name,
                ChaosValue = Math.Round(line.ChaosValue, 2),
                Change24h = Math.Round(line.Sparkline.TotalChange, 2),
                Icon = line.Icon,
                StackSize = line.StackSize,
                Category = CurrencyClassifier.GetCategory(line.Name)
            };

            prices.Add(price);
        }

        return prices;
    }

    /// <summary>
    /// 將聖甲蟲資料轉換為價格資料（向後兼容）
    /// </summary>
    private List<CurrencyPrice> ConvertScarabToCurrencyPrices(ItemOverviewResponse data)
    {
        return ConvertItemToCurrencyPrices(data);
    }

    /// <summary>
    /// 測試與 poe.ninja API 的連接
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var url = $"{CurrencyBaseUrl}?league={Uri.EscapeDataString(_leagueName)}&type=Currency";
            var response = await _httpClient.GetStringAsync(url);
            return !string.IsNullOrEmpty(response);
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Connection test failed - {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        // HttpClient 是靜態的，不需要 dispose
    }
}