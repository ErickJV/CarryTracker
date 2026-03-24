using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;

namespace CarryTracker;

public class CarryTrackerCore : BaseSettingsPlugin<CarryTrackerSettings>
{
    public override bool Initialise() => true;

    public override void Render()
    {
        if (!Settings.Enable.Value)
            return;

        var carryName = Settings.CarryName.Value;
        var trackAll = Settings.TrackAllPlayers.Value;

        if (!trackAll && string.IsNullOrWhiteSpace(carryName))
            return;

        var localPlayer = GameController.Game.IngameState.Data.LocalPlayer;
        if (localPlayer == null)
            return;

        var trackedEntities = GetTrackedEntities(trackAll, carryName, localPlayer).ToList();

        if (trackedEntities.Count == 0)
            return;

        if (Settings.DebugBuffs.Value)
        {
            DrawDebugBuffs(localPlayer, trackedEntities);
        }

        DrawTrackingMarkers(localPlayer, trackedEntities);
    }

    /// <summary>
    /// Retrieves all player entities matching the tracking criteria, excluding the local player.
    /// </summary>
    private IEnumerable<Entity> GetTrackedEntities(bool trackAll, string carryName, Entity localPlayer)
    {
        return GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player]
            .Where(entity => entity != localPlayer)
            .Where(entity => trackAll || 
                string.Equals(entity.GetComponent<Player>()?.PlayerName, carryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Renders debug information for buffs active on the local player and all tracked entities.
    /// </summary>
    private void DrawDebugBuffs(Entity localPlayer, IEnumerable<Entity> trackedEntities)
    {
        var startY = 150f;
        var localBuffs = localPlayer.GetComponent<Buffs>()?.BuffsList;

        if (localBuffs != null)
        {
            Graphics.DrawText("--- Local Player Buffs ---", new Vector2(20, startY), Color.Yellow);
            startY += 20f;
            foreach (var b in localBuffs)
            {
                Graphics.DrawText($"{b.Name} : {b.Timer:F1}s", new Vector2(20, startY), Color.White);
                startY += 20f;
            }
        }

        var startX = 300f;
        foreach (var targetEntity in trackedEntities)
        {
            startY = 150f;
            var targetBuffs = targetEntity.GetComponent<Buffs>()?.BuffsList;
            if (targetBuffs == null) continue;

            var playerName = targetEntity.GetComponent<Player>()?.PlayerName ?? "Target";
            Graphics.DrawText($"--- {playerName} Buffs ---", new Vector2(startX, startY), Color.Yellow);
            startY += 20f;

            foreach (var b in targetBuffs)
            {
                Graphics.DrawText($"{b.Name} : {b.Timer:F1}s", new Vector2(startX, startY), Color.White);
                startY += 20f;
            }
            startX += 200f;
        }
    }

    /// <summary>
    /// Evaluates soul link uptime and renders the visual tracking markers for all valid entities.
    /// </summary>
    private void DrawTrackingMarkers(Entity localPlayer, IEnumerable<Entity> trackedEntities)
    {
        var localBuffs = localPlayer.GetComponent<Buffs>()?.BuffsList;

        foreach (var carryEntity in trackedEntities)
        {
            if (!carryEntity.IsAlive) continue;

            var carryBuffs = carryEntity.GetComponent<Buffs>()?.BuffsList;
            var (hasBuff, timeLeft) = CalculateSoulLinkStatus(localBuffs, carryBuffs);

            var currentColor = Settings.ColorMissing.Value;
            if (hasBuff)
            {
                currentColor = timeLeft <= Settings.WarningDuration.Value
                    ? Settings.ColorWarning.Value
                    : Settings.ColorActive.Value;
            }

            RenderEntityMarker(carryEntity, hasBuff, timeLeft, currentColor);
        }
    }

    /// <summary>
    /// Determines if the target has soul link buff and calculates the remaining duration.
    /// </summary>
    private (bool HasBuff, float TimeLeft) CalculateSoulLinkStatus(IList<Buff> localBuffs, IList<Buff> carryBuffs)
    {
        if (carryBuffs == null) return (false, 0f);

        var carryBuff = carryBuffs.FirstOrDefault(b => b.Name?.Contains("soul_link", StringComparison.OrdinalIgnoreCase) == true);
        if (carryBuff == null) return (false, 0f);

        var timeLeft = carryBuff.Timer;

        if (localBuffs != null)
        {
            var sourceBuff = localBuffs.FirstOrDefault(b => b.Name?.Contains("soul_link_source", StringComparison.OrdinalIgnoreCase) == true);
            if (sourceBuff != null)
            {
                timeLeft = sourceBuff.Timer;
            }
        }

        if (float.IsInfinity(timeLeft)) 
        {
            timeLeft = 99.9f;
        }

        return (true, timeLeft);
    }

    /// <summary>
    /// Calculates positional data and routes to the appropriate on-screen or off-screen drawing logic.
    /// </summary>
    private void RenderEntityMarker(Entity carryEntity, bool hasBuff, float timeLeft, Color color)
    {
        var screenPos = GameController.Game.IngameState.Camera.WorldToScreen(carryEntity.Pos);
        var screenBounds = GameController.Window.GetWindowRectangleTimeCache;

        var isOffScreen = screenPos.X < 0 || screenPos.X > screenBounds.Width ||
                          screenPos.Y < 0 || screenPos.Y > screenBounds.Height;

        var textPos = isOffScreen 
            ? DrawOffScreenPointer(screenPos, screenBounds, color) 
            : DrawOnScreenBox(screenPos, color);

        if (hasBuff)
        {
            var timerText = timeLeft > 90f ? "\u221e" : timeLeft.ToString("F1");
            Graphics.DrawText(timerText, textPos, color, FontAlign.Center);
        }
    }

    /// <summary>
    /// Computes and renders the off-screen directional arrow pointer towards the entity.
    /// </summary>
    private Vector2 DrawOffScreenPointer(Vector2 screenPos, RectangleF screenBounds, Color color)
    {
        var screenCenter = new Vector2(screenBounds.Width / 2, screenBounds.Height / 2);
        var direction = screenPos - screenCenter;
        direction.Normalize();

        var distanceToEdgeX = direction.X != 0 ? (screenBounds.Width / 2f) / Math.Abs(direction.X) : float.MaxValue;
        var distanceToEdgeY = direction.Y != 0 ? (screenBounds.Height / 2f) / Math.Abs(direction.Y) : float.MaxValue;

        var distanceToEdge = Math.Min(distanceToEdgeX, distanceToEdgeY) - 120f;
        var tipPos = screenCenter + direction * distanceToEdge;
        var basePos = tipPos - direction * 100f;

        var sideDir = new Vector2(-direction.Y, direction.X);
        var leftWing = tipPos - direction * 35f + sideDir * 25f;
        var rightWing = tipPos - direction * 35f - sideDir * 25f;

        var thickness = Settings.BoxThickness.Value * 2f;

        Graphics.DrawLine(basePos, tipPos, thickness, color);
        Graphics.DrawLine(leftWing, tipPos, thickness, color);
        Graphics.DrawLine(rightWing, tipPos, thickness, color);

        return basePos - direction * 20f;
    }

    /// <summary>
    /// Draws the bounding box over the visible entity.
    /// </summary>
    private Vector2 DrawOnScreenBox(Vector2 screenPos, Color color)
    {
        var boxWidth = 70f;
        var boxHeight = 100f;
        var rect = new RectangleF(screenPos.X - boxWidth / 2, screenPos.Y - boxHeight + 25f, boxWidth, boxHeight);

        Graphics.DrawFrame(rect, color, Settings.BoxThickness.Value);

        return new Vector2(rect.Center.X, rect.Top - 15f);
    }
}
