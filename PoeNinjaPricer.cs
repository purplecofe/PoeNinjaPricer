using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Cache;
using PoeNinjaPricer.Models;
using PoeNinjaPricer.Services;
using ImGuiNET;
using System.Numerics;
using System.Linq;
using System.IO;

namespace PoeNinjaPricer;

public class PoeNinjaPricer : BaseSettingsPlugin<PoeNinjaPricerSettings>
{
    private PoeNinjaApiService _apiService;
    private PriceCacheService _cacheService;
    private FilterManager _filterManager;
    private UniversalItemMappingService _itemMappingService;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _showPriceWindow = false;
    private bool _isUpdating = false;
    private string _searchFilter = "";
    private List<CurrencyPrice> _displayPrices = new();
    private List<CurrencyPrice> _allPrices = new();
    private string _currentLeague = "";
    private string _errorMessage = "";
    private readonly CachedValue<string> _gameLeague;
    
    // Hover item price display variables
    private Entity _lastHoveredItem;
    private CurrencyPrice _hoveredItemPrice;
    private DateTime _lastHoverCheck = DateTime.MinValue;
    
    // Filter states (no longer in settings to hide from F12 menu)
    // Default: Currency Only preset
    private bool _showCurrency = true;
    private bool _showFragments = false;
    private bool _showUniqueIdols = false;
    private bool _showRunecrafts = false;
    private bool _showAllflameEmbers = false;
    private bool _showTattoos = false;
    private bool _showOmens = false;
    private bool _showDivinationCards = false;
    private bool _showArtifacts = false;
    private bool _showOils = false;
    private bool _showIncubators = false;
    private bool _showUniqueWeapons = false;
    private bool _showUniqueArmours = false;
    private bool _showUniqueAccessories = false;
    private bool _showUniqueFlasks = false;
    private bool _showUniqueJewels = false;
    private bool _showUniqueTinctures = false;
    private bool _showUniqueRelics = false;
    private bool _showSkillGems = false;
    private bool _showClusterJewels = false;
    private bool _showMaps = false;
    private bool _showBlightedMaps = false;
    private bool _showBlightRavagedMaps = false;
    private bool _showUniqueMaps = false;
    private bool _showDeliriumOrbs = false;
    private bool _showInvitations = false;
    private bool _showScarabs = false;
    private bool _showMemories = false;
    private bool _showBaseTypes = false;
    private bool _showFossils = false;
    private bool _showResonators = false;
    private bool _showBeasts = false;
    private bool _showEssences = false;
    private bool _showVials = false;
    private bool _showOthers = false;

    public PoeNinjaPricer()
    {
        Name = "PoeNinja Pricer";
        _gameLeague = new TimeCache<string>(() => DetectCurrentLeague(), 5000);
    }

