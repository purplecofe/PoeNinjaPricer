# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PoeNinjaPricer is an ExileCore plugin that fetches Path of Exile currency and fragment price data from the poe.ninja API. It is a real-time price checking tool developed with C# .NET 8.0 and ImGui.NET, featuring a complete multilingual item mapping system.

## Architecture

### Core Components

**PoeNinjaPricer.cs**: Main plugin class inheriting from `BaseSettingsPlugin<PoeNinjaPricerSettings>`
- Manages UI rendering (ImGui interface)
- Handles hotkey input and window state
- Coordinates various service components
- Implements automatic updates and manual refresh functionality

**Services Layer**:
- `PoeNinjaApiService.cs`: Handles HTTP communication with poe.ninja API
- `PriceCacheService.cs`: Manages local caching and persistence of price data
- `UniversalItemMappingService.cs`: **Universal item mapping system supporting unified mapping for multiple item categories**

**Models**:
- `PoeNinjaModels.cs`: API response data models and price data structures

**Support Components**:
- `FilterManager.cs`: Implements category filtering and search functionality
- `CurrencyCategory.cs`: Currency category definitions
- `PoeNinjaPricerSettings.cs`: Plugin settings and UI configuration

### Data Flow Architecture

1. **Initialization**: League detection → Load mapping data → Load cache → Create service instances
2. **Update Cycle**: API request → Data transformation → Cache storage → UI update
3. **User Interaction**: Hotkey trigger → Filter application → Table sorting → Real-time search
4. **Item Mapping**: Hover detection → Name identification → Price query → Real-time display

## Universal Item Mapping System

### Architecture Overview

**UniversalItemMappingService** is an extensible unified mapping system that supports multiple item categories:

```csharp
public class UniversalItemMappingService
{
    private Dictionary<string, ItemMappingData> _categoryMappings = new();
    private Dictionary<string, ItemMapping> _globalPathIndex = new();
    
    // Register item category
    public bool RegisterCategory(string category, string jsonPath);
    
    // Unified query interface - uses only Metadata path queries
    public string GetEnglishName(string metadataPath);
}
```

### Supported Item Categories

Currently supported item categories:
- **Currency**: 359 currencies and fragments (`json/currency.json`)
- **Scarab**: 123 scarabs (`json/scarab.json`)
- **Fragments**: 93 map device recipe fragments (`json/fragments.json`)
- **Essence**: 105 essence league items (`json/essence.json`)
- **Delirium**: 22 delirium orb items (`json/delirium.json`)
- **Vial**: 9 vial items (`json/vial.json`)
- **Blessing**: 5 blessing items (`json/blessing.json`)
- **Breach**: 13 breach splinter items (`json/breach.json`)
- **Oil**: 16 oil items (`json/oil.json`)
- **Fossil**: 25 fossil items (`json/fossil.json`)
- **Catalyst**: 11 catalyst items (`json/catalyst.json`)
- **Total**: 11 categories with 781 items

### Data Structure

```csharp
public class ItemMapping
{
    public string NameZh { get; set; }      // Chinese name
    public string NameEn { get; set; }      // English name  
    public string BaseTypeZh { get; set; }  // Chinese base type
    public string BaseTypeEn { get; set; }  // English base type
    public string Type { get; set; }        // Metadata path
    public string Category { get; set; }    // Category
}
```

### Query Strategy

**Single Query Method**: Metadata path query
- Each item has a unique metadata path
- Direct query via `_globalPathIndex[metadataPath]` to get corresponding English name
- Most accurate and efficient query method
- 100% dependent on JSON mapping data integrity

## Key Development Patterns

### Universal Mapping Registration

```csharp
// Initialize mapping service
_itemMappingService = new UniversalItemMappingService();
_itemMappingService.Initialize(sourceDirectory);

// System will automatically load:
// - currency.json (574 items)
// - scarab.json (195 items)
```

### Adding New Item Categories

To extend new item categories:

1. **Create JSON data file**:
```json
[
  {
    "name_zh": "Chinese Name",
    "name_en": "English Name", 
    "base_type_zh": "Chinese Base Type",
    "base_type_en": "English Base Type",
    "type": "Metadata/Items/Path/ItemType",
    "url": "https://poedb.tw/tw/item_page"
  }
]
```

