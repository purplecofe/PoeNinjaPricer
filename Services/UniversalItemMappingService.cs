using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore;
using ExileCore.Shared.Helpers;
using Newtonsoft.Json;

namespace PoeNinjaPricer.Services;

/// <summary>
/// 通用物品映射數據模型
/// </summary>
public class ItemMapping
{
    [JsonProperty("name_zh")]
    public string NameZh { get; set; } = string.Empty;
    
    [JsonProperty("name_en")]
    public string NameEn { get; set; } = string.Empty;
    
    [JsonProperty("base_type_zh")]
    public string BaseTypeZh { get; set; } = string.Empty;
    
    [JsonProperty("base_type_en")]
    public string BaseTypeEn { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// 所屬類別（運行時設定）
    /// </summary>
    [JsonIgnore]
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// 單一類別的映射資料容器
/// </summary>
public class ItemMappingData
{
    public string Category { get; set; } = string.Empty;
    public List<ItemMapping> Mappings { get; set; } = new();
    public Dictionary<string, ItemMapping> PathIndex { get; set; } = new();
    
    public void BuildIndexes()
    {
        PathIndex.Clear();
        
        foreach (var mapping in Mappings)
        {
            mapping.Category = Category;
            
            // Metadata 路徑索引（全域唯一）
            if (!string.IsNullOrEmpty(mapping.Type))
            {
                PathIndex[mapping.Type] = mapping;
            }
        }
    }
}

/// <summary>
/// 通用物品映射服務 - 支援多種物品類別的統一映射系統
/// </summary>
public class UniversalItemMappingService
{
    private readonly Dictionary<string, ItemMappingData> _categoryMappings = new();
    private readonly Dictionary<string, ItemMapping> _globalPathIndex = new();
    
    public bool IsInitialized { get; private set; }
    public int TotalMappingCount => _categoryMappings.Values.Sum(data => data.Mappings.Count);
    public IReadOnlyList<string> LoadedCategories => _categoryMappings.Keys.ToList().AsReadOnly();
    
    /// <summary>
    /// 初始化服務，載入所有預設類別
    /// </summary>
    public void Initialize(string pluginDirectory)
    {
        try
        {
            // 註冊預設的物品類別
            RegisterCategory("currency", Path.Combine(pluginDirectory, "json", "currency.json"));
            RegisterCategory("scarab", Path.Combine(pluginDirectory, "json", "scarab.json"));
            
            // 建立全域路徑索引
            BuildGlobalPathIndex();
            
            IsInitialized = true;
            DebugWindow.LogMsg($"UniversalItemMappingService: Initialized with {TotalMappingCount} total mappings across {LoadedCategories.Count} categories: {string.Join(", ", LoadedCategories)}");
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"UniversalItemMappingService: Failed to initialize - {ex.Message}");
            IsInitialized = false;
        }
    }
    
    /// <summary>
    /// 註冊新的物品類別
    /// </summary>
    public bool RegisterCategory(string category, string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                DebugWindow.LogError($"UniversalItemMappingService: JSON file not found for category '{category}' at {jsonPath}");
                return false;
            }
            
            var jsonContent = File.ReadAllText(jsonPath);
            var mappings = JsonConvert.DeserializeObject<List<ItemMapping>>(jsonContent);
            
            if (mappings == null || mappings.Count == 0)
            {
                DebugWindow.LogError($"UniversalItemMappingService: No mappings found for category '{category}'");
                return false;
            }
            
            var mappingData = new ItemMappingData
            {
                Category = category,
                Mappings = mappings
            };
            
            mappingData.BuildIndexes();
            _categoryMappings[category] = mappingData;
            
            DebugWindow.LogMsg($"UniversalItemMappingService: Loaded {mappings.Count} mappings for category '{category}' from {jsonPath}");
            return true;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"UniversalItemMappingService: Failed to register category '{category}' - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 建立全域路徑索引，優化路徑查詢效能
    /// </summary>
    private void BuildGlobalPathIndex()
    {
        _globalPathIndex.Clear();
        
        foreach (var categoryData in _categoryMappings.Values)
        {
            foreach (var pathMapping in categoryData.PathIndex)
            {
                // 檢查路徑衝突
                if (_globalPathIndex.ContainsKey(pathMapping.Key))
                {
                    DebugWindow.LogError($"UniversalItemMappingService: Path conflict detected: {pathMapping.Key}");
                }
                
                _globalPathIndex[pathMapping.Key] = pathMapping.Value;
            }
        }
    }
    
    /// <summary>
    /// 根據 Metadata 路徑查詢英文名稱（最高優先級）
    /// </summary>
    public string GetEnglishNameByPath(string metadataPath)
    {
        if (!IsInitialized || string.IsNullOrEmpty(metadataPath))
            return string.Empty;
            
        return _globalPathIndex.TryGetValue(metadataPath, out var mapping) ? mapping.NameEn : string.Empty;
    }
    
    
    /// <summary>
    /// 統一查詢介面 - 僅使用 Metadata 路徑查詢
    /// </summary>
    public string GetEnglishName(string metadataPath)
    {
        return GetEnglishNameByPath(metadataPath);
    }
    
    /// <summary>
    /// 獲取指定類別的所有映射（除錯用）
    /// </summary>
    public IReadOnlyList<ItemMapping> GetMappingsByCategory(string category)
    {
        if (_categoryMappings.TryGetValue(category, out var data))
            return data.Mappings.AsReadOnly();
        return new List<ItemMapping>().AsReadOnly();
    }
    
    /// <summary>
    /// 獲取統計資訊
    /// </summary>
    public string GetStatistics()
    {
        if (!IsInitialized)
            return "Service not initialized";
            
        var stats = _categoryMappings.Select(kvp => 
            $"{kvp.Key}: {kvp.Value.Mappings.Count} items").ToList();
            
        return $"Total: {TotalMappingCount} mappings\n" + string.Join("\n", stats);
    }
}