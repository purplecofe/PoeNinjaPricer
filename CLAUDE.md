# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PoeNinjaPricer 是一個 ExileCore 插件，用於從 poe.ninja API 獲取 Path of Exile 的通貨和碎片價格資料。這是一個基於 C# .NET 8.0 和 ImGui.NET 開發的即時價格查詢工具，支援完整的多語言物品映射系統。

## Architecture

### Core Components

**PoeNinjaPricer.cs**: 主要插件類別，繼承自 `BaseSettingsPlugin<PoeNinjaPricerSettings>`
- 管理 UI 渲染（ImGui 介面）
- 處理熱鍵輸入和視窗狀態
- 協調各個服務組件
- 實現自動更新和手動刷新功能

**Services Layer**:
- `PoeNinjaApiService.cs`: 負責與 poe.ninja API 的 HTTP 通訊
- `PriceCacheService.cs`: 處理價格資料的本地快取和持久化
- `UniversalItemMappingService.cs`: **通用物品映射系統，支援多種物品類別的統一映射**

**Models**:
- `PoeNinjaModels.cs`: API 回應資料模型和價格資料結構

**Support Components**:
- `FilterManager.cs`: 實現分類過濾和搜尋功能
- `CurrencyCategory.cs`: 通貨分類定義
- `PoeNinjaPricerSettings.cs`: 插件設定和 UI 配置

### Data Flow Architecture

1. **初始化**: 偵測聯盟 → 載入映射資料 → 載入快取 → 建立服務實例
2. **更新循環**: API 請求 → 資料轉換 → 快取儲存 → UI 更新
3. **用戶互動**: 熱鍵觸發 → 過濾器應用 → 表格排序 → 即時搜尋
4. **物品映射**: Hover 偵測 → 名稱識別 → 價格查詢 → 即時顯示

## Universal Item Mapping System

### Architecture Overview

**UniversalItemMappingService** 是一個可擴展的統一映射系統，支援多種物品類別：

```csharp
public class UniversalItemMappingService
{
    private Dictionary<string, ItemMappingData> _categoryMappings = new();
    private Dictionary<string, ItemMapping> _globalPathIndex = new();
    
    // 註冊物品類別
    public bool RegisterCategory(string category, string jsonPath);
    
    // 統一查詢介面 - 僅使用 Metadata 路徑查詢
    public string GetEnglishName(string metadataPath);
}
```

### Supported Item Categories

目前支援的物品類別：
- **Currency**: 574 項通貨和碎片（`json/currency.json`）
- **Scarab**: 195 項聖甲蟲（`json/scarab.json`）
- **Future**: 可輕易擴展至 10+ 種其他物品類別

### Data Structure

```csharp
public class ItemMapping
{
    public string NameZh { get; set; }      // 中文名稱
    public string NameEn { get; set; }      // 英文名稱  
    public string BaseTypeZh { get; set; }  // 中文基底類型
    public string BaseTypeEn { get; set; }  // 英文基底類型
    public string Type { get; set; }        // Metadata 路徑
    public string Category { get; set; }    // 所屬類別
}
```

### Query Strategy

**唯一查詢方式**: Metadata 路徑查詢
- 每個物品都有唯一的 metadata 路徑
- 直接查詢 `_globalPathIndex[metadataPath]` 獲得對應英文名稱
- 最準確且高效的查詢方式
- 100% 依賴 JSON 映射資料的完整性

## Key Development Patterns

### Universal Mapping Registration

```csharp
// 初始化映射服務
_itemMappingService = new UniversalItemMappingService();
_itemMappingService.Initialize(sourceDirectory);

// 系統會自動載入：
// - currency.json (574 項)
// - scarab.json (195 項)
```

### Adding New Item Categories

擴展新物品類別只需：

1. **建立 JSON 資料檔案**:
```json
[
  {
    "name_zh": "中文名稱",
    "name_en": "English Name", 
    "base_type_zh": "中文基底類型",
    "base_type_en": "English Base Type",
    "type": "Metadata/Items/Path/ItemType",
    "url": "https://poedb.tw/tw/item_page"
  }
]
```

2. **註冊新類別**（在 `Initialize` 方法中）:
```csharp
RegisterCategory("essence", Path.Combine(pluginDirectory, "json", "essence.json"));
```

### Settings Pattern
```csharp
public class PoeNinjaPricerSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);
    public HotkeyNodeV2 TogglePriceWindow { get; set; } = Keys.F8;
    public ToggleNode EnableHoverPricing { get; set; } = new(true);
    public RangeNode<int> UpdateIntervalMinutes { get; set; } = new(5, 1, 30);
}
```

### ImGui Rendering Pattern
```csharp
public override void Render()
{
    if (!Settings.Enable.Value || !_showPriceWindow) return;
    
    ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.FirstUseEver);
    if (ImGui.Begin("Window Title", ref _showPriceWindow))
    {
        RenderContent();
    }
    ImGui.End();
    
    // Always check hover functionality
    CheckHoveredItem();
    RenderHoveredItemPrice();
}
```

