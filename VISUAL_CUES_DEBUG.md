# Visual Cues Implementation & Debug Guide

## What Was Implemented

I've added left/right visual cue lines that:
- Spawn at each note's `StartTime` 
- Move from the edges toward the center
- Arrive at the center at `EndTime` (when player should press key)

## Files Changed

1. **TypeBeat.Game/Gameplay/Objects/DrawableNotePair.cs**
   - Replaced texture-based sprites with Box shapes (Red left, Blue right for debug)
   - Lines are 20px wide × 200px tall (very visible for debugging)
   - Spawn off-screen and lerp to center positions
   - Alpha controlled per-line

2. **TypeBeat.Game/Gameplay/Scheduling/NoteScheduler.cs**
   - Changed anchor from Centre to TopLeft
   - Set PreloadMs = 0 (spawn exactly at StartTime)

3. **TypeBeat.Game.Tests/Visual/TestSceneCueLines.cs** (NEW)
   - Test scene to verify cue lines work in isolation

## How to Debug

### Step 1: Build and Run
```powershell
dotnet build .\TypeBeat.sln -c Debug
dotnet run --project .\TypeBeat.Desktop\TypeBeat.Desktop.csproj
```

### Step 2: Check Console Logs

Look for these log messages:
- `[NoteScheduler] Loaded segment with X notes` - confirms segment loaded
- `[NoteScheduler] Spawn note: now=X start=Y end=Z` - confirms notes are spawning
- `[DrawableNotePair] Initialised cue lines start=X end=Y` - confirms drawable created
- `[DrawableNotePair] First Update: t=X startTime=Y endTime=Z size=WxH` - confirms Update() is running
- `[DrawableNotePair] t=X p=Y leftX=A rightX=B y=C` - shows movement every 100ms

### Step 3: What to Look For

**If you see no logs at all:**
- Beatmap might not have note data
- Check beatmap file has StartTime/EndTime values

**If you see spawn logs but no "First Update" logs:**
- DrawableNotePair isn't being added to scene graph correctly
- Check NoteScheduler is actually a child of playfield

**If you see Update logs but no lines:**
- Lines might be positioned off-screen
- Check the logged positions (leftX, rightX, y) are within screen bounds
- Current layout config spawns at -12% and +112% of width

### Step 4: Verify Beatmap Data

Your beatmap MUST have:
```json
{
  "MapData": [
    {
      "Notes": [
        {
          "Character": "H",
          "StartTime": 1000,  // <-- REQUIRED
          "EndTime": 2500     // <-- REQUIRED
        }
      ]
    }
  ]
}
```

**Check a beatmap file** to confirm StartTime/EndTime exist and are reasonable values (> 0, EndTime > StartTime).

## Current Debug Settings

- **Line Colors**: Red (left), Blue (right) - hardcoded for visibility
- **Line Size**: 20px × 200px - much bigger than normal
- **Spawn Position**: Off-screen left (-12% width) and right (+112% width)
- **Destination**: Near center (50% ± 6% width)
- **Center Line Y**: 50% of screen height

## Known Issues to Check

1. **TimeOffsetMs mismatch**: If gameplay starts at a different time than expected, lines might spawn at wrong times
2. **Beatmap timing**: If all StartTime/EndTime values are very large (like 60000ms+), you won't see lines for a long time
3. **Screen size**: If DrawSize is 0×0, positioning won't work

## Quick Test

Run the test scene:
```powershell
# This should show cue lines spawning at specific times
dotnet run --project .\TypeBeat.Game.Tests\TypeBeat.Game.Tests.csproj
# Then navigate to "TestSceneCueLines" in the visual test browser
```

## What's Next

Once you confirm the lines are visible:
1. Re-enable proper colouring (uncomment code in load())
2. Reduce line size to something reasonable (8px × 80px)
3. Adjust layout config if needed (spawn distance, gap width, etc.)
4. Add a center target line for players to aim at

## Contact Me With

- Screenshots of what you see (or don't see)
- Console log output (especially the [DrawableNotePair] and [NoteScheduler] lines)
- A sample of your beatmap JSON showing Note structure
