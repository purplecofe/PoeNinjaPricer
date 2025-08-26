using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeNinjaPricer;

[Flags]
public enum CurrencyCategory
{
    None = 0,
    BasicCurrency = 1 << 0,     // 基礎通貨
    Fragments = 1 << 1,         // 碎片類
    Essences = 1 << 2,          // 精髓
    Fossils = 1 << 3,           // 化石
    Resonators = 1 << 4,        // 共鳴器
    Oils = 1 << 5,              // 聖油
    Catalysts = 1 << 6,         // 催化劑
    DeliriumOrbs = 1 << 7,      // 譫妄玉
    Scarabs = 1 << 8,           // 聖甲蟲
    Others = 1 << 9,            // 其他
    All = BasicCurrency | Fragments | Essences | Fossils | Resonators | Oils | Catalysts | DeliriumOrbs | Scarabs | Others
}

public static class CurrencyCategoryExtensions
{
    public static string GetDisplayName(this CurrencyCategory category)
    {
        return category switch
        {
            CurrencyCategory.BasicCurrency => "基礎通貨",
            CurrencyCategory.Fragments => "碎片類",
            CurrencyCategory.Essences => "精髓",
            CurrencyCategory.Fossils => "化石",
            CurrencyCategory.Resonators => "共鳴器",
            CurrencyCategory.Oils => "聖油",
            CurrencyCategory.Catalysts => "催化劑",
            CurrencyCategory.DeliriumOrbs => "譫妄玉",
            CurrencyCategory.Scarabs => "聖甲蟲",
            CurrencyCategory.Others => "其他",
            CurrencyCategory.All => "全部",
            CurrencyCategory.None => "無",
            _ => category.ToString()
        };
    }

    public static bool HasFlag(this CurrencyCategory source, CurrencyCategory flag)
    {
        return (source & flag) == flag;
    }
}

public static class CurrencyClassifier
{
    private static readonly Dictionary<string, CurrencyCategory> _categoryMappings = new()
    {
        // 基礎通貨
        { "Chaos Orb", CurrencyCategory.BasicCurrency },
        { "Divine Orb", CurrencyCategory.BasicCurrency },
        { "Exalted Orb", CurrencyCategory.BasicCurrency },
        { "Ancient Orb", CurrencyCategory.BasicCurrency },
        { "Orb of Fusing", CurrencyCategory.BasicCurrency },
        { "Orb of Alchemy", CurrencyCategory.BasicCurrency },
        { "Chromatic Orb", CurrencyCategory.BasicCurrency },
        { "Jeweller's Orb", CurrencyCategory.BasicCurrency },
        { "Orb of Alteration", CurrencyCategory.BasicCurrency },
        { "Orb of Augmentation", CurrencyCategory.BasicCurrency },
        { "Regal Orb", CurrencyCategory.BasicCurrency },
        { "Orb of Scouring", CurrencyCategory.BasicCurrency },
        { "Blessed Orb", CurrencyCategory.BasicCurrency },
        { "Orb of Regret", CurrencyCategory.BasicCurrency },
        { "Gemcutter's Prism", CurrencyCategory.BasicCurrency },
        { "Cartographer's Chisel", CurrencyCategory.BasicCurrency },
        { "Glassblower's Bauble", CurrencyCategory.BasicCurrency },
        { "Armourer's Scrap", CurrencyCategory.BasicCurrency },
        { "Blacksmith's Whetstone", CurrencyCategory.BasicCurrency },
        { "Portal Scroll", CurrencyCategory.BasicCurrency },
        { "Scroll of Wisdom", CurrencyCategory.BasicCurrency },
        { "Orb of Chance", CurrencyCategory.BasicCurrency },
        { "Vaal Orb", CurrencyCategory.BasicCurrency },
        
        // 碎片類
        { "Splinter", CurrencyCategory.Fragments },
        { "Fragment", CurrencyCategory.Fragments },
        { "Breachstone", CurrencyCategory.Fragments },
        { "Timeless", CurrencyCategory.Fragments },
        
        // 精髓
        { "Essence", CurrencyCategory.Essences },
        { "Remnant of Corruption", CurrencyCategory.Essences },
        
        // 化石
        { "Fossil", CurrencyCategory.Fossils },
        
        // 共鳴器
        { "Resonator", CurrencyCategory.Resonators },
        
        // 聖油
        { "Oil", CurrencyCategory.Oils },
        
        // 催化劑
        { "Catalyst", CurrencyCategory.Catalysts },
        
        // 譫妄玉
        { "Delirium Orb", CurrencyCategory.DeliriumOrbs },
        
        // 聖甲蟲
        { "Scarab", CurrencyCategory.Scarabs }
    };

    public static CurrencyCategory GetCategory(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return CurrencyCategory.Others;

        // 直接匹配
        if (_categoryMappings.TryGetValue(itemName, out var category))
            return category;

        // 部分匹配
        foreach (var mapping in _categoryMappings)
        {
            if (itemName.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                return mapping.Value;
        }

        return CurrencyCategory.Others;
    }

    public static List<CurrencyCategory> GetAllCategories()
    {
        return Enum.GetValues<CurrencyCategory>()
            .Where(c => c != CurrencyCategory.None && c != CurrencyCategory.All)
            .ToList();
    }

    public static int CountItemsInCategory(List<Models.CurrencyPrice> prices, CurrencyCategory category)
    {
        return prices.Count(price => GetCategory(price.Name).HasFlag(category));
    }
}