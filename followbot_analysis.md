# Technical Analysis: Followbot & Auto-Link Automation

This document outlines the architectural requirements and implementation strategy for building a specialized followbot and automating the casting of Link skills (e.g., Soul Link, Intuitive Link) using the ExileApi framework.

## 1. Followbot Architecture

A reliable followbot requires a state machine that handles different distances and navigation obstacles.

### Core Logic (`FollowerCore.cs`)
- **Target Identification**: Use the `CarryName` or `TrackAllPlayers` logic from CarryTracker to select the leader.
- **Distance Management**:
    - **Close Range (< 20 units)**: Idle or move slightly to stay in buff range.
    - **Medium Range (20-80 units)**: Use `Input.Click` or `Input.MoveMouse` towards the leader's `PosNum`.
    - **Long Range (> 80 units)**: If the leader is off-screen, use the `WorldToScreen` logic to find the edge vector and move in that direction.
- **Pathfinding**: ExileApi does not have a built-in A* pathfinder for terrain. You will likely need to rely on the "Click-to-Move" behavior of the game client or integrate an external pathfinding library that reads the `Terrain` mesh.

### Features
- **Stuck Detection**: If the player's `Address` position hasn't changed in X seconds while trying to move, trigger a repositioning logic (e.g., flame dashing towards the leader).
- **Dash Usage**: Make use of player movement skills like Flame Dash to get closer to the leader if the distance is too large.
---

## 2. Auto-Link Casting Logic

Link skills require a specific secondary player target and have a duration that needs management.

### Detection Logic
- **Valid Targets**: Iterate through `ValidEntitiesByType[EntityType.Player]`.
- **Buff Status**: Check the target for the specific Link buff (e.g., `soul_link`).
- **Distance Check**: Ensure the target is within the skill's cast range (usually 60-80 units).

### Execution Logic
- **Condition**: If `!target.HasBuff("soul_link")` AND `DistanceToPlayer < MaxRange`.
- **Casting**:
    - Move mouse to `WorldToScreen(target.Pos)`.
    - Press the Hotkey assigned to the Link skill.
    - Implement a small delay (100-200ms) to ensure the game registers the click on the target.

---

## 3. Integration with CarryTracker

Since CarryTracker already identifies and calculates the "Soul Link" time remaining for all players, it can serve as the "Sensory Layer" for the followbot.

- **Data Sharing**: CarryTracker can expose a `Dictionary<string, float> LinkDurations` that the Follower plugin can subscribe to.
- **Priority System**: If multiple players are tracked, the Follower can prioritize the player with the lowest Link duration or the one closest to the bot.

## 4. Risks and Considerations

- **Desync**: Predictive networking can cause the leader's position to jump. Always use `entity.PosNum` which is updated from the server-side memory.
- **Obstacles**: Without a proper navigation mesh, the bot will get stuck on walls. Most advanced follower plugins use a "breadcrumb" system where the bot follows the exact path the leader took.
