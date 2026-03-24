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

    public ToggleNode EnableFollowbot { get; set; } = new(false);
    public HotkeyNode MoveKey { get; set; } = new(System.Windows.Forms.Keys.T);
    public RangeNode<int> FollowDistance { get; set; } = new(30, 10, 150);

    public ToggleNode EnableAutoLink { get; set; } = new(false);
    public HotkeyNode LinkSkillKey { get; set; } = new(System.Windows.Forms.Keys.R);
    public RangeNode<int> MaxLinkDistance { get; set; } = new(60, 10, 100);
}
