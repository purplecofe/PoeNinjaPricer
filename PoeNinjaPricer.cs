using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using PoeNinjaPricer.Models;
using PoeNinjaPricer.Services;
using ImGuiNET;
using System.Numerics;
using System.Linq;

namespace PoeNinjaPricer;

public class PoeNinjaPricer : BaseSettingsPlugin<PoeNinjaPricerSettings>
{
    private PoeNinjaApiService _apiService;
    private PriceCacheService _cacheService;
    private FilterManager _filterManager;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _showPriceWindow = false;
    private bool _isUpdating = false;
    private string _searchFilter = "";
    private List<CurrencyPrice> _displayPrices = new();
    private List<CurrencyPrice> _allPrices = new();
    private string _currentLeague = "";
    private string _errorMessage = "";
    private readonly CachedValue<string> _gameLeague;

    public PoeNinjaPricer()
    {
        Name = "PoeNinja Pricer";
        _gameLeague = new TimeCache<string>(() => DetectCurrentLeague(), 5000);
    }

    public override bool Initialise()
    {
        try
        {
            // 偵測聯盟
            _currentLeague = GetCurrentLeague();
            DebugWindow.LogMsg($"PoeNinjaPricer: Using league '{_currentLeague}'");

            // 初始化服務
            _apiService = new PoeNinjaApiService(_currentLeague);
            _cacheService = new PriceCacheService(DirectoryFullName);
            _filterManager = new FilterManager(Settings);

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
            _errorMessage = $"初始化失敗: {ex.Message}";
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

            // 檢查聯盟變化
            var currentLeague = GetCurrentLeague();
            if (_currentLeague != currentLeague)
            {
                DebugWindow.LogMsg($"PoeNinjaPricer: League changed from '{_currentLeague}' to '{currentLeague}'");
                _currentLeague = currentLeague;
                _apiService = new PoeNinjaApiService(_currentLeague);
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
        if (!Settings.Enable.Value || !_showPriceWindow) return;

        try
        {
            RenderPriceWindow();
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

        // 篩選器控制和搜尋框
        RenderFilterControls();
        
        // 搜尋框
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("搜尋通貨", ref _searchFilter, 100))
        {
            FilterPrices();
        }

        ImGui.SameLine();
        if (ImGui.Button("重新整理"))
        {
            _ = Task.Run(UpdatePricesAsync);
        }

        ImGui.SameLine();
        if (ImGui.Button("清除快取"))
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
        var statusText = $"聯盟: {_currentLeague} | {filterSummary}";
        if (_lastUpdateTime != DateTime.MinValue)
        {
            statusText += $" | 最後更新: {_lastUpdateTime:HH:mm:ss}";
        }
        else
        {
            statusText += " | 尚未更新";
        }
        
        if (_isUpdating)
        {
            statusText += " | 更新中...";
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0.3f, 1)); // 黃色
        }
        
        ImGui.Text(statusText);
        
        if (_isUpdating)
        {
            ImGui.PopStyleColor();
        }

        // 顯示 Divine Orb 匯率
        ImGui.SameLine();
        ImGui.Text($"| Divine: {CurrencyPrice.GetDivineRate():F1}c");

        ImGui.Separator();

        // 價格表格
        RenderPriceTable();

        ImGui.End();
    }

