# MoreVehicles

A mod for **Cities: Skylines** that expands the game's vehicle buffers from the vanilla limits (16,384 active vehicles / 32,768 parked vehicles) up to 65,536.

## Latest Critical Fixes (March 2026)

This rebuild includes fixes for **game-breaking infinite loops and simulation frame desync issues** that prevented race/parade events from working correctly:

### ✅ Fixed: Race/Parade Event Creation Freeze
- **Issue**: When race/parade events started preparing, `EventManager.InvalidatePathsOnClosedRoads()` contained a `ushort` loop counter trying to reach 65,536 (ushort wraps at 65,535 → 0 → infinite loop)
- **Fix**: [New patch] `EventManagerPatch.InvalidatePathsOnClosedRoads()` replaces the loop with `int`-based counter
- **Impact**: Events no longer freeze the simulation thread during preparation phase

### ✅ Fixed: Race Cars Not All Moving in Events
- **Issue**: Only ~25% of race cars received animation; the rest stayed stationary. Root cause: `VehicleManager.SimulationStepImpl()` was using a frame-mask approach (changing `0xF` to `0x3F` for 64 frames) incompatible with `Vehicle.GetTargetFrame()` which uses frame-offset calculation expecting only 16 frames.
- **Fix**: Reverted to chunk-size expansion (`1024 → 4096 vehicles/frame`) keeping 16-frame cycle. Now: 16 frames × 4096 vehicles = 65,536 total. This keeps simulation and rendering frame offsets mathematically synchronized.
- **Impact**: All vehicles now render and animate correctly; race events progress normally with all participants

### ✅ Removed: Workshop ID Gate
- Mod can now be built and tested locally without requiring Steam Workshop compatibility checks
- Allows rapid iteration and testing

---

## What Was Actually Fixed

With MoreVehicles enabled, creating a race/parade event would **freeze the entire simulation**. Users reported the game becoming unresponsive the moment they started a Race Day event.

### Root Cause 1: EventManager.InvalidatePathsOnClosedRoads() Infinite Loop

When an event begins preparing, the game calls:
```
RaceEventAI.BeginPreparing() → CloseEventRoute() → UpdateEventRoute() → InvalidatePathsOnClosedRoads()
```

This method contained:
```csharp
for (ushort num = 1; num < buffer.Length; num = (ushort)(num + 1))
```

With the vanilla buffer at 16,384 entries, this works fine. But with MoreVehicles expanding to 65,536 entries, `ushort` wraps from 65535 → 0 → infinite loop. The simulation thread gets stuck forever.

**Fix**: `EventManagerPatch.InvalidatePathsOnClosedRoads()` replaces the loop with an `int` counter that safely reaches 65,536.

✅ **Result**: Event preparation no longer freezes the simulation.

---

### Root Cause 2: Race Cars Not All Moving (SimulationStepImpl Frame Desync)

Even after the freeze was fixed, noticed that **only a small fraction of race cars actually moved** during events (~6 out of 30 visible in the race track). The rest stayed stationary.

The simulation divides 65,536 vehicles into time-sliced chunks, with each vehicle getting an AI tick once per N frames. The original patch used a frame-mask approach (16 frames × 4,096 per frame), but `Vehicle.GetTargetFrame()` calculates rendering frame positions assuming only 16 frame offsets total. This misalignment meant:
- Vehicles were simulated correctly at frame offset X
- But their animation frames were read from offset Y
- Rendering showed stale data, making high-ID vehicles appear frozen

**Fix**: Changed `SimulationStepImpl` to use a chunk-size approach (16 frames × 4,096 vehicles/frame). By expanding the chunk size instead of the frame count, simulation and rendering frame offsets stay mathematically synchronized.

✅ **Result**: All race vehicles now animate and drive correctly.

---

## Changes Made in This Build

Comprehensive fixes for infinite-loop bugs and simulation desync issues:

### Critical Patches (Event System)
- ✅ **EventManagerPatch**: Fixed `InvalidatePathsOnClosedRoads()` ushort infinite loop (events no longer freeze on preparation)

### Critical Patches (Vehicle Simulation)
- ✅ **VehicleManagerPatch.SimulationStepImpl**: Changed from frame-mask approach (64 frames, 1024 vehicles) to proper chunk-size expansion (16 frames, 4096 vehicles/frame). This aligns with `Vehicle.GetTargetFrame()` calculation, keeping simulation and rendering in sync across all 65,536 vehicle IDs.
- ✅ **VehicleManagerPatch.ExtraSimulationStep**: Expanded loop from 16,384 → 65,536 so all vehicles receive extra simulation ticks

### Grid Traversal Guards
- ✅ **VehicleLinkedListPatch**: Raised all linked-list safety guards from 16,384 → 65,536 in:
  - `VehicleManager.EndRenderingImpl()` (rendering)
  - `VehicleManager.PlayAudioImpl()` (audio)
  - `VehicleManager.ReleaseVehicleImplementation()` (cleanup)

### Building Vehicle Lists
- ✅ **BuildingPatch**: Raised linked-list guards from 16,384 → 65,536 in:
  - `Building.RemoveOwnVehicle()`
  - `Building.IsOwnVehicle()`
  - `Building.RemoveGuestVehicle()`
  - `Building.IsGuestVehicle()`

### Other
- ✅ **Removed Workshop ID gate**: Allows local testing and builds
- ✅ **Harmony 2 compatible**: Uses `CitiesHarmony.API` and updated SkyTools DLLs

### Expected Deployed Files
- `MoreVehicles.dll`
- `SkyTools.Common.dll`
- `SkyTools.Patching.dll`
- `CitiesHarmony.API.dll`

## Why `CitiesHarmony.API.dll` Is Included
`CitiesHarmony.API.dll` provides the Harmony 2 API surface (type definitions) that the mod uses at compile time. It is typically included in successful City Skylines mods and is required at runtime in the mod folder when the game loads the mod.

The actual Harmony runtime implementation is provided by the Steam workshop `CitiesHarmony` mod, but the API DLL is still needed in the mod folder to satisfy loader dependency resolution.

## Notes for Modders
- MoreVehicles works by patching internal game constants and resizing the `VehicleManager` arrays.
- The mod must apply its custom array sizing every time a level is loaded, because some edge cases can reset the arrays to vanilla sizes (e.g., enabling the mod mid-session).
- **Linked-list guard patches** are critical for stability with expanded vehicles; without them, traversals that legitimately exceed 16,384 steps corrupt lists and cause crashes or freezes.
- The **`SimulationStepImpl` patch** is the most delicate: it must chunk vehicles in sync with `Vehicle.GetTargetFrame()` frame offset calculations. The mod uses 16 simulation frames × 4,096 vehicles/frame = 65,536 total, which keeps both simulation tick scheduling and rendering frame interpolation mathematically aligned. A misaligned approach (e.g., 64 frames × 1,024 vehicles) causes vehicles with high IDs to simulate correctly but render at wrong frame offsets—appearing frozen or twitchy.
- **Vehicle spawning order matters**: Vehicle IDs are assigned sequentially as vehicles are spawned. Events that spawn many vehicles rapidly will fill ID slots densely; this is where most simulation alignment bugs surface.

If you find that Race Day events still crash or freeze after these changes, please report the exact error from the game's output log (`~/Library/Logs/Unity/Player.log` on macOS) along with the steps to reproduce (e.g., which event type, how many racers, saved game context, etc.).