    public override bool Initialise()
    {
        try
        {
            // Initialize localization
            
            // 偵測聯盟
            _currentLeague = GetCurrentLeague();
            if (Settings.EnableDebugLogging.Value)
                DebugWindow.LogMsg($"PoeNinjaPricer: Using league '{_currentLeague}'");

            // 初始化服務
            _apiService = new PoeNinjaApiService(_currentLeague, () => Settings.EnableDebugLogging.Value);
            _cacheService = new PriceCacheService(DirectoryFullName, () => Settings.EnableDebugLogging.Value);
            _filterManager = new FilterManager(Settings);
            _filterManager.SetActiveCategoriesProvider(GetCurrentActiveCategories);
            _filterManager.SetAllCategoriesProvider(SetAllFilterCategories);
            
            // 初始化通用物品映射服務
            _itemMappingService = new UniversalItemMappingService();
            // 對於 Source 插件，直接使用源碼目錄路徑
            var sourceDirectory = Path.Combine(Environment.CurrentDirectory, "Plugins", "Source", "PoeNinjaPricer");
            _itemMappingService.Initialize(sourceDirectory);

            // 註冊熱鍵
            Input.RegisterKey(Settings.TogglePriceWindow.Value);
            Settings.TogglePriceWindow.OnValueChanged += () => Input.RegisterKey(Settings.TogglePriceWindow.Value);

            // 載入快取資料
            LoadCachedPrices();

            // 如果沒有快取或快取過期，立即更新一次
            if (!_cacheService.HasValidCache || _cacheService.IsCacheExpired(Settings.UpdateIntervalMinutes.Value))
            {
                _ = Task.Run(UpdatePricesAsync);
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Initialization failed - {ex.Message}");
            _errorMessage = $"Initialization failed: {ex.Message}";
            return false;
        }
    }

    public override Job Tick()
    {
        try
        {
            // 處理熱鍵
            if (Settings.TogglePriceWindow.PressedOnce())
            {
                _showPriceWindow = !_showPriceWindow;
            }

            // 自動更新價格
            if (Settings.AutoUpdatePrices.Value && !_isUpdating)
            {
                var timeSinceLastUpdate = DateTime.Now - _lastUpdateTime;
                if (timeSinceLastUpdate.TotalMinutes >= Settings.UpdateIntervalMinutes.Value)
                {
                    _ = Task.Run(UpdatePricesAsync);
                }
            }

            var currentLeague = GetCurrentLeague();
            if (_currentLeague != currentLeague)
            {
                if (Settings.EnableDebugLogging.Value)
                    DebugWindow.LogMsg($"PoeNinjaPricer: League changed from '{_currentLeague}' to '{currentLeague}'");
                _currentLeague = currentLeague;
                _apiService = new PoeNinjaApiService(_currentLeague, () => Settings.EnableDebugLogging.Value);
                _ = Task.Run(UpdatePricesAsync);
            }


            return null;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Tick error - {ex.Message}");
            return null;
        }
    }

    public override void Render()
    {
        if (!Settings.Enable.Value) return;

        try
        {
            // Always check for hover item pricing if enabled
            CheckHoveredItem();
            RenderHoveredItemPrice();
            
            // Only render main window if visible
            if (_showPriceWindow)
            {
                RenderPriceWindow();
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Render error - {ex.Message}");
        }
    }

    private void RenderPriceWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(Settings.WindowWidth.Value, Settings.WindowHeight.Value), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(Settings.WindowPosX.Value, Settings.WindowPosY.Value), ImGuiCond.FirstUseEver);

        var windowFlags = ImGuiWindowFlags.None;
        if (!ImGui.Begin($"PoeNinja Price Checker##PoeNinjaPricer", ref _showPriceWindow, windowFlags))
        {
            ImGui.End();
            return;
        }

        // 儲存視窗位置和大小
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        Settings.WindowPosX.Value = (int)pos.X;
        Settings.WindowPosY.Value = (int)pos.Y;
        Settings.WindowWidth.Value = (int)size.X;
        Settings.WindowHeight.Value = (int)size.Y;

        // 頂部工具列
        RenderToolbar();

        ImGui.Separator();

        // 篩選器控制
        RenderFilterControls();
        
        // 搜尋框
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Search Currency", ref _searchFilter, 100))
        {
            FilterPrices();
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            _ = Task.Run(UpdatePricesAsync);
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Cache"))
        {
            _cacheService.ClearCache();
            _allPrices.Clear();
            _displayPrices.Clear();
            _errorMessage = "";
        }

        // 錯誤訊息
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
            ImGui.TextWrapped(_errorMessage);
            ImGui.PopStyleColor();
        }

        // 狀態資訊
        var filterSummary = _filterManager?.GetFilterSummary(_allPrices, _displayPrices) ?? $"項目: {_displayPrices?.Count ?? 0}";
        var statusText = $"League: {_currentLeague} | {filterSummary}";
        if (_lastUpdateTime != DateTime.MinValue)
        {
            statusText += $" | Last update: {_lastUpdateTime:HH:mm:ss}";
        }
        else
        {
            statusText += $" | Not updated yet";
        }
        
        if (_isUpdating)
        {
            statusText += $" | Updating...";
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0.3f, 1)); // 黃色
        }
        
        ImGui.Text(statusText);
        
        if (_isUpdating)
        {
            ImGui.PopStyleColor();
        }

        // Display Divine Orb rate
        ImGui.SameLine();
        ImGui.Text($"| Divine: {CurrencyPrice.GetDivineRate():F1}c");

        ImGui.Separator();

        // 價格表格
        RenderPriceTable();

        ImGui.End();
    }

    private void RenderFilterControls()
    {
        // GENERAL category filters
        ImGui.Text("GENERAL:");
        ImGui.SameLine();
        
        if (ImGui.Checkbox("Currency", ref _showCurrency))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Fragments", ref _showFragments))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Divination Cards", ref _showDivinationCards))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Oils", ref _showOils))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Incubators", ref _showIncubators))
        {
            FilterPrices();
        }
        
        // ATLAS category filters
        ImGui.Text("ATLAS:");
        ImGui.SameLine();
        
        if (ImGui.Checkbox("Scarabs", ref _showScarabs))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Delirium Orbs", ref _showDeliriumOrbs))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Invitations", ref _showInvitations))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Memories", ref _showMemories))
        {
            FilterPrices();
        }
        
        // CRAFTING category filters
        ImGui.Text("CRAFT:");
        ImGui.SameLine();
        
        if (ImGui.Checkbox("Fossils", ref _showFossils))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Resonators", ref _showResonators))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Essences", ref _showEssences))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Beasts", ref _showBeasts))
        {
            FilterPrices();
        }
        
        ImGui.SameLine();
        if (ImGui.Checkbox("Vials", ref _showVials))
        {
            FilterPrices();
        }
        
        // Other categories
        ImGui.Text("Others:");
        ImGui.SameLine();
        
        if (ImGui.Checkbox("Others", ref _showOthers))
        {
            FilterPrices();
        }
        
        // 快速切換按鈕
        ImGui.Separator();
        
        if (ImGui.Button("Select All/None"))
        {
            if (_filterManager != null)
            {
                var currentCategories = _filterManager.GetActiveCategories();
                var hasAnyEnabled = currentCategories != CurrencyCategory.None;
                _filterManager.SetAllCategories(!hasAnyEnabled);
                FilterPrices();
            }
        }
        
    }

    private void RenderToolbar()
    {
        ImGui.Text("Options:");
        ImGui.SameLine();
        
        ImGui.SameLine();
        
        var showChaos = Settings.ShowChaosValues.Value;
        if (ImGui.Checkbox("Chaos", ref showChaos))
        {
            Settings.ShowChaosValues.Value = showChaos;
        }

        ImGui.SameLine();
        var showDivine = Settings.ShowDivineValues.Value;
        if (ImGui.Checkbox("Divine", ref showDivine))
        {
            Settings.ShowDivineValues.Value = showDivine;
        }

        ImGui.SameLine();
        var showChanges = Settings.ShowPriceChanges.Value;
        if (ImGui.Checkbox("Price Change", ref showChanges))
        {
            Settings.ShowPriceChanges.Value = showChanges;
        }
    }

    private void RenderPriceTable()
    {
        if (_displayPrices.Count == 0)
        {
            ImGui.Text("No price data. Click 'Refresh' to load prices.");
            return;
        }

        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
        
        var columnCount = 2;
        if (Settings.ShowChaosValues.Value) columnCount++;
        if (Settings.ShowDivineValues.Value) columnCount++;
        if (Settings.ShowPriceChanges.Value) columnCount++;

        if (!ImGui.BeginTable("PriceTable", columnCount, tableFlags)) return;

        // 表頭設定（支援所有欄位排序）
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 120);
        if (Settings.ShowChaosValues.Value)
            ImGui.TableSetupColumn("Chaos", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);
        if (Settings.ShowDivineValues.Value)
            ImGui.TableSetupColumn("Divine", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);
        if (Settings.ShowPriceChanges.Value)
            ImGui.TableSetupColumn("24h Change", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);

        ImGui.TableHeadersRow();

        // 排序
        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty)
        {
            SortPrices(sortSpecs);
        }

        // 資料列
        foreach (var price in _displayPrices)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(price.Name);

            // 類型
            ImGui.TableSetColumnIndex(1);
            ImGui.Text(price.Category.GetDisplayName());

            var colIndex = 2;

            // Chaos 價值
            if (Settings.ShowChaosValues.Value)
            {
                ImGui.TableSetColumnIndex(colIndex++);
                ImGui.Text(price.GetFormattedChaosValue());
            }

            // Divine 價值
            if (Settings.ShowDivineValues.Value)
            {
                ImGui.TableSetColumnIndex(colIndex++);
                if (price.ChaosValue >= 1)
                    ImGui.Text(price.GetFormattedDivineValue());
                else
                    ImGui.Text("-");
            }

            // 24h 變化
            if (Settings.ShowPriceChanges.Value)
            {
                ImGui.TableSetColumnIndex(colIndex++);
                if (price.Change24h != 0)
                {
                    var color = price.Change24h > 0 ? new Vector4(0.3f, 1, 0.3f, 1) : new Vector4(1, 0.3f, 0.3f, 1);
                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    ImGui.Text($"{price.Change24h:+0.0;-0.0;0}%");
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.Text("-");
                }
            }
        }

        ImGui.EndTable();
    }

    private void SortPrices(ImGuiTableSortSpecsPtr sortSpecs)
    {
        var columnIndex = sortSpecs.Specs.ColumnIndex;
        var ascending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

        // 根據當前顯示的欄位計算實際欄位索引
        var currentColumnIndex = 0;
        
        if (columnIndex == 0)
        {
            _displayPrices = ascending
                ? _displayPrices.OrderBy(p => p.Name).ToList()
                : _displayPrices.OrderByDescending(p => p.Name).ToList();
        }
        else if (columnIndex == 1) // 類型
        {
            _displayPrices = ascending
                ? _displayPrices.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList()
                : _displayPrices.OrderByDescending(p => p.Category).ThenBy(p => p.Name).ToList();
        }
        else
        {
            // 動態判斷其他欄位
            currentColumnIndex = 2; // 從第三欄開始
            
            if (Settings.ShowChaosValues.Value && columnIndex == currentColumnIndex) // Chaos
            {
                _displayPrices = ascending
                    ? _displayPrices.OrderBy(p => p.ChaosValue).ToList()
                    : _displayPrices.OrderByDescending(p => p.ChaosValue).ToList();
            }
            else
            {
                if (Settings.ShowChaosValues.Value) currentColumnIndex++;
                
                if (Settings.ShowDivineValues.Value && columnIndex == currentColumnIndex) // Divine
                {
                    _displayPrices = ascending
                        ? _displayPrices.OrderBy(p => p.DivineValue).ToList()
                        : _displayPrices.OrderByDescending(p => p.DivineValue).ToList();
                }
                else
                {
                    if (Settings.ShowDivineValues.Value) currentColumnIndex++;
                    
                    if (Settings.ShowPriceChanges.Value && columnIndex == currentColumnIndex) // 24h變化
                    {
                        _displayPrices = ascending
                            ? _displayPrices.OrderBy(p => p.Change24h).ToList()
                            : _displayPrices.OrderByDescending(p => p.Change24h).ToList();
                    }
                }
            }
        }

        sortSpecs.SpecsDirty = false;
    }

    private async Task UpdatePricesAsync()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        _errorMessage = "";

        try
        {
            if (Settings.EnableDebugLogging.Value)
                DebugWindow.LogMsg("PoeNinjaPricer: Starting price update");
            var prices = await _apiService.GetCurrencyPricesAsync();
            
            _cacheService.UpdateCache(prices);
            _lastUpdateTime = DateTime.Now;
            
            _allPrices = prices;
            FilterPrices();
            if (Settings.EnableDebugLogging.Value)
                DebugWindow.LogMsg($"PoeNinjaPricer: Price update completed with {prices.Count} items");
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Price update failed - {ex.Message}");
            _errorMessage = $"Price update failed: {ex.Message}";
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void LoadCachedPrices()
    {
        _allPrices = _cacheService.GetCachedPrices();
        _lastUpdateTime = _cacheService.LastUpdateTime;
        FilterPrices();
    }

    private void FilterPrices()
    {
        if (_allPrices == null)
        {
            _displayPrices = new List<CurrencyPrice>();
            return;
        }

        var searchFiltered = string.IsNullOrWhiteSpace(_searchFilter)
            ? _allPrices
            : _allPrices.Where(p => p != null && !string.IsNullOrEmpty(p.Name) && 
                p.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        
        if (_filterManager != null)
        {
            _displayPrices = _filterManager.ApplyFilters(searchFiltered);
        }
        else
        {
            _displayPrices = searchFiltered;
        }
        
        // 最後按 Chaos 價值降序排序
        if (_displayPrices != null)
        {
            _displayPrices = _displayPrices.OrderByDescending(p => p?.ChaosValue ?? 0).ToList();
        }
        else
        {
            _displayPrices = new List<CurrencyPrice>();
        }
    }

    private string GetCurrentLeague()
    {
        // 優先使用設定中的聯盟名稱
        if (!string.IsNullOrWhiteSpace(Settings.LeagueName.Value))
        {
            return Settings.LeagueName.Value;
        }

        // 嘗試從遊戲記憶體偵測
        var detectedLeague = _gameLeague.Value;
        if (!string.IsNullOrWhiteSpace(detectedLeague))
        {
            return detectedLeague;
        }

        // 預設值
        return "Mercenaries";
    }

    private string DetectCurrentLeague()
    {
        try
        {
            // 嘗試從遊戲資料獲取聯盟名稱
            var game = GameController?.Game;
            if (game?.IngameState?.Data?.LocalPlayer != null)
            {
                // 這裡需要找到正確的聯盟名稱欄位
                // 目前先返回空字串，讓它使用預設值
                return "";
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Failed to detect league - {ex.Message}");
        }

        return "";
    }

    public override void DrawSettings()
    {
        ImGui.Text("PoeNinja Pricer");
        ImGui.Separator();

        base.DrawSettings();

        ImGui.Separator();
        if (ImGui.Button("Update Now"))
        {
            _ = Task.Run(UpdatePricesAsync);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Clear Cache"))
        {
            _cacheService?.ClearCache();
            _allPrices.Clear();
            _displayPrices.Clear();
            _errorMessage = "";
        }

        ImGui.SameLine();
        if (ImGui.Button("Test Connection"))
        {
            _ = Task.Run(TestConnectionAsync);
        }

        if (!string.IsNullOrEmpty(_errorMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
            ImGui.TextWrapped(_errorMessage);
            ImGui.PopStyleColor();
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            _errorMessage = "Testing connection...";
            var result = await _apiService.TestConnectionAsync();
            _errorMessage = result ? "Connection test successful!" : "Connection test failed";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Connection test failed: {ex.Message}";
        }
    }

    
    private CurrencyCategory GetCurrentActiveCategories()
    {
        var activeCategories = CurrencyCategory.None;
        
        // GENERAL 類別
        if (_showCurrency)
            activeCategories |= CurrencyCategory.Currency;
        if (_showFragments)
            activeCategories |= CurrencyCategory.Fragments;
        if (_showUniqueIdols)
            activeCategories |= CurrencyCategory.UniqueIdols;
        if (_showRunecrafts)
            activeCategories |= CurrencyCategory.Runecrafts;
        if (_showAllflameEmbers)
            activeCategories |= CurrencyCategory.AllflameEmbers;
        if (_showTattoos)
            activeCategories |= CurrencyCategory.Tattoos;
        if (_showOmens)
            activeCategories |= CurrencyCategory.Omens;
        if (_showDivinationCards)
            activeCategories |= CurrencyCategory.DivinationCards;
        if (_showArtifacts)
            activeCategories |= CurrencyCategory.Artifacts;
        if (_showOils)
            activeCategories |= CurrencyCategory.Oils;
        if (_showIncubators)
            activeCategories |= CurrencyCategory.Incubators;
            
        // EQUIPMENT & GEMS 類別
        if (_showUniqueWeapons)
            activeCategories |= CurrencyCategory.UniqueWeapons;
        if (_showUniqueArmours)
            activeCategories |= CurrencyCategory.UniqueArmours;
        if (_showUniqueAccessories)
            activeCategories |= CurrencyCategory.UniqueAccessories;
        if (_showUniqueFlasks)
            activeCategories |= CurrencyCategory.UniqueFlasks;
        if (_showUniqueJewels)
            activeCategories |= CurrencyCategory.UniqueJewels;
        if (_showUniqueTinctures)
            activeCategories |= CurrencyCategory.UniqueTinctures;
        if (_showUniqueRelics)
            activeCategories |= CurrencyCategory.UniqueRelics;
        if (_showSkillGems)
            activeCategories |= CurrencyCategory.SkillGems;
        if (_showClusterJewels)
            activeCategories |= CurrencyCategory.ClusterJewels;
            
        // ATLAS 類別
        if (_showMaps)
            activeCategories |= CurrencyCategory.Maps;
        if (_showBlightedMaps)
            activeCategories |= CurrencyCategory.BlightedMaps;
        if (_showBlightRavagedMaps)
            activeCategories |= CurrencyCategory.BlightRavagedMaps;
        if (_showUniqueMaps)
            activeCategories |= CurrencyCategory.UniqueMaps;
        if (_showDeliriumOrbs)
            activeCategories |= CurrencyCategory.DeliriumOrbs;
        if (_showInvitations)
            activeCategories |= CurrencyCategory.Invitations;
        if (_showScarabs)
            activeCategories |= CurrencyCategory.Scarabs;
        if (_showMemories)
            activeCategories |= CurrencyCategory.Memories;
            
        // CRAFTING 類別
        if (_showBaseTypes)
            activeCategories |= CurrencyCategory.BaseTypes;
        if (_showFossils)
            activeCategories |= CurrencyCategory.Fossils;
        if (_showResonators)
            activeCategories |= CurrencyCategory.Resonators;
        if (_showBeasts)
            activeCategories |= CurrencyCategory.Beasts;
        if (_showEssences)
            activeCategories |= CurrencyCategory.Essences;
        if (_showVials)
            activeCategories |= CurrencyCategory.Vials;
            
        if (_showOthers)
            activeCategories |= CurrencyCategory.Others;
            
        return activeCategories;
    }
    
    private void SetAllFilterCategories(bool enabled)
    {
        _showCurrency = enabled;
        _showFragments = enabled;
        _showUniqueIdols = enabled;
        _showRunecrafts = enabled;
        _showAllflameEmbers = enabled;
        _showTattoos = enabled;
        _showOmens = enabled;
        _showDivinationCards = enabled;
        _showArtifacts = enabled;
        _showOils = enabled;
        _showIncubators = enabled;
        _showUniqueWeapons = enabled;
        _showUniqueArmours = enabled;
        _showUniqueAccessories = enabled;
        _showUniqueFlasks = enabled;
        _showUniqueJewels = enabled;
        _showUniqueTinctures = enabled;
        _showUniqueRelics = enabled;
        _showSkillGems = enabled;
        _showClusterJewels = enabled;
        _showMaps = enabled;
        _showBlightedMaps = enabled;
        _showBlightRavagedMaps = enabled;
        _showUniqueMaps = enabled;
        _showDeliriumOrbs = enabled;
        _showInvitations = enabled;
        _showScarabs = enabled;
        _showMemories = enabled;
        _showBaseTypes = enabled;
        _showFossils = enabled;
        _showResonators = enabled;
        _showBeasts = enabled;
        _showEssences = enabled;
        _showVials = enabled;
        _showOthers = enabled;
    }
    
    private void CheckHoveredItem()
    {
        // Skip if hover pricing is disabled
        if (!Settings.EnableHoverPricing.Value)
        {
            _hoveredItemPrice = null;
            _lastHoveredItem = null;
            return;
        }
        
        // Throttle checks to avoid performance issues
        if (DateTime.Now - _lastHoverCheck < TimeSpan.FromMilliseconds(100))
            return;
            
        _lastHoverCheck = DateTime.Now;
        
        try
        {
            var currentHoveredItem = GetHoveredItem();
            
            // Debug logging to see if we're getting hover items
            if (currentHoveredItem != _lastHoveredItem)
            {
                if (currentHoveredItem != null)
                {
                    var itemName = GetItemBaseName(currentHoveredItem);
                    var englishName = GetEnglishItemName(currentHoveredItem);
                    if (Settings.EnableDebugLogging.Value)
                        DebugWindow.LogMsg($"PoeNinjaPricer: Hovering item: {itemName} (Path: {currentHoveredItem.Path}) English: {englishName}");
                }
                else if (_lastHoveredItem != null)
                {
                    if (Settings.EnableDebugLogging.Value)
                        DebugWindow.LogMsg($"PoeNinjaPricer: No longer hovering item");
                }
            }
            
            // Check if hovered item changed
            if (currentHoveredItem != _lastHoveredItem)
            {
                _lastHoveredItem = currentHoveredItem;
                _hoveredItemPrice = null;
                
                if (currentHoveredItem != null)
                {
                    var itemName = GetEnglishItemName(currentHoveredItem);
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        _hoveredItemPrice = FindPriceByName(itemName);
                        if (_hoveredItemPrice != null)
                        {
                            if (Settings.EnableDebugLogging.Value)
                                DebugWindow.LogMsg($"PoeNinjaPricer: Found price for {itemName}: {_hoveredItemPrice.ChaosValue}c");
                        }
                        else
                        {
                            if (Settings.EnableDebugLogging.Value)
                                DebugWindow.LogMsg($"PoeNinjaPricer: No price found for {itemName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: CheckHoveredItem error - {ex.Message}");
        }
    }
    
    private Entity GetHoveredItem()
    {
        try
        {
            var gameController = GameController;
            if (gameController?.IngameState == null)
                return null;
                
            // Use the same approach as ItemFilterLibInspector
            var uiHover = gameController.IngameState.UIHover;
            if (uiHover == null || uiHover.Address == 0)
                uiHover = gameController.IngameState.UIHoverElement;
                
            if (uiHover == null || uiHover.Address == 0)
                return null;

            // Check if this is an item hover
            var hoverItemIcon = uiHover.AsObject<HoverItemIcon>();
            return hoverItemIcon?.Item;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: GetHoveredItem error - {ex.Message}");
            return null;
        }
    }
    
    private string GetItemBaseName(Entity item)
    {
        try
        {
            return GameController?.Files?.BaseItemTypes?.Translate(item?.Path)?.BaseName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private string GetEnglishItemName(Entity item)
    {
        try
        {
            var path = item?.Path;
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            
            // Use UniversalItemMappingService for metadata path lookup
            if (_itemMappingService?.IsInitialized == true)
            {
                var mappedName = _itemMappingService.GetEnglishName(path);
                if (!string.IsNullOrEmpty(mappedName))
                    return mappedName;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: GetEnglishItemName error - {ex.Message}");
            return string.Empty;
        }
    }
    
    
    
    
    
    private CurrencyPrice FindPriceByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || _allPrices == null)
            return null;
            
        // Try exact match first
        var exactMatch = _allPrices.FirstOrDefault(p => 
            string.Equals(p.Name, itemName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;
            
        // Try partial match for items that might have different variations
        var partialMatch = _allPrices.FirstOrDefault(p => 
            p.Name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0 ||
            itemName.IndexOf(p.Name, StringComparison.OrdinalIgnoreCase) >= 0);
            
        return partialMatch;
    }
    
    private void RenderHoveredItemPrice()
    {
        if (!Settings.EnableHoverPricing.Value)
            return;
            
        if (_hoveredItemPrice == null || _lastHoveredItem?.Address == 0)
            return;
            
        try
        {
            var mousePos = Input.MousePosition;
            ImGui.SetNextWindowPos(new Vector2(mousePos.X + 20, mousePos.Y), ImGuiCond.Always);
            
            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                       ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | 
                       ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking |
                       ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs;
            
            if (ImGui.Begin("##HoverPriceTooltip", flags))
            {
                // Background with slight transparency
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                
                ImGui.Text($"{_hoveredItemPrice.Name}");
                ImGui.Separator();
                
                if (Settings.ShowChaosValues.Value)
                {
                    ImGui.Text($"Chaos: {_hoveredItemPrice.GetFormattedChaosValue()}c");
                }
                
                if (Settings.ShowDivineValues.Value && _hoveredItemPrice.DivineValue >= 0.01)
                {
                    ImGui.Text($"Divine: {_hoveredItemPrice.GetFormattedDivineValue()}d");
                }
                
                if (Settings.ShowPriceChanges.Value && _hoveredItemPrice.Change24h != 0)
                {
                    var changeColor = _hoveredItemPrice.Change24h > 0 ? 
                        new Vector4(0f, 1f, 0f, 1f) : // Green for positive
                        new Vector4(1f, 0f, 0f, 1f);  // Red for negative
                        
                    ImGui.PushStyleColor(ImGuiCol.Text, changeColor);
                    ImGui.Text($"24h: {(_hoveredItemPrice.Change24h > 0 ? "+" : "")}{_hoveredItemPrice.Change24h:F1}%");
                    ImGui.PopStyleColor();
                }
                
                ImGui.PopStyleColor(2);
            }
            ImGui.End();
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: RenderHoveredItemPrice error - {ex.Message}");
        }
    }

    public override void OnClose()
    {
        _apiService?.Dispose();
    }
}