using System;
using System.Collections.Generic;

namespace PoeNinjaPricer;

public enum Language
{
    English = 0,
    Chinese = 1
}

public static class LocalizationService
{
    private static Language _currentLanguage = Language.English;
    
    public static Language CurrentLanguage 
    { 
        get => _currentLanguage;
        set => _currentLanguage = value;
    }
    
    private static readonly Dictionary<string, Dictionary<Language, string>> _translations = new()
    {
        // Window and UI Elements
        ["window_title"] = new() { [Language.English] = "PoeNinja Pricer", [Language.Chinese] = "PoeNinja 價格查詢" },
        ["options"] = new() { [Language.English] = "Options:", [Language.Chinese] = "選項:" },
        ["language"] = new() { [Language.English] = "Language", [Language.Chinese] = "語言" },
        ["min_value"] = new() { [Language.English] = "Min Value:", [Language.Chinese] = "最小價值:" },
        ["search"] = new() { [Language.English] = "Search:", [Language.Chinese] = "搜尋:" },
        ["search_currency"] = new() { [Language.English] = "Search Currency", [Language.Chinese] = "搜尋通貨" },
        ["league"] = new() { [Language.English] = "League", [Language.Chinese] = "聯盟" },
        ["currency_type"] = new() { [Language.English] = "Currency", [Language.Chinese] = "通貨" },
        ["fragment_type"] = new() { [Language.English] = "Fragment", [Language.Chinese] = "碎片" },
        
        // Category Headers
        ["category_general"] = new() { [Language.English] = "GENERAL:", [Language.Chinese] = "GENERAL:" },
        ["category_atlas"] = new() { [Language.English] = "ATLAS:", [Language.Chinese] = "ATLAS:" },
        ["category_crafting"] = new() { [Language.English] = "CRAFT:", [Language.Chinese] = "CRAFT:" },
        ["category_others"] = new() { [Language.English] = "Others:", [Language.Chinese] = "其他:" },
        
        // Category Names
        ["currency"] = new() { [Language.English] = "Currency", [Language.Chinese] = "通貨" },
        ["fragments"] = new() { [Language.English] = "Fragments", [Language.Chinese] = "碎片" },
        ["unique_idols"] = new() { [Language.English] = "Unique Idols", [Language.Chinese] = "獨特神像" },
        ["runecrafts"] = new() { [Language.English] = "Runecrafts", [Language.Chinese] = "符文工藝" },
        ["allflame_embers"] = new() { [Language.English] = "Allflame Embers", [Language.Chinese] = "火焰餘燼" },
        ["tattoos"] = new() { [Language.English] = "Tattoos", [Language.Chinese] = "刺青" },
        ["omens"] = new() { [Language.English] = "Omens", [Language.Chinese] = "徵兆" },
        ["divination_cards"] = new() { [Language.English] = "Divination Cards", [Language.Chinese] = "占卜卡" },
        ["artifacts"] = new() { [Language.English] = "Artifacts", [Language.Chinese] = "聖物" },
        ["oils"] = new() { [Language.English] = "Oils", [Language.Chinese] = "油脂" },
        ["incubators"] = new() { [Language.English] = "Incubators", [Language.Chinese] = "孵化器" },
        
        // Equipment & Gems
        ["unique_weapons"] = new() { [Language.English] = "Unique Weapons", [Language.Chinese] = "獨特武器" },
        ["unique_armours"] = new() { [Language.English] = "Unique Armours", [Language.Chinese] = "獨特護甲" },
        ["unique_accessories"] = new() { [Language.English] = "Unique Accessories", [Language.Chinese] = "獨特飾品" },
        ["unique_flasks"] = new() { [Language.English] = "Unique Flasks", [Language.Chinese] = "獨特藥劑" },
        ["unique_jewels"] = new() { [Language.English] = "Unique Jewels", [Language.Chinese] = "獨特珠寶" },
        ["unique_tinctures"] = new() { [Language.English] = "Unique Tinctures", [Language.Chinese] = "獨特酊劑" },
        ["unique_relics"] = new() { [Language.English] = "Unique Relics", [Language.Chinese] = "獨特聖器" },
        ["skill_gems"] = new() { [Language.English] = "Skill Gems", [Language.Chinese] = "技能寶石" },
        ["cluster_jewels"] = new() { [Language.English] = "Cluster Jewels", [Language.Chinese] = "星團珠寶" },
        
        // Atlas
        ["maps"] = new() { [Language.English] = "Maps", [Language.Chinese] = "地圖" },
        ["blighted_maps"] = new() { [Language.English] = "Blighted Maps", [Language.Chinese] = "凋落地圖" },
        ["blight_ravaged_maps"] = new() { [Language.English] = "Blight-ravaged Maps", [Language.Chinese] = "凋落肆虐地圖" },
        ["unique_maps"] = new() { [Language.English] = "Unique Maps", [Language.Chinese] = "獨特地圖" },
        ["delirium_orbs"] = new() { [Language.English] = "Delirium Orbs", [Language.Chinese] = "昏迷寶珠" },
        ["invitations"] = new() { [Language.English] = "Invitations", [Language.Chinese] = "邀請函" },
        ["scarabs"] = new() { [Language.English] = "Scarabs", [Language.Chinese] = "聖甲蟲" },
        ["memories"] = new() { [Language.English] = "Memories", [Language.Chinese] = "記憶" },
        
        // Crafting
        ["base_types"] = new() { [Language.English] = "Base Types", [Language.Chinese] = "基礎類型" },
        ["fossils"] = new() { [Language.English] = "Fossils", [Language.Chinese] = "化石" },
        ["resonators"] = new() { [Language.English] = "Resonators", [Language.Chinese] = "共鳴器" },
        ["beasts"] = new() { [Language.English] = "Beasts", [Language.Chinese] = "野獸" },
        ["essences"] = new() { [Language.English] = "Essences", [Language.Chinese] = "精華" },
        ["vials"] = new() { [Language.English] = "Vials", [Language.Chinese] = "藥瓶" },
        ["others"] = new() { [Language.English] = "Others", [Language.Chinese] = "未分類" },
        
        // Generic Terms
        ["all"] = new() { [Language.English] = "All", [Language.Chinese] = "全部" },
        ["none"] = new() { [Language.English] = "None", [Language.Chinese] = "無" },
        
        // Buttons
        ["select_all_none"] = new() { [Language.English] = "Select All/None", [Language.Chinese] = "全選/全不選" },
        ["high_value"] = new() { [Language.English] = "High Value", [Language.Chinese] = "高價值" },
        ["currency_only"] = new() { [Language.English] = "Currency Only", [Language.Chinese] = "僅通貨" },
        ["common_items"] = new() { [Language.English] = "Common Items", [Language.Chinese] = "常用項目" },
        ["update_now"] = new() { [Language.English] = "Update Now", [Language.Chinese] = "立即更新價格" },
        ["test_connection"] = new() { [Language.English] = "Test Connection", [Language.Chinese] = "測試連線" },
        ["refresh"] = new() { [Language.English] = "Refresh", [Language.Chinese] = "重新整理" },
        
        // Table Headers
        ["item_name"] = new() { [Language.English] = "Name", [Language.Chinese] = "名稱" },
        ["item_type"] = new() { [Language.English] = "Type", [Language.Chinese] = "類型" },
        ["chaos_price"] = new() { [Language.English] = "Chaos", [Language.Chinese] = "混沌" },
        ["divine_price"] = new() { [Language.English] = "Divine", [Language.Chinese] = "神聖" },
        ["price_change"] = new() { [Language.English] = "Change", [Language.Chinese] = "變化" },
        ["change_24h"] = new() { [Language.English] = "24h Change", [Language.Chinese] = "24h變化" },
        ["confidence"] = new() { [Language.English] = "Confidence", [Language.Chinese] = "信心值" },
        
        // Status Messages
        ["updating"] = new() { [Language.English] = "Updating...", [Language.Chinese] = "更新中..." },
        ["last_update"] = new() { [Language.English] = "Last update", [Language.Chinese] = "最後更新" },
        ["not_updated"] = new() { [Language.English] = "Not updated yet", [Language.Chinese] = "尚未更新" },
        ["no_price_data"] = new() { [Language.English] = "No price data. Click 'Refresh' to load prices.", [Language.Chinese] = "沒有價格資料。請點擊「重新整理」載入價格。" },
        ["divine_rate"] = new() { [Language.English] = "Divine", [Language.Chinese] = "Divine" },
        
        // Error Messages
        ["init_failed"] = new() { [Language.English] = "Initialization failed", [Language.Chinese] = "初始化失敗" },
        ["price_update_failed"] = new() { [Language.English] = "Price update failed", [Language.Chinese] = "價格更新失敗" },
        ["testing_connection"] = new() { [Language.English] = "Testing connection...", [Language.Chinese] = "測試連線中..." },
        ["connection_test_success"] = new() { [Language.English] = "Connection test successful!", [Language.Chinese] = "連線測試成功！" },
        ["connection_test_failed"] = new() { [Language.English] = "Connection test failed", [Language.Chinese] = "連線測試失敗" },
        
        // Filter Summary
        ["no_items_displayed"] = new() { [Language.English] = "No items displayed", [Language.Chinese] = "無項目顯示" },
        ["showing_all"] = new() { [Language.English] = "Showing all {0} items", [Language.Chinese] = "顯示全部 {0} 項" },
        ["showing_filtered"] = new() { [Language.English] = "Showing {0}/{1} items ({2} categories)", [Language.Chinese] = "顯示 {0}/{1} 項 ({2} 類別)" },
        
        // Settings
        ["auto_update"] = new() { [Language.English] = "Auto Update", [Language.Chinese] = "自動更新" },
        ["show_chaos"] = new() { [Language.English] = "Show Chaos Values", [Language.Chinese] = "顯示混沌價值" },
        ["show_divine"] = new() { [Language.English] = "Show Divine Values", [Language.Chinese] = "顯示神聖價值" },
        ["show_changes"] = new() { [Language.English] = "Show Price Changes", [Language.Chinese] = "顯示價格變化" }
    };
    
    public static string Get(string key)
    {
        if (_translations.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(_currentLanguage, out var translation))
            {
                return translation;
            }
            
            // Fallback to English if current language not found
            if (translations.TryGetValue(Language.English, out var fallback))
            {
                return fallback;
            }
        }
        
        // Return key if no translation found
        return key;
    }
    
    public static string Get(string key, params object[] args)
    {
        var translation = Get(key);
        return args.Length > 0 ? string.Format(translation, args) : translation;
    }
    
    public static void SetLanguage(Language language)
    {
        _currentLanguage = language;
    }
    
    public static string[] GetLanguageNames()
    {
        return new[] { "English", "中文" };
    }
    
    public static Language[] GetSupportedLanguages()
    {
        return new[] { Language.English, Language.Chinese };
    }
}