## Plugin Dependencies

### NuGet References
```xml
<PackageReference Include="ImGui.NET" Version="1.90.0.1" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
```

### ExileCore References
```xml
<Reference Include="ExileCore">
  <HintPath>$(exapiPackage)\ExileCore.dll</HintPath>
  <Private>False</Private>
</Reference>
<Reference Include="GameOffsets">
  <HintPath>$(exapiPackage)\GameOffsets.dll</HintPath>
  <Private>False</Private>
</Reference>
```

## Configuration and Settings

**Key Settings**:
- `TogglePriceWindow`: 切換視窗顯示的快捷鍵（預設 F8）
- `EnableHoverPricing`: 啟用 hover 價格顯示功能（預設開啟）
- `UpdateIntervalMinutes`: 自動更新間隔（1-30分鐘）
- `LeagueName`: 聯盟名稱（空白時自動偵測）
- `ShowChaosValues`/`ShowDivineValues`/`ShowPriceChanges`: 顯示選項

**Filter System**:
- 過濾器控制只在主視窗中顯示，F12 設定面板不顯示種類篩選
- 使用私有變數管理過濾器狀態，不持久化到設定檔
- 預設啟動時只顯示通貨類別
- **支援 Scarab 過濾**：完整的聖甲蟲分類和搜尋

## API Integration

**Data Sources**:
- Currency: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Currency`
- Fragments: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Fragment`

**Rate Limiting**: 尊重 API 限制，使用本地快取減少請求頻率

**Error Handling**: 
- 網路錯誤時使用快取資料
- 顯示詳細錯誤訊息給用戶
- 自動重試機制

## Hover Item Price Display System

### Features
- **即時價格顯示**: 滑鼠懸停在物品上時顯示價格
- **完整多語言支援**: 支援中文客戶端的所有通貨和聖甲蟲
- **智能映射**: 100% 覆蓋率的名稱映射系統
- **效能優化**: 100ms 節流機制，記憶體高效索引

### Implementation Details

```csharp
private Entity GetHoveredItem()
{
    var uiHover = GameController.IngameState.UIHover;
    if (uiHover == null || uiHover.Address == 0)
        uiHover = GameController.IngameState.UIHoverElement;
    
    var hoverItemIcon = uiHover.AsObject<HoverItemIcon>();
    return hoverItemIcon?.Item;
}

private string GetEnglishItemName(Entity item)
{
    var path = item?.Path;
    if (string.IsNullOrEmpty(path)) return string.Empty;
    
    // 只使用 Metadata 路徑查詢
    if (_itemMappingService?.IsInitialized == true)
    {
        return _itemMappingService.GetEnglishName(path);
    }
    
    return string.Empty;
}
```
## Mapping Coverage

### Current Status
- **Currency**: 574 項通貨和碎片（100% 覆蓋率）
- **Scarab**: 195 項聖甲蟲（100% 覆蓋率）
- **Total**: 769 項物品的完整中英文映射

### JSON Data Structure
所有映射資料存放於 `json/` 目錄：
- `currency.json`: 通貨映射資料
- `scarab.json`: 聖甲蟲映射資料

每個 JSON 項目包含：
```json
{
  "name_zh": "中文顯示名稱",
  "name_en": "English Display Name",
  "base_type_zh": "中文基底類型", 
  "base_type_en": "English Base Type",
  "type": "Metadata/Items/Path/To/Item",
  "url": "https://poedb.tw/tw/item_reference"
}
```

## Common Development Tasks

### Adding New Item Categories

1. **準備 JSON 資料**: 建立新的物品映射 JSON 檔案
2. **註冊類別**: 在 `UniversalItemMappingService.Initialize()` 中加入新類別
3. **測試映射**: 確認新物品的 hover 功能正常運作

### Extending Mapping Coverage

1. **獲取 Metadata 路徑**: 使用除錯模式記錄新物品的路徑
2. **建立映射項目**: 在對應的 JSON 檔案中新增映射
3. **驗證功能**: 測試新物品的名稱識別和價格查詢

### UI Customization
- ImGui 表格支援排序、調整大小、篩選
- 使用 `ImGuiTableFlags` 控制表格行為
- 色彩主題通過 `ImGui.PushStyleColor` 設定
- Hover tooltip 支援半透明背景和自適應大小

## Future Roadmap

### 準備支援的物品類別
- **Essence** (精華): 約 50 項
- **Fossil** (化石): 約 30 項  
- **Beast** (野獸): 約 100 項
- **Prophecy** (預言): 約 80 項
- **Divination Cards** (命運卡): 約 500 項
- **Unique Items** (傳奇物品): 約 1000+ 項

### 架構優勢
- **一行擴展**: 新類別只需一行註冊程式碼
- **統一維護**: 所有類別共用查詢邏輯
- **效能擴展**: 索引系統支援大規模物品庫
- **記憶體效率**: 延遲載入和 LRU 快取機制