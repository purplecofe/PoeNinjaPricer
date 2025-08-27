using System.Linq;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace PoeNinjaPricer;

public class PoeNinjaPricerSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);

    public HotkeyNodeV2 TogglePriceWindow { get; set; } = Keys.F8;
    public RangeNode<int> UpdateIntervalMinutes { get; set; } = new(5, 1, 30);
    public ToggleNode ShowChaosValues { get; set; } = new(true);
    public ToggleNode ShowDivineValues { get; set; } = new(true);
    public ToggleNode ShowPriceChanges { get; set; } = new(true);
    public ToggleNode AutoUpdatePrices { get; set; } = new(true);
    [Menu("League Name (leave blank for auto-detection)")]
    public TextNode LeagueName { get; set; } = new("");
    
    public RangeNode<int> WindowPosX { get; set; } = new(100, 0, 2000);
    public RangeNode<int> WindowPosY { get; set; } = new(100, 0, 2000);
    public RangeNode<int> WindowWidth { get; set; } = new(600, 300, 1200);
    public RangeNode<int> WindowHeight { get; set; } = new(400, 200, 800);
    
    
    // Hover item price display settings
    public ToggleNode EnableHoverPricing { get; set; } = new(true);
    
    // Debug settings
    public ToggleNode EnableDebugLogging { get; set; } = new(false);
}