using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeNinjaPricer;

[Flags]
public enum CurrencyCategory : long
{
    None = 0,
    
    // GENERAL 類別
    Currency = 1L << 0,           // 通貨
    Fragments = 1L << 1,          // 碎片
    UniqueIdols = 1L << 2,        // 獨特神像
    Runecrafts = 1L << 3,         // 符文工藝
    AllflameEmbers = 1L << 4,     // 火焰餘燼
    Tattoos = 1L << 5,            // 刺青
    Omens = 1L << 6,              // 徵兆
    DivinationCards = 1L << 7,    // 占卜卡
    Artifacts = 1L << 8,          // 聖物
    Oils = 1L << 9,               // 油脂
    Incubators = 1L << 10,        // 孵化器
    
    // EQUIPMENT & GEMS 類別
    UniqueWeapons = 1L << 11,     // 獨特武器
    UniqueArmours = 1L << 12,     // 獨特護甲
    UniqueAccessories = 1L << 13, // 獨特飾品
    UniqueFlasks = 1L << 14,      // 獨特藥劑
    UniqueJewels = 1L << 15,      // 獨特珠寶
    UniqueTinctures = 1L << 16,   // 獨特酊劑
    UniqueRelics = 1L << 17,      // 獨特聖器
    SkillGems = 1L << 18,         // 技能寶石
    ClusterJewels = 1L << 19,     // 星團珠寶
    
    // ATLAS 類別
    Maps = 1L << 20,              // 地圖
    BlightedMaps = 1L << 21,      // 凋落地圖
    BlightRavagedMaps = 1L << 22, // 凋落肆虐地圖
    UniqueMaps = 1L << 23,        // 獨特地圖
    DeliriumOrbs = 1L << 24,      // 昏迷寶珠
    Invitations = 1L << 25,       // 邀請函
    Scarabs = 1L << 26,           // 聖甲蟲
    Memories = 1L << 27,          // 記憶
    
    // CRAFTING 類別
    BaseTypes = 1L << 28,         // 基礎類型
    Fossils = 1L << 29,           // 化石
    Resonators = 1L << 30,        // 共鳴器
    Beasts = 1L << 31,            // 野獸
    Essences = 1L << 32,         // 精華
    Vials = 1L << 33,            // 藥瓶
    
    Others = 1L << 34,           // 其他
    
    All = Currency | Fragments | UniqueIdols | Runecrafts | AllflameEmbers | Tattoos | Omens | DivinationCards | Artifacts | Oils | Incubators |
          UniqueWeapons | UniqueArmours | UniqueAccessories | UniqueFlasks | UniqueJewels | UniqueTinctures | UniqueRelics | SkillGems | ClusterJewels |
          Maps | BlightedMaps | BlightRavagedMaps | UniqueMaps | DeliriumOrbs | Invitations | Scarabs | Memories |
          BaseTypes | Fossils | Resonators | Beasts | Essences | Vials | Others
}

