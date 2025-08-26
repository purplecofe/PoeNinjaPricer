using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeNinjaPricer;

[Flags]
public enum CurrencyCategory : long
{
    None = 0,
    
    
    Currency = 1L << 0,
    Fragments = 1L << 1,
    UniqueIdols = 1L << 2,
    Runecrafts = 1L << 3,
    AllflameEmbers = 1L << 4,
    Tattoos = 1L << 5,
    Omens = 1L << 6,
    DivinationCards = 1L << 7,
    Artifacts = 1L << 8,
    Oils = 1L << 9,
    Incubators = 1L << 10,
    
    
    UniqueWeapons = 1L << 11,     
    UniqueArmours = 1L << 12,     
    UniqueAccessories = 1L << 13, 
    UniqueFlasks = 1L << 14,      
    UniqueJewels = 1L << 15,      
    UniqueTinctures = 1L << 16,   
    UniqueRelics = 1L << 17,      
    SkillGems = 1L << 18,         
    ClusterJewels = 1L << 19,     
    
    
    Maps = 1L << 20,              
    BlightedMaps = 1L << 21,      
    BlightRavagedMaps = 1L << 22, 
    UniqueMaps = 1L << 23,        
    DeliriumOrbs = 1L << 24,      
    Invitations = 1L << 25,       
    Scarabs = 1L << 26,           
    Memories = 1L << 27,          
    
    
    BaseTypes = 1L << 28,         
    Fossils = 1L << 29,           
    Resonators = 1L << 30,        
    Beasts = 1L << 31,            
    Essences = 1L << 32,         
    Vials = 1L << 33,            
    
    Others = 1L << 34,           
    
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
            
            CurrencyCategory.Currency => "Currency",
            CurrencyCategory.Fragments => "Fragments",
            CurrencyCategory.UniqueIdols => "Unique Idols",
            CurrencyCategory.Runecrafts => "Runecrafts",
            CurrencyCategory.AllflameEmbers => "Allflame Embers",
            CurrencyCategory.Tattoos => "Tattoos",
            CurrencyCategory.Omens => "Omens",
            CurrencyCategory.DivinationCards => "Divination Cards",
            CurrencyCategory.Artifacts => "Artifacts",
            CurrencyCategory.Oils => "Oils",
            CurrencyCategory.Incubators => "Incubators",
            
            
            CurrencyCategory.UniqueWeapons => "Unique Weapons",
            CurrencyCategory.UniqueArmours => "Unique Armours",
            CurrencyCategory.UniqueAccessories => "Unique Accessories",
            CurrencyCategory.UniqueFlasks => "Unique Flasks",
            CurrencyCategory.UniqueJewels => "Unique Jewels",
            CurrencyCategory.UniqueTinctures => "Unique Tinctures",
            CurrencyCategory.UniqueRelics => "Unique Relics",
            CurrencyCategory.SkillGems => "Skill Gems",
            CurrencyCategory.ClusterJewels => "Cluster Jewels",
            
            
            CurrencyCategory.Maps => "Maps",
            CurrencyCategory.BlightedMaps => "Blighted Maps",
            CurrencyCategory.BlightRavagedMaps => "Blight-ravaged Maps",
            CurrencyCategory.UniqueMaps => "Unique Maps",
            CurrencyCategory.DeliriumOrbs => "Delirium Orbs",
            CurrencyCategory.Invitations => "Invitations",
            CurrencyCategory.Scarabs => "Scarabs",
            CurrencyCategory.Memories => "Memories",
            
            
            CurrencyCategory.BaseTypes => "Base Types",
            CurrencyCategory.Fossils => "Fossils",
            CurrencyCategory.Resonators => "Resonators",
            CurrencyCategory.Beasts => "Beasts",
            CurrencyCategory.Essences => "Essences",
            CurrencyCategory.Vials => "Vials",
            
            CurrencyCategory.Others => "Others",
            CurrencyCategory.All => "All",
            CurrencyCategory.None => "None",
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
        
        
        { "Splinter", CurrencyCategory.Fragments },
        { "Fragment", CurrencyCategory.Fragments },
        { "Breachstone", CurrencyCategory.Fragments },
        { "Timeless", CurrencyCategory.Fragments },
        
        
        { "The ", CurrencyCategory.DivinationCards },
        
        
        { "Artifact", CurrencyCategory.Artifacts },
        
        
        { " Oil", CurrencyCategory.Oils },
        
        
        { "Incubator", CurrencyCategory.Incubators },
        
        
        { "Tattoo", CurrencyCategory.Tattoos },
        
        
        { "Omen", CurrencyCategory.Omens },
        
        
        { "Runecraft", CurrencyCategory.Runecrafts },
        
        
        { "Allflame", CurrencyCategory.AllflameEmbers },
        
        
        { "Unique Idol", CurrencyCategory.UniqueIdols },
        
        
        { "Unique Weapon", CurrencyCategory.UniqueWeapons },
        { "Unique Armour", CurrencyCategory.UniqueArmours },
        { "Unique Accessory", CurrencyCategory.UniqueAccessories },
        { "Unique Flask", CurrencyCategory.UniqueFlasks },
        { "Unique Jewel", CurrencyCategory.UniqueJewels },
        { "Unique Tincture", CurrencyCategory.UniqueTinctures },
        { "Unique Relic", CurrencyCategory.UniqueRelics },
        
        
        { "Skill Gem", CurrencyCategory.SkillGems },
        { "Support Gem", CurrencyCategory.SkillGems },
        { "Cluster Jewel", CurrencyCategory.ClusterJewels },
        
        
        { " Map", CurrencyCategory.Maps },
        { "Blighted ", CurrencyCategory.BlightedMaps },
        { "Blight-ravaged", CurrencyCategory.BlightRavagedMaps },
        
        
        { "Delirium Orb", CurrencyCategory.DeliriumOrbs },
        
        
        { "Invitation", CurrencyCategory.Invitations },
        
        
        { "Scarab", CurrencyCategory.Scarabs },
        
        
        { "Memory", CurrencyCategory.Memories },
        
        
        { "Base Type", CurrencyCategory.BaseTypes },
        
        
        { "Fossil", CurrencyCategory.Fossils },
        
        
        { "Resonator", CurrencyCategory.Resonators },
        
        
        { "Beast", CurrencyCategory.Beasts },
        
        
        { "Essence", CurrencyCategory.Essences },
        { "Remnant of Corruption", CurrencyCategory.Essences },
        
        
        { "Vial", CurrencyCategory.Vials }
    };

    public static CurrencyCategory GetCategory(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return CurrencyCategory.Others;

        
        if (_categoryMappings.TryGetValue(itemName, out var category))
            return category;

        
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