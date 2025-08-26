# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PoeNinjaPricer 是一個 ExileCore 插件，用於從 poe.ninja API 獲取 Path of Exile 的通貨和碎片價格資料。這是一個基於 C# .NET 8.0 和 ImGui.NET 開發的即時價格查詢工具。

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

**Models**:
- `PoeNinjaModels.cs`: API 回應資料模型和價格資料結構

**Support Components**:
- `FilterManager.cs`: 實現分類過濾和搜尋功能
- `CurrencyCategory.cs`: 通貨分類定義
- `PoeNinjaPricerSettings.cs`: 插件設定和 UI 配置

### Data Flow Architecture

1. **初始化**: 偵測聯盟 → 載入快取 → 建立服務實例
2. **更新循環**: API 請求 → 資料轉換 → 快取儲存 → UI 更新
3. **用戶互動**: 熱鍵觸發 → 過濾器應用 → 表格排序 → 即時搜尋

## Development Commands

### Build Commands

```bash
# 確保環境變數已設定
set exapiPackage=C:\Users\user\Downloads\ExileApi-Compiled-3.26.last

# 建置專案
dotnet build

# 清理建置檔案
dotnet clean

# 發布 Release 版本
dotnet build --configuration Release -p:Platform=x64
```

### Testing and Debugging

**插件測試**:
- 啟動 `Loader.exe` 進行即時測試
- 使用 F12 開啟 ExileCore 主選單查看編譯錯誤
- 檢查 `Logs/` 目錄中的詳細錯誤日誌

**API 測試**:
- 插件內建連線測試功能
- 手動測試：訪問 `https://poe.ninja/api/data/currencyoverview?league=Mercenaries&type=Currency`

## Key Development Patterns

### Settings Pattern
```csharp
public class PoeNinjaPricerSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);
    public HotkeyNodeV2 TogglePriceWindow { get; set; } = Keys.F8;
    public RangeNode<int> UpdateIntervalMinutes { get; set; } = new(5, 1, 30);
}
```

### Async Service Pattern
```csharp
private async Task UpdatePricesAsync()
{
    if (_isUpdating) return;
    _isUpdating = true;
    try
    {
        var prices = await _apiService.GetCurrencyPricesAsync();
        _cacheService.UpdateCache(prices);
        FilterPrices();
    }
    finally
    {
        _isUpdating = false;
    }
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
}
```

### Caching System
- **Local Storage**: 使用 JSON 序列化儲存在插件目錄
- **Time-based Expiry**: 根據設定的更新間隔判斷快取過期
- **Fallback Strategy**: API 失敗時使用快取資料

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
- `UpdateIntervalMinutes`: 自動更新間隔（1-30分鐘）
- `LeagueName`: 聯盟名稱（空白時自動偵測）
- `MinChaosValue`: 最小價值過濾（0-100c）
- `ShowChaosValues`/`ShowDivineValues`/`ShowPriceChanges`: 顯示選項

**Filter System**:
- 過濾器控制只在主視窗中顯示，F12 設定面板不顯示種類篩選
- 使用私有變數管理過濾器狀態，不持久化到設定檔
- 支援快速預設過濾器：高價值、僅通貨、常用物品

**Window State Persistence**:
- 視窗位置和大小自動儲存
- 搜尋歷史記錄

## API Integration

**Data Sources**:
- Currency: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Currency`
- Fragments: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Fragment`

**Rate Limiting**: 尊重 API 限制，使用本地快取減少請求頻率

**Error Handling**: 
- 網路錯誤時使用快取資料
- 顯示詳細錯誤訊息給用戶
- 自動重試機制

## Common Development Tasks

### Adding New Currency Categories
1. 更新 `CurrencyCategory.cs` 列舉
2. 在主程式 `PoeNinjaPricer.cs` 中添加對應的私有 bool 變數
3. 修改 `GetCurrentActiveCategories()` 和 `SetAllFilterCategories()` 方法
4. 更新 `RenderFilterControls()` 中的 UI 渲染

### Extending API Support
1. 在 `PoeNinjaModels.cs` 定義新的資料模型
2. 擴展 `PoeNinjaApiService.cs` 的 API 調用
3. 更新資料轉換邏輯
4. 相應調整快取系統

### UI Customization
- ImGui 表格支援排序、調整大小、篩選
- 使用 `ImGuiTableFlags` 控制表格行為
- 色彩主題通過 `ImGui.PushStyleColor` 設定

## Recent Changes

### Filter System Redesign (2025-08-27)
- 移除了設定檔案中的種類過濾器屬性，避免 F12 設定面板過於複雜
- 過濾器狀態改為使用主程式中的私有變數管理
- `FilterManager` 完全依賴主程式提供的回調函數
- 快速過濾器按鈕直接在主程式中處理，提供更好的用戶體驗

