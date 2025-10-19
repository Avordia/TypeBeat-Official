# Quality of Life Updates

## Changes Made

### 1. Smaller Visual Cues ✅
**File**: `DrawableNotePair.cs`
- Reduced sprite scale from `1.5f` to `0.8f`
- Lines are now smaller and less obtrusive

### 2. Key Input Window Restriction ✅
**File**: `TypingManager.cs`
- Key presses are **only accepted** between `note.StartTime` and `note.EndTime + late_window`
- Random keypresses **outside this window are completely ignored**
- No penalty, no consumption, no effect

**Benefits for ADHD players:**
- Can press keys freely before notes appear without penalty
- No accidental misses from nervous button mashing
- Only inputs during the actual note window count

### 3. Lines Disappear at Center ✅
**File**: `DrawableNotePair.cs`
- Lines start fading out at **80% progress**
- Fully invisible when they reach the center (100% progress)
- Smooth fade transition over the last 20% of travel

## Technical Details

### Input Window Logic
```
Active Window = [StartTime, EndTime + late_window]
```

- **Before StartTime**: Keys ignored (note hasn't spawned yet)
- **StartTime to EndTime**: Keys accepted and judged
- **EndTime to EndTime + late_window**: Keys accepted but judged as late/50
- **After late_window**: Auto-miss triggered, manual input ignored

### Fade Out Calculation
```
Fade starts: 80% progress
Fade ends: 100% progress
Alpha = 1.0 - ((progress - 0.8) / 0.2)
```

- At 80% progress: alpha = 1.0 (fully visible)
- At 90% progress: alpha = 0.5 (half transparent)
- At 100% progress: alpha = 0.0 (invisible)

## User Experience Improvements

### For Players with ADHD:
✅ **No punishment for fidgeting** - press keys all you want between notes  
✅ **Clear active window** - only inputs during note visibility count  
✅ **Less visual clutter** - lines disappear cleanly at hit point  

### For All Players:
✅ **Cleaner visuals** - smaller, less distracting note cues  
✅ **Smooth animations** - fade out instead of abrupt disappearance  
✅ **Predictable timing** - inputs only count when notes are active  

## Testing

Build and test:
```powershell
dotnet build .\TypeBeat.sln -c Debug
dotnet run --project .\TypeBeat.Desktop\TypeBeat.Desktop.csproj
```

### What to observe:
1. **Smaller arcs** - visual cues should be more subtle
2. **Ignored inputs** - mash keys before notes spawn, nothing happens
3. **Fade out** - arcs gradually disappear as they approach center
4. **Active window** - only inputs during note travel are judged

## Customization

### Adjust Fade Timing
In `DrawableNotePair.cs`, modify fade start point:
```csharp
// Current: fade starts at 80%
float fadeProgress = Math.Max(0, (float)((p - 0.8) / 0.2));

// Start earlier (50%):
float fadeProgress = Math.Max(0, (float)((p - 0.5) / 0.5));

// Start later (90%):
float fadeProgress = Math.Max(0, (float)((p - 0.9) / 0.1));
```

### Adjust Sprite Size
In `DrawableNotePair.cs`:
```csharp
const float note_scale = 0.8f; // Current size
// 0.5f = very small
// 1.0f = original size
// 1.2f = slightly bigger
```

### Adjust Input Window Strictness
In `TypingManager.cs`, modify boundaries:
```csharp
// Current: Accept from StartTime to EndTime + late_window
double earlyBoundary = note.StartTime;
double lateBoundary = note.EndTime + windows.Window50;

// More lenient (accept earlier):
double earlyBoundary = note.StartTime - 100; // Accept 100ms before spawn

// More strict (shorter window):
double lateBoundary = note.EndTime + (windows.Window50 * 0.5); // Half the late window
```

## Notes

- Input window uses the same timing as visual cues (StartTime → EndTime)
- Auto-miss still triggers for notes that pass completely without input
- The fade creates a satisfying "arrival" feel at the hit point
- Smaller sprites reduce screen clutter while maintaining visibility
