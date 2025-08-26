using System;
using System.Collections.Generic;
using System.Linq;
using PoeNinjaPricer.Models;

namespace PoeNinjaPricer;

public class FilterManager
{
    private readonly PoeNinjaPricerSettings _settings;
    private Func<CurrencyCategory> _getActiveCategories;
    private Action<bool> _setAllCategories;
    
    public FilterManager(PoeNinjaPricerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
    
    public void SetActiveCategoriesProvider(Func<CurrencyCategory> provider)
    {
        _getActiveCategories = provider;
    }
    
    public void SetAllCategoriesProvider(Action<bool> provider)
    {
        _setAllCategories = provider;
    }
    
    public CurrencyCategory GetActiveCategories()
    {
        // 優先使用提供者函數
        if (_getActiveCategories != null)
            return _getActiveCategories();
            
        // 如果沒有提供者函數，返回預設顯示全部
        return CurrencyCategory.All;
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
        // 優先使用提供者函數
        if (_setAllCategories != null)
        {
            _setAllCategories(enabled);
        }
    }
    
    public void ApplyHighValueFilter()
    {
        // 這些方法現在由主程式直接處理，這裡只設置最小價值
        if (_settings != null)
        {
            _settings.MinChaosValue.Value = 1.0f; // 只顯示 1c 以上的物品
        }
    }
    
    public void ApplyCurrencyOnlyFilter()
    {
        // 這些方法現在由主程式直接處理，這裡只設置最小價值
        if (_settings != null)
        {
            _settings.MinChaosValue.Value = 0f;
        }
    }
    
    public void ApplyGeneralItemsFilter()
    {
        // 這些方法現在由主程式直接處理，這裡只設置最小價值
        if (_settings != null)
        {
            _settings.MinChaosValue.Value = 0f;
        }
    }
    
    
    public string GetFilterSummary(List<CurrencyPrice> originalPrices, List<CurrencyPrice> filteredPrices)
    {
        var totalItems = originalPrices?.Count ?? 0;
        var filteredItems = filteredPrices?.Count ?? 0;
        var activeCategories = GetActiveCategories();
        
        if (activeCategories == CurrencyCategory.None)
            return LocalizationService.Get("no_items_displayed");
        
        if (activeCategories == CurrencyCategory.All && _settings.MinChaosValue.Value == 0f)
            return LocalizationService.Get("showing_all", totalItems);
            
        var categoryCount = CountEnabledCategories(activeCategories);
        var summary = LocalizationService.Get("showing_filtered", filteredItems, totalItems, categoryCount);
        
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