public static class CurrencyCategoryExtensions
{
    public static string GetDisplayName(this CurrencyCategory category)
    {
        return category switch
        {
            // GENERAL 類別
            CurrencyCategory.Currency => LocalizationService.Get("currency"),
            CurrencyCategory.Fragments => LocalizationService.Get("fragments"),
            CurrencyCategory.UniqueIdols => LocalizationService.Get("unique_idols"),
            CurrencyCategory.Runecrafts => LocalizationService.Get("runecrafts"),
            CurrencyCategory.AllflameEmbers => LocalizationService.Get("allflame_embers"),
            CurrencyCategory.Tattoos => LocalizationService.Get("tattoos"),
            CurrencyCategory.Omens => LocalizationService.Get("omens"),
            CurrencyCategory.DivinationCards => LocalizationService.Get("divination_cards"),
            CurrencyCategory.Artifacts => LocalizationService.Get("artifacts"),
            CurrencyCategory.Oils => LocalizationService.Get("oils"),
            CurrencyCategory.Incubators => LocalizationService.Get("incubators"),
            
            // EQUIPMENT & GEMS 類別
            CurrencyCategory.UniqueWeapons => LocalizationService.Get("unique_weapons"),
            CurrencyCategory.UniqueArmours => LocalizationService.Get("unique_armours"),
            CurrencyCategory.UniqueAccessories => LocalizationService.Get("unique_accessories"),
            CurrencyCategory.UniqueFlasks => LocalizationService.Get("unique_flasks"),
            CurrencyCategory.UniqueJewels => LocalizationService.Get("unique_jewels"),
            CurrencyCategory.UniqueTinctures => LocalizationService.Get("unique_tinctures"),
            CurrencyCategory.UniqueRelics => LocalizationService.Get("unique_relics"),
            CurrencyCategory.SkillGems => LocalizationService.Get("skill_gems"),
            CurrencyCategory.ClusterJewels => LocalizationService.Get("cluster_jewels"),
            
            // ATLAS 類別
            CurrencyCategory.Maps => LocalizationService.Get("maps"),
            CurrencyCategory.BlightedMaps => LocalizationService.Get("blighted_maps"),
            CurrencyCategory.BlightRavagedMaps => LocalizationService.Get("blight_ravaged_maps"),
            CurrencyCategory.UniqueMaps => LocalizationService.Get("unique_maps"),
            CurrencyCategory.DeliriumOrbs => LocalizationService.Get("delirium_orbs"),
            CurrencyCategory.Invitations => LocalizationService.Get("invitations"),
            CurrencyCategory.Scarabs => LocalizationService.Get("scarabs"),
            CurrencyCategory.Memories => LocalizationService.Get("memories"),
            
            // CRAFTING 類別
            CurrencyCategory.BaseTypes => LocalizationService.Get("base_types"),
            CurrencyCategory.Fossils => LocalizationService.Get("fossils"),
            CurrencyCategory.Resonators => LocalizationService.Get("resonators"),
            CurrencyCategory.Beasts => LocalizationService.Get("beasts"),
            CurrencyCategory.Essences => LocalizationService.Get("essences"),
            CurrencyCategory.Vials => LocalizationService.Get("vials"),
            
            CurrencyCategory.Others => LocalizationService.Get("others"),
            CurrencyCategory.All => LocalizationService.Get("all"),
            CurrencyCategory.None => LocalizationService.Get("none"),
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
        // GENERAL - Currency (通貨)
        { "Chaos Orb", CurrencyCategory.Currency },
        { "Divine Orb", CurrencyCategory.Currency },
        { "Exalted Orb", CurrencyCategory.Currency },
        { "Ancient Orb", CurrencyCategory.Currency },
        { "Orb of Fusing", CurrencyCategory.Currency },
        { "Orb of Alchemy", CurrencyCategory.Currency },
        { "Chromatic Orb", CurrencyCategory.Currency },
        { "Jeweller's Orb", CurrencyCategory.Currency },
        { "Orb of Alteration", CurrencyCategory.Currency },
        { "Orb of Augmentation", CurrencyCategory.Currency },
        { "Regal Orb", CurrencyCategory.Currency },
        { "Orb of Scouring", CurrencyCategory.Currency },
        { "Blessed Orb", CurrencyCategory.Currency },
        { "Orb of Regret", CurrencyCategory.Currency },
        { "Gemcutter's Prism", CurrencyCategory.Currency },
        { "Cartographer's Chisel", CurrencyCategory.Currency },
        { "Glassblower's Bauble", CurrencyCategory.Currency },
        { "Armourer's Scrap", CurrencyCategory.Currency },
        { "Blacksmith's Whetstone", CurrencyCategory.Currency },
        { "Portal Scroll", CurrencyCategory.Currency },
        { "Scroll of Wisdom", CurrencyCategory.Currency },
        { "Orb of Chance", CurrencyCategory.Currency },
        { "Vaal Orb", CurrencyCategory.Currency },
        { "Mirror of Kalandra", CurrencyCategory.Currency },
        { "Hinekora's Lock", CurrencyCategory.Currency },
        { "Mirror Shard", CurrencyCategory.Currency },
        { "Reflecting Mist", CurrencyCategory.Currency },
        { "Veiled Exalted Orb", CurrencyCategory.Currency },
        { "Orb of Remembrance", CurrencyCategory.Currency },
        { "Orb of Dominance", CurrencyCategory.Currency },
        { "Orb of Unravelling", CurrencyCategory.Currency },
        { "Fracturing Orb", CurrencyCategory.Currency },
        { "Tainted Divine Teardrop", CurrencyCategory.Currency },
        { "Awakener's Orb", CurrencyCategory.Currency },
        { "Crusader's Exalted Orb", CurrencyCategory.Currency },
        { "Hunter's Exalted Orb", CurrencyCategory.Currency },
        { "Warlord's Exalted Orb", CurrencyCategory.Currency },
        { "Redeemer's Exalted Orb", CurrencyCategory.Currency },
        { "Tailoring Orb", CurrencyCategory.Currency },
        { "Blessing of Chayula", CurrencyCategory.Currency },
        { "Orb of Conflict", CurrencyCategory.Currency },
        { "Exceptional Eldritch Ember", CurrencyCategory.Currency },
        { "Blessing of Esh", CurrencyCategory.Currency },
        { "Blessing of Uul-Netol", CurrencyCategory.Currency },
        { "Blessing of Xoph", CurrencyCategory.Currency },
        { "Exceptional Eldritch Ichor", CurrencyCategory.Currency },
        { "Tempering Orb", CurrencyCategory.Currency },
        { "Blessing of Tul", CurrencyCategory.Currency },
        { "Elder's Exalted Orb", CurrencyCategory.Currency },
        { "Shaper's Exalted Orb", CurrencyCategory.Currency },
        { "Tainted Exalted Orb", CurrencyCategory.Currency },
        { "Sacred Crystallised Lifeforce", CurrencyCategory.Currency },
        
        // GENERAL - Fragments (碎片)
        { "Splinter", CurrencyCategory.Fragments },
        { "Fragment", CurrencyCategory.Fragments },
        { "Breachstone", CurrencyCategory.Fragments },
        { "Timeless", CurrencyCategory.Fragments },
        
        // GENERAL - Divination Cards (占卜卡) - 通常以 "The " 開頭
        { "The ", CurrencyCategory.DivinationCards },
        
        // GENERAL - Artifacts (聖物)
        { "Artifact", CurrencyCategory.Artifacts },
        
        // GENERAL - Oils (油脂)
        { " Oil", CurrencyCategory.Oils },
        
        // GENERAL - Incubators (孵化器)
        { "Incubator", CurrencyCategory.Incubators },
        
        // GENERAL - Tattoos (刺青)
        { "Tattoo", CurrencyCategory.Tattoos },
        
        // GENERAL - Omens (徵兆)
        { "Omen", CurrencyCategory.Omens },
        
        // GENERAL - Runecrafts (符文工藝)
        { "Runecraft", CurrencyCategory.Runecrafts },
        
        // GENERAL - Allflame Embers (火焰餘燼)
        { "Allflame", CurrencyCategory.AllflameEmbers },
        
        // GENERAL - Unique Idols (獨特神像)
        { "Unique Idol", CurrencyCategory.UniqueIdols },
        
        // EQUIPMENT & GEMS - Unique items
        { "Unique Weapon", CurrencyCategory.UniqueWeapons },
        { "Unique Armour", CurrencyCategory.UniqueArmours },
        { "Unique Accessory", CurrencyCategory.UniqueAccessories },
        { "Unique Flask", CurrencyCategory.UniqueFlasks },
        { "Unique Jewel", CurrencyCategory.UniqueJewels },
        { "Unique Tincture", CurrencyCategory.UniqueTinctures },
        { "Unique Relic", CurrencyCategory.UniqueRelics },
        
        // EQUIPMENT & GEMS - Gems
        { "Skill Gem", CurrencyCategory.SkillGems },
        { "Support Gem", CurrencyCategory.SkillGems },
        { "Cluster Jewel", CurrencyCategory.ClusterJewels },
        
        // ATLAS - Maps
        { " Map", CurrencyCategory.Maps },
        { "Blighted ", CurrencyCategory.BlightedMaps },
        { "Blight-ravaged", CurrencyCategory.BlightRavagedMaps },
        
        // ATLAS - Delirium Orbs (昏迷寶珠)
        { "Delirium Orb", CurrencyCategory.DeliriumOrbs },
        
        // ATLAS - Invitations (邀請函)
        { "Invitation", CurrencyCategory.Invitations },
        
        // ATLAS - Scarabs (聖甲蟲)
        { "Scarab", CurrencyCategory.Scarabs },
        
        // ATLAS - Memories (記憶)
        { "Memory", CurrencyCategory.Memories },
        
        // CRAFTING - Base Types (基礎類型)
        { "Base Type", CurrencyCategory.BaseTypes },
        
        // CRAFTING - Fossils (化石)
        { "Fossil", CurrencyCategory.Fossils },
        
        // CRAFTING - Resonators (共鳴器)
        { "Resonator", CurrencyCategory.Resonators },
        
        // CRAFTING - Beasts (野獸)
        { "Beast", CurrencyCategory.Beasts },
        
        // CRAFTING - Essences (精華)
        { "Essence", CurrencyCategory.Essences },
        { "Remnant of Corruption", CurrencyCategory.Essences },
        
        // CRAFTING - Vials (藥瓶)
        { "Vial", CurrencyCategory.Vials }
    };

    public static CurrencyCategory GetCategory(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return CurrencyCategory.Others;

        // 直接匹配
        if (_categoryMappings.TryGetValue(itemName, out var category))
            return category;

        // 部分匹配 - 按優先級排序，更具體的先匹配
        var sortedMappings = _categoryMappings.OrderByDescending(x => x.Key.Length);
        foreach (var mapping in sortedMappings)
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