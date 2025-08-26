# PoeNinja Pricer Plugin

An ExileCore plugin for querying Path of Exile currency prices using the poe.ninja API.

## Features

- **Real-time Price Queries**: Fetch current league currency and fragment prices from poe.ninja
- **Auto Update**: Configurable auto-update interval (default 5 minutes)
- **Smart Caching**: Local cache system to reduce API requests and support offline usage
- **Advanced Filter System**: Multiple category filters with quick preset options
- **Search Filtering**: Quick search for specific currencies
- **Dynamic Divine Rate**: Auto-calculated and updated Divine Orb exchange rates
- **Customizable UI**: Adjustable display columns and window sizing

## Usage

1. Press **F8** to toggle the price window
2. Use category checkboxes to filter different item types (currency, fragments, scarabs, etc.)
3. Use quick filter buttons:
   - **Select All/None**: Toggle all filters quickly
   - **High Value**: Show only valuable items (≥1c)
   - **Currency Only**: Show only basic currencies
   - **Common Items**: Show commonly traded items
4. Enter item names in the search box for instant filtering
5. Drag the "Min Value" slider to set value threshold
6. Click "Refresh" to manually update prices
7. Adjust basic settings in F12 panel (update interval, display options, etc.)

## Configuration Options

**Main Settings (F12 Settings Panel)**:
- **Toggle Price Window**: Hotkey to open price window (default F8)
- **Update Interval**: Auto-update interval (1-30 minutes, default 5 minutes)
- **League Name**: League name (leave empty for auto-detection, default Mercenaries)
- **Show Chaos/Divine Values**: Display Chaos/Divine value columns
- **Show Price Changes**: Display 24-hour price changes
- **Auto Update Prices**: Enable automatic updates
- **Language**: Interface language selection
- **Window Position/Size**: Window position and size settings

**Filter Controls (Main Window Only)**:
- Item category display toggles (not shown in F12 settings)
- Minimum value filter slider
- Quick preset filter buttons

## Supported Data Types

**Main Categories**:
- **Currency**: Basic currencies (Chaos Orb, Exalted Orb, Divine Orb, etc.)
- **Fragments**: Map fragments and other fragment items
- **Divination Cards**: Divination cards
- **Oils**: Oils (for enchanting)
- **Incubators**: Incubators

**Atlas Related**:
- **Scarabs**: Scarabs
- **Delirium Orbs**: Delirium orbs
- **Invitations**: Invitations
- **Memories**: Memories

**Crafting Related**:
- **Fossils**: Fossils
- **Resonators**: Resonators
- **Essences**: Essences
- **Beasts**: Beasts
- **Vials**: Vials

## Build Guide

Ensure environment variable is set:
```bash
setx exapiPackage "C:\Users\user\Downloads\ExileApi-Compiled-3.26.last"
```

Run in plugin directory:
```bash
dotnet build
```

## Troubleshooting

1. **Cannot fetch price data**: Check network connection and firewall settings
2. **Wrong league name**: Manually specify the correct league name in settings
3. **Plugin load failure**: Check ExileCore log files for error messages

## Technical Information

- **Framework**: ExileCore (ExileAPI)
- **UI**: ImGui.NET
- **HTTP**: .NET HttpClient
- **Serialization**: Newtonsoft.Json
- **Data Source**: poe.ninja API

## Version History

- **v1.1.0** (2025-08-27): Filter System Redesign
  - Redesigned filter system, removed complex options from F12 settings
  - Fully integrated filter controls into main window
  - Added quick preset filters (High Value, Currency Only, Common Items)
  - Improved user interface and experience
  
- **v1.0.0**: Initial Implementation
  - Currency and fragment price queries
  - Auto-update and cache system
  - Basic UI and search functionality