    private void RenderFilterControls()
    {
        // 分類篩選器
        ImGui.Text("分類:");
        ImGui.SameLine();
        
        var showBasic = Settings.ShowBasicCurrency.Value;
        if (ImGui.Checkbox("基礎", ref showBasic))
        {
            Settings.ShowBasicCurrency.Value = showBasic;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showFragments = Settings.ShowFragments.Value;
        if (ImGui.Checkbox("碎片", ref showFragments))
        {
            Settings.ShowFragments.Value = showFragments;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showEssences = Settings.ShowEssences.Value;
        if (ImGui.Checkbox("精髓", ref showEssences))
        {
            Settings.ShowEssences.Value = showEssences;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showOils = Settings.ShowOils.Value;
        if (ImGui.Checkbox("聖油", ref showOils))
        {
            Settings.ShowOils.Value = showOils;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showScarabs = Settings.ShowScarabs.Value;
        if (ImGui.Checkbox("聖甲蟲", ref showScarabs))
        {
            Settings.ShowScarabs.Value = showScarabs;
            FilterPrices();
        }
        
        // 第二行篩選器
        ImGui.Text("     ");
        ImGui.SameLine();
        
        var showFossils = Settings.ShowFossils.Value;
        if (ImGui.Checkbox("化石", ref showFossils))
        {
            Settings.ShowFossils.Value = showFossils;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showCatalysts = Settings.ShowCatalysts.Value;
        if (ImGui.Checkbox("催化劑", ref showCatalysts))
        {
            Settings.ShowCatalysts.Value = showCatalysts;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showDelirium = Settings.ShowDeliriumOrbs.Value;
        if (ImGui.Checkbox("譫妄玉", ref showDelirium))
        {
            Settings.ShowDeliriumOrbs.Value = showDelirium;
            FilterPrices();
        }
        
        ImGui.SameLine();
        var showOthers = Settings.ShowOthers.Value;
        if (ImGui.Checkbox("其他", ref showOthers))
        {
            Settings.ShowOthers.Value = showOthers;
            FilterPrices();
        }
        
        // 快速切換按鈕
        ImGui.SameLine();
        if (ImGui.Button("全選/全不選"))
        {
            if (_filterManager != null)
            {
                var currentCategories = _filterManager.GetActiveCategories();
                var hasAnyEnabled = currentCategories != CurrencyCategory.None;
                _filterManager.SetAllCategories(!hasAnyEnabled);
                FilterPrices();
            }
        }
        
        ImGui.SameLine();
        if (ImGui.Button("高價值"))
        {
            if (_filterManager != null)
            {
                _filterManager.ApplyHighValueFilter();
                FilterPrices();
            }
        }
        
        ImGui.SameLine();
        if (ImGui.Button("僅基礎"))
        {
            if (_filterManager != null)
            {
                _filterManager.ApplyBasicCurrencyFilter();
                FilterPrices();
            }
        }
        
        // 最小價值滑桿
        ImGui.Text("最小價值:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        var minValue = Settings.MinChaosValue.Value;
        if (ImGui.SliderFloat("##MinChaosValue", ref minValue, 0f, 50f, "%.1fc"))
        {
            Settings.MinChaosValue.Value = minValue;
            FilterPrices();
        }
    }

    private void RenderToolbar()
    {
        ImGui.Text("選項: ");
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
        if (ImGui.Checkbox("變化", ref showChanges))
        {
            Settings.ShowPriceChanges.Value = showChanges;
        }
    }

    private void RenderPriceTable()
    {
        if (_displayPrices.Count == 0)
        {
            ImGui.Text("沒有價格資料。請點擊「重新整理」載入價格。");
            return;
        }

        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
        
        var columnCount = 2; // Name, Type
        if (Settings.ShowChaosValues.Value) columnCount++;
        if (Settings.ShowDivineValues.Value) columnCount++;
        if (Settings.ShowPriceChanges.Value) columnCount++;

        if (!ImGui.BeginTable("PriceTable", columnCount, tableFlags)) return;

        // 表頭設定（支援所有欄位排序）
        ImGui.TableSetupColumn("名稱", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("類型", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 60);
        if (Settings.ShowChaosValues.Value)
            ImGui.TableSetupColumn("Chaos", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);
        if (Settings.ShowDivineValues.Value)
            ImGui.TableSetupColumn("Divine", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);
        if (Settings.ShowPriceChanges.Value)
            ImGui.TableSetupColumn("24h變化", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 80);

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

            // 名稱
            ImGui.TableSetColumnIndex(0);
            ImGui.Text(price.Name);

            // 類型
            ImGui.TableSetColumnIndex(1);
            ImGui.Text(price.IsFragment ? "碎片" : "通貨");

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
        
        if (columnIndex == 0) // 名稱
        {
            _displayPrices = ascending
                ? _displayPrices.OrderBy(p => p.Name).ToList()
                : _displayPrices.OrderByDescending(p => p.Name).ToList();
        }
        else if (columnIndex == 1) // 類型
        {
            _displayPrices = ascending
                ? _displayPrices.OrderBy(p => p.IsFragment).ThenBy(p => p.Name).ToList()
                : _displayPrices.OrderByDescending(p => p.IsFragment).ThenBy(p => p.Name).ToList();
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
            DebugWindow.LogMsg("PoeNinjaPricer: Starting price update");
            var prices = await _apiService.GetCurrencyPricesAsync();
            
            _cacheService.UpdateCache(prices);
            _lastUpdateTime = DateTime.Now;
            
            _allPrices = prices;
            FilterPrices();
            DebugWindow.LogMsg($"PoeNinjaPricer: Price update completed with {prices.Count} items");
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"PoeNinjaPricer: Price update failed - {ex.Message}");
            _errorMessage = $"價格更新失敗: {ex.Message}";
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

        // 先應用搜尋過濾
        var searchFiltered = string.IsNullOrWhiteSpace(_searchFilter)
            ? _allPrices
            : _allPrices.Where(p => p != null && !string.IsNullOrEmpty(p.Name) && 
                p.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        
        // 再應用分類和價值過濾
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
        ImGui.Text("PoeNinja 通貨查價器設定");
        ImGui.Separator();

        base.DrawSettings();

        ImGui.Separator();
        if (ImGui.Button("立即更新價格"))
        {
            _ = Task.Run(UpdatePricesAsync);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("清除快取"))
        {
            _cacheService?.ClearCache();
            _allPrices.Clear();
            _displayPrices.Clear();
            _errorMessage = "";
        }

        ImGui.SameLine();
        if (ImGui.Button("測試連線"))
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
            _errorMessage = "測試連線中...";
            var result = await _apiService.TestConnectionAsync();
            _errorMessage = result ? "連線測試成功！" : "連線測試失敗";
        }
        catch (Exception ex)
        {
            _errorMessage = $"連線測試失敗: {ex.Message}";
        }
    }

    public override void OnClose()
    {
        _apiService?.Dispose();
    }
}