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

    private const string BaseUrl = "https://poe.ninja/api/data/currencyoverview";
    private readonly string _leagueName;

    public PoeNinjaApiService(string leagueName)
    {
        _leagueName = leagueName;
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
                prices.AddRange(ConvertToCurrencyPrices(currencyData, false));
            }

            // 獲取碎片
            var fragmentData = await GetCurrencyDataAsync("Fragment");
            if (fragmentData != null)
            {
                prices.AddRange(ConvertToCurrencyPrices(fragmentData, true));
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
            var url = $"{BaseUrl}?league={Uri.EscapeDataString(_leagueName)}&type={type}";
            DebugWindow.LogMsg($"PoeNinjaPricer: Fetching {type} data from {url}");

            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<PoeNinjaResponse>(response);

            if (data?.Lines?.Count > 0)
            {
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

    private List<CurrencyPrice> ConvertToCurrencyPrices(PoeNinjaResponse data, bool isFragment)
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
                IsFragment = isFragment
            };

            // 更新 Divine Orb 匯率
            if (line.CurrencyTypeName.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase))
            {
                CurrencyPrice.UpdateDivineRate(price.ChaosValue);
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
    /// 測試與 poe.ninja API 的連接
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var url = $"{BaseUrl}?league={Uri.EscapeDataString(_leagueName)}&type=Currency";
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