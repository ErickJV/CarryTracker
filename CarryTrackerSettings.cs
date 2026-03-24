using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace CarryTracker;

public class CarryTrackerSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);
    public ToggleNode TrackAllPlayers { get; set; } = new(false);
    public TextNode CarryName { get; set; } = new("CarryNameHere");
    public RangeNode<float> WarningDuration { get; set; } = new(4.0f, 0.1f, 10.0f);
    public RangeNode<int> BoxThickness { get; set; } = new(5, 1, 10);
    public ColorNode ColorMissing { get; set; } = new(Color.Red);
    public ColorNode ColorActive { get; set; } = new(Color.Green);
    public ColorNode ColorWarning { get; set; } = new(Color.Blue);
    public ToggleNode DebugBuffs { get; set; } = new(false);
}