2. **Register new category** (in `Initialize` method):
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
- `TogglePriceWindow`: Toggle window display hotkey (default F8)
- `EnableHoverPricing`: Enable hover price display functionality (default enabled)
- `UpdateIntervalMinutes`: Auto-update interval (1-30 minutes)
- `LeagueName`: League name (auto-detect when empty)
- `ShowChaosValues`/`ShowDivineValues`/`ShowPriceChanges`: Display options

**Filter System**:
- Filter controls only displayed in main window, F12 settings panel doesn't show category filters
- Uses private variables to manage filter state, not persisted to settings file
- Default startup shows only currency category
- **Scarab filtering support**: Complete scarab categorization and search

## API Integration

**Data Sources**:
- Currency: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Currency`
- Fragments: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Fragment`

**Rate Limiting**: Respects API limits, uses local cache to reduce request frequency

**Error Handling**: 
- Uses cached data during network errors
- Displays detailed error messages to users
- Automatic retry mechanism

## Hover Item Price Display System

### Features
- **Real-time price display**: Shows price when mouse hovers over items
- **Complete multilingual support**: Supports all currencies and scarabs for Chinese client
- **Intelligent mapping**: 100% coverage name mapping system
- **Performance optimization**: 100ms throttling mechanism, memory-efficient indexing

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
    
    // Only use Metadata path queries
    if (_itemMappingService?.IsInitialized == true)
    {
        return _itemMappingService.GetEnglishName(path);
    }
    
    return string.Empty;
}
```
## Mapping Coverage

### Current Status
- **Map Fragments**: 93 items (100% coverage) ✅
- **Essence**: 105 items (100% coverage) ✅  
- **Currency**: 574 currencies and fragments (100% coverage) ✅
- **Scarab**: 195 scarabs (100% coverage) ✅
- **Delirium Orbs**: 22 items (100% coverage) ✅
- **Vials**: 9 items (100% coverage) ✅
- **Blessings**: 5 items (100% coverage) ✅
- **Breach Splinters**: 13 items (100% coverage) ✅
- **Oils**: 16 items (100% coverage) ✅
- **Fossils**: 25 items (100% coverage) ✅
- **Catalysts**: 11 items (100% coverage) ✅
- **Total**: 781 items with complete Chinese-English mapping

### JSON Data Structure
All mapping data stored in `json/` directory:
- `fragments.json`: Map device recipe fragments
- `essence.json`: Essence league items
- `currency.json`: Currency mapping data
- `scarab.json`: Scarab mapping data
- `delirium.json`: Delirium orb items
- `vial.json`: Vial items
- `blessing.json`: Blessing items
- `breach.json`: Breach splinter items
- `oil.json`: Oil items
- `fossil.json`: Fossil items
- `catalyst.json`: Catalyst items

Each JSON item contains:
```json
{
  "name_zh": "Chinese Display Name",
  "name_en": "English Display Name",
  "base_type_zh": "Chinese Base Type", 
  "base_type_en": "English Base Type",
  "type": "Metadata/Items/Path/To/Item",
  "url": "https://poedb.tw/tw/item_reference"
}
```

## Common Development Tasks

### Adding New Item Categories

1. **Prepare JSON data**: Create new item mapping JSON file
2. **Register category**: Add new category in `UniversalItemMappingService.Initialize()`
3. **Test mapping**: Verify new items' hover functionality works correctly

### Extending Mapping Coverage

1. **Get Metadata path**: Use debug mode to record new item paths
2. **Create mapping entries**: Add mapping to corresponding JSON file
3. **Verify functionality**: Test new items' name identification and price queries

### UI Customization
- ImGui tables support sorting, resizing, filtering
- Use `ImGuiTableFlags` to control table behavior
- Color themes set through `ImGui.PushStyleColor`
- Hover tooltips support translucent background and adaptive sizing

## Universal PoeDB Scraper System

### Overview
A flexible, configuration-driven scraper system that can crawl multiple PoeDB item categories using a single universal program. This eliminates the need for separate Python scripts for each item category.

### Architecture
- **Universal Scraper**: `scraper/universal_poedb_scraper.py`
- **Configuration File**: `scraper/scraper_configs.json`
- **Output Directory**: `json/` (standardized JSON format)

### Supported Categories

Currently configured for 11 item categories:

| Category | Items Count | Status | Output File |
|----------|-------------|---------|-------------|
| Map Fragments | 93 items | ✅ Complete & Integrated | `fragments.json` |
| Essence | 105 items | ✅ Complete & Integrated | `essence.json` |
| Currency | 574 items | ✅ Complete & Integrated | `currency.json` |
| Scarabs | 195 items | ✅ Complete & Integrated | `scarab.json` |
| Delirium Orbs | 22 items | ✅ Complete & Integrated | `delirium.json` |
| Vials | 9 items | ✅ Complete & Integrated | `vial.json` |
| Blessings | 5 items | ✅ Complete & Integrated | `blessing.json` |
| Breach Splinters | 13 items | ✅ Complete & Integrated | `breach.json` |
| Oils | 16 items | ✅ Complete & Integrated | `oil.json` |
| Fossils | 25 items | ✅ Complete & Integrated | `fossil.json` |
| Catalysts | 11 items | ✅ Complete & Integrated | `catalyst.json` |

### Usage

**List available categories:**
```bash
cd scraper
python universal_poedb_scraper.py --list
```

**Scrape single category:**
```bash
python universal_poedb_scraper.py --category essence
```

**Scrape multiple categories:**
```bash
python universal_poedb_scraper.py --category essence fossil oil
```

**Scrape all configured categories:**
```bash
python universal_poedb_scraper.py --all
```

### Configuration Format

Each category requires configuration in `scraper_configs.json`:

```json
{
  "category_name": {
    "base_url": "https://poedb.tw/tw/PageName#SectionID",
    "container_selector": "#SectionID > div > div",
    "link_selector": "div > div.flex-shrink-0 > a",
    "output_file": "../json/output.json",
    "category_name": "Display Name"
  }
}
```

### Features

1. **Unified Architecture**: One scraper handles all PoeDB categories
2. **Configuration-Driven**: Add new categories by modifying JSON config
3. **Progress Management**: Auto-saves progress every 10 items
4. **Error Handling**: Robust timeout and retry mechanisms  
5. **Standardized Output**: All outputs follow the same JSON schema
6. **Intelligent Parsing**: Adapts to both table and div-based page structures

### Adding New Categories

1. **Analyze Target Page**: Identify container and link selectors
2. **Update Configuration**: Add new entry to `scraper_configs.json`
3. **Test Scraping**: Run `python universal_poedb_scraper.py --category new_category`
4. **Register in Mapping Service**: Add to `UniversalItemMappingService.Initialize()`

### Example Configuration Entry

```json
"new_category": {
  "base_url": "https://poedb.tw/tw/ItemPage#SectionName",
  "container_selector": "#SectionName > div > div",
  "link_selector": "div > div.flex-shrink-0 > a",
  "output_file": "../json/new_category.json",
  "category_name": "New Category Name"
}
```

### Output JSON Schema

All scraped data follows this standardized format:

```json
[
  {
    "name_zh": "Chinese Display Name",
    "name_en": "English Display Name", 
    "base_type_zh": "Chinese Base Type",
    "base_type_en": "English Base Type",
    "type": "Metadata/Items/Path/To/Item",
    "url": "https://poedb.tw/tw/item_page"
  }
]
```

## Future Roadmap

### Item Categories Ready for Support
With the Universal Scraper System, the following categories can be easily added:
- **Fossil**: ~30 items (Configuration ready)
- **Oil**: ~12 items (Configuration ready)
- **Catalyst**: ~10 items (Configuration ready)
- **Delirium Orbs**: ~50 items (Configuration ready)
- **Vials**: ~30 items (Configuration ready)
- **Blessings**: ~20 items (Configuration ready)
- **Breach Splinters**: ~15 items (Configuration ready)
- **Beast**: ~100 items
- **Prophecy**: ~80 items
- **Divination Cards**: ~500 items
- **Unique Items**: ~1000+ items

### Architecture Advantages
- **One-line extension**: New categories require only one line of registration code
- **Unified maintenance**: All categories share query logic
- **Performance scaling**: Index system supports large-scale item libraries
- **Memory efficiency**: Lazy loading and LRU caching mechanisms