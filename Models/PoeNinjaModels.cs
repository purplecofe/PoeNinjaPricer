using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace PoeNinjaPricer.Models;

public class PoeNinjaResponse
{
    [JsonProperty("lines")]
    public List<CurrencyLine> Lines { get; set; } = new();

    [JsonProperty("currencyDetails")]
    public List<CurrencyDetail> CurrencyDetails { get; set; } = new();
}

public class CurrencyLine
{
    [JsonProperty("currencyTypeName")]
    public string CurrencyTypeName { get; set; } = "";

    [JsonProperty("pay")]
    public CurrencyExchange Pay { get; set; } = new();

    [JsonProperty("receive")]
    public CurrencyExchange Receive { get; set; } = new();

    [JsonProperty("paySparkLine")]
    public SparkLine PaySparkLine { get; set; } = new();

    [JsonProperty("receiveSparkLine")]
    public SparkLine ReceiveSparkLine { get; set; } = new();

    [JsonProperty("chaosEquivalent")]
    public double ChaosEquivalent { get; set; }

    [JsonProperty("detailsId")]
    public string DetailsId { get; set; } = "";
}

public class CurrencyExchange
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("league_id")]
    public int LeagueId { get; set; }

    [JsonProperty("pay_currency_id")]
    public int PayCurrencyId { get; set; }

    [JsonProperty("get_currency_id")]
    public int GetCurrencyId { get; set; }

    [JsonProperty("sample_time_utc")]
    public string SampleTimeUtc { get; set; } = "";

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("value")]
    public double Value { get; set; }

    [JsonProperty("data_point_count")]
    public int DataPointCount { get; set; }

    [JsonProperty("includes_secondary")]
    public bool IncludesSecondary { get; set; }

    [JsonProperty("listing_count")]
    public int ListingCount { get; set; }
}

public class SparkLine
{
    [JsonProperty("data")]
    public List<double?> Data { get; set; } = new();

    [JsonProperty("totalChange")]
    public double TotalChange { get; set; }

    /// <summary>
    /// 獲取過濾掉 null 值的資料
    /// </summary>
    public List<double> GetValidData()
    {
        return Data.Where(x => x.HasValue).Select(x => x!.Value).ToList();
    }
}

public class CurrencyDetail
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("tradeId")]
    public string TradeId { get; set; } = "";
}

// 簡化的價格資料用於 UI 顯示
public class CurrencyPrice
{
    public string Name { get; set; } = "";
    public double ChaosValue { get; set; }
    public double Change24h { get; set; }
    public string Icon { get; set; } = "";
    public bool IsFragment { get; set; }

    private static double _divineRate = 250.0; // 預設值

    public double DivineValue => ChaosValue / _divineRate;

    public static void UpdateDivineRate(double newRate)
    {
        if (newRate > 0)
        {
            _divineRate = newRate;
        }
    }

    public static double GetDivineRate()
    {
        return _divineRate;
    }

    public string GetFormattedChaosValue()
    {
        return ChaosValue >= 1 ? ChaosValue.ToString("F2") : ChaosValue.ToString("F4");
    }

    public string GetFormattedDivineValue()
    {
        return DivineValue >= 0.01 ? DivineValue.ToString("F3") : DivineValue.ToString("F6");
    }
}