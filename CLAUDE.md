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
- **Currency**: 574 currencies and fragments (`json/currency.json`)
- **Scarab**: 195 scarabs (`json/scarab.json`)
- **Future**: Can easily extend to 10+ other item categories

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
- **Currency**: 574 currencies and fragments (100% coverage)
- **Scarab**: 195 scarabs (100% coverage)
- **Total**: 769 items with complete Chinese-English mapping

### JSON Data Structure
All mapping data stored in `json/` directory:
- `currency.json`: Currency mapping data
- `scarab.json`: Scarab mapping data

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

## Future Roadmap

### Item Categories Ready for Support
- **Essence**: ~50 items
- **Fossil**: ~30 items  
- **Beast**: ~100 items
- **Prophecy**: ~80 items
- **Divination Cards**: ~500 items
- **Unique Items**: ~1000+ items

### Architecture Advantages
- **One-line extension**: New categories require only one line of registration code
- **Unified maintenance**: All categories share query logic
- **Performance scaling**: Index system supports large-scale item libraries
- **Memory efficiency**: Lazy loading and LRU caching mechanisms