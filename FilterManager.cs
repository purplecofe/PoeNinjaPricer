using System;
using System.Collections.Generic;
using System.Linq;
using PoeNinjaPricer.Models;

namespace PoeNinjaPricer;

public class FilterManager
{
    private readonly PoeNinjaPricerSettings _settings;
    
    public FilterManager(PoeNinjaPricerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
    
    public CurrencyCategory GetActiveCategories()
    {
        if (_settings == null)
            return CurrencyCategory.All; // 預設顯示全部
            
        var activeCategories = CurrencyCategory.None;
        
        if (_settings.ShowBasicCurrency?.Value ?? true)
            activeCategories |= CurrencyCategory.BasicCurrency;
        if (_settings.ShowFragments?.Value ?? true)
            activeCategories |= CurrencyCategory.Fragments;
        if (_settings.ShowEssences?.Value ?? true)
            activeCategories |= CurrencyCategory.Essences;
        if (_settings.ShowFossils?.Value ?? true)
            activeCategories |= CurrencyCategory.Fossils;
        if (_settings.ShowResonators?.Value ?? true)
            activeCategories |= CurrencyCategory.Resonators;
        if (_settings.ShowOils?.Value ?? true)
            activeCategories |= CurrencyCategory.Oils;
        if (_settings.ShowCatalysts?.Value ?? true)
            activeCategories |= CurrencyCategory.Catalysts;
        if (_settings.ShowDeliriumOrbs?.Value ?? true)
            activeCategories |= CurrencyCategory.DeliriumOrbs;
        if (_settings.ShowScarabs?.Value ?? true)
            activeCategories |= CurrencyCategory.Scarabs;
        if (_settings.ShowOthers?.Value ?? true)
            activeCategories |= CurrencyCategory.Others;
            
        return activeCategories;
    }
    
    public bool IsCategoryEnabled(CurrencyCategory category)
    {
        var activeCategories = GetActiveCategories();
        return activeCategories.HasFlag(category);
    }
    
    public List<CurrencyPrice> ApplyFilters(List<CurrencyPrice> prices)
    {
        if (prices == null || !prices.Any())
            return new List<CurrencyPrice>();
        
        var activeCategories = GetActiveCategories();
        var minChaosValue = _settings?.MinChaosValue?.Value ?? 0f;
        
        return prices.Where(price => 
        {
            if (price == null || string.IsNullOrEmpty(price.Name))
                return false;
                
            // 分類篩選
            var itemCategory = CurrencyClassifier.GetCategory(price.Name);
            if (!activeCategories.HasFlag(itemCategory))
                return false;
            
            // 最小價值篩選
            if (price.ChaosValue < minChaosValue)
                return false;
            
            return true;
        }).ToList();
    }
    
    public void SetAllCategories(bool enabled)
    {
        if (_settings == null) return;
        
        _settings.ShowBasicCurrency.Value = enabled;
        _settings.ShowFragments.Value = enabled;
        _settings.ShowEssences.Value = enabled;
        _settings.ShowFossils.Value = enabled;
        _settings.ShowResonators.Value = enabled;
        _settings.ShowOils.Value = enabled;
        _settings.ShowCatalysts.Value = enabled;
        _settings.ShowDeliriumOrbs.Value = enabled;
        _settings.ShowScarabs.Value = enabled;
        _settings.ShowOthers.Value = enabled;
    }
    
    public void ApplyHighValueFilter()
    {
        if (_settings == null) return;
        
        SetAllCategories(false);
        _settings.ShowBasicCurrency.Value = true;
        _settings.ShowFragments.Value = true;
        _settings.ShowScarabs.Value = true;
        _settings.MinChaosValue.Value = 1.0f; // 只顯示 1c 以上的物品
    }
    
    public void ApplyBasicCurrencyFilter()
    {
        if (_settings == null) return;
        
        SetAllCategories(false);
        _settings.ShowBasicCurrency.Value = true;
        _settings.MinChaosValue.Value = 0f;
    }
    
    
    public string GetFilterSummary(List<CurrencyPrice> originalPrices, List<CurrencyPrice> filteredPrices)
    {
        var totalItems = originalPrices?.Count ?? 0;
        var filteredItems = filteredPrices?.Count ?? 0;
        var activeCategories = GetActiveCategories();
        
        if (activeCategories == CurrencyCategory.None)
            return "無項目顯示";
        
        if (activeCategories == CurrencyCategory.All && _settings.MinChaosValue.Value == 0f)
            return $"顯示全部 {totalItems} 項";
            
        var categoryCount = CountEnabledCategories(activeCategories);
        var summary = $"顯示 {filteredItems}/{totalItems} 項 ({categoryCount} 類別)";
        
        if (_settings.MinChaosValue.Value > 0f)
            summary += $" (≥{_settings.MinChaosValue.Value:F1}c)";
            
        return summary;
    }
    
    public Dictionary<CurrencyCategory, int> GetCategoryItemCounts(List<CurrencyPrice> prices)
    {
        var counts = new Dictionary<CurrencyCategory, int>();
        
        foreach (var category in CurrencyClassifier.GetAllCategories())
        {
            counts[category] = prices.Count(price => 
                CurrencyClassifier.GetCategory(price.Name).HasFlag(category));
        }
        
        return counts;
    }
    
    private static int CountEnabledCategories(CurrencyCategory categories)
    {
        var count = 0;
        foreach (CurrencyCategory category in Enum.GetValues<CurrencyCategory>())
        {
            if (category != CurrencyCategory.None && 
                category != CurrencyCategory.All && 
                categories.HasFlag(category))
            {
                count++;
            }
        }
        return count;
    }
}