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
        
        return prices.Where(price => 
        {
            if (price == null || string.IsNullOrEmpty(price.Name))
                return false;
                
            // 分類篩選
            var itemCategory = CurrencyClassifier.GetCategory(price.Name);
            if (!activeCategories.HasFlag(itemCategory))
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
    
    
    
    public string GetFilterSummary(List<CurrencyPrice> originalPrices, List<CurrencyPrice> filteredPrices)
    {
        var totalItems = originalPrices?.Count ?? 0;
        var filteredItems = filteredPrices?.Count ?? 0;
        var activeCategories = GetActiveCategories();
        
        if (activeCategories == CurrencyCategory.None)
            return "No items displayed";
        
        if (activeCategories == CurrencyCategory.All)
            return $"Showing all {totalItems} items";
            
        var categoryCount = CountEnabledCategories(activeCategories);
        var summary = $"Showing {filteredItems}/{totalItems} items ({categoryCount} categories)";
            
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