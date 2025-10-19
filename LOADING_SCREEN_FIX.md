# Loading Screen Stack Management Fix

## Problem

When exiting GameScreen (pressing ESC), the user was stuck on the LoadingScreen instead of returning to SongSelection.

## Root Cause

**Screen Stack Order**:
```
SongSelection.Push(LoadingScreen)
  → LoadingScreen.Push(GameScreen)
    → GameScreen.Exit() returns to LoadingScreen ❌
```

The stack was: `SongSelection` → `LoadingScreen` → `GameScreen`

When GameScreen exited, it naturally returned to the previous screen (LoadingScreen), not SongSelection.

## Solution

Make LoadingScreen exit itself immediately after pushing GameScreen.

### Code Change
**File**: `LoadingScreen.cs`

```csharp
private void simulateLoading()
{
    Scheduler.AddDelayed(() =>
    {
        if (progress >= 0.99f)
        {
            var gameScreen = new GameScreen(beatpack, beatmap);
            this.Push(gameScreen);
            
            // NEW: Exit loading screen immediately
            Schedule(() => this.Exit());
        }
    }, 2100);
}
```

### Result

**New Screen Stack**:
```
SongSelection.Push(LoadingScreen)
  → LoadingScreen.Push(GameScreen)
  → LoadingScreen.Exit() (removes itself)
  → Stack is now: SongSelection → GameScreen ✅
  → GameScreen.Exit() returns to SongSelection ✅
```

## How It Works

1. **SongSelection** pushes **LoadingScreen**
2. **LoadingScreen** shows animation for 2 seconds
3. **LoadingScreen** pushes **GameScreen**
4. **LoadingScreen** immediately exits (schedules its own Exit())
5. Stack cleanup: LoadingScreen is removed
6. Final stack: **SongSelection** → **GameScreen**
7. When GameScreen exits → returns to **SongSelection** ✅

## Visual Flow

```
┌─────────────────┐
│  SongSelection  │  ← Starting point
└────────┬────────┘
         │ Push LoadingScreen
         ↓
┌─────────────────┐
│ LoadingScreen   │  ← Shows for 2 seconds
└────────┬────────┘
         │ Push GameScreen + Exit()
         ↓
┌─────────────────┐
│   GameScreen    │  ← LoadingScreen removed from stack
└────────┬────────┘
         │ Exit (ESC)
         ↓
┌─────────────────┐
│  SongSelection  │  ← Returns here correctly! ✅
└─────────────────┘
```

## Technical Details

### Screen Stack Behavior
- `Push(screen)`: Adds screen on top of current screen
- `Exit()`: Removes current screen from stack, returns to previous
- Stack is LIFO (Last In, First Out)

### Schedule vs Immediate
```csharp
this.Push(gameScreen);        // Immediate
Schedule(() => this.Exit());  // Next frame
```

The `Schedule()` ensures GameScreen is fully pushed before LoadingScreen exits, preventing timing issues.

## Testing

✅ **Before Fix**:
- Play game → ESC → stuck on loading screen

✅ **After Fix**:
- Play game → ESC → returns to song selection
- Music restarts from beginning
- Can select and play again

## Alternative Solutions Considered

### 1. Replace Instead of Push
```csharp
// LoadingScreen would do:
this.MakeCurrent(); // Replace itself with GameScreen
```
**Rejected**: More complex, less clear intent

### 2. Direct Push from SongSelection
```csharp
// SongSelection would do:
this.Push(new GameScreen(beatpack, beatmap));
```
**Rejected**: Loses loading screen UX

### 3. LoadingScreen as Overlay
```csharp
// Show loading as overlay, not screen
```
**Rejected**: Doesn't fit screen navigation pattern

## Best Practice

**When using transitional/temporary screens:**
- Push the target screen
- Immediately exit the transitional screen
- This keeps the stack clean and navigation intuitive

## Summary

One line of code fixes the stuck loading screen issue:
```csharp
Schedule(() => this.Exit());
```

Now the screen flow works perfectly! 🎮
