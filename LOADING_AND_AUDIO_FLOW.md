# Loading Screen & Audio Flow Implementation

## Summary

Added a complete loading screen transition and proper audio management flow for the rhythm game.

## Changes Made

### 1. New LoadingScreen Component ✅
**File**: `LoadingScreen.cs` (NEW)

**Features**:
- Dark overlay background (90% opacity)
- Displays beatmap info: Title, Artist, Difficulty
- Animated loading text with dots
- Progress bar animation (2 seconds)
- Automatically transitions to GameScreen when complete

**Visual Elements**:
- Title: 48pt bold
- Artist: 32pt gray
- Difficulty: 24pt yellow
- Loading bar: 400px wide, lime green fill
- Smooth fade-in (300ms)

### 2. Audio Flow Management ✅

#### SongSelection → Loading → GameScreen
**File**: `SongSelectionScreen.cs`
- **Line 478-489**: Modified `startGame()` method
- Stops preview music when entering loading screen
- Transitions to `LoadingScreen` instead of directly to `GameScreen`

#### GameScreen Audio Start
**File**: `GameScreen.cs`
- **Lines 5-7, 27**: Added audio imports
- **Line 64**: Added `gameTrack` field
- **Line 69**: Added `AudioManager` dependency
- **Lines 210-230**: Load beatmap audio from ZIP archive
- **Lines 344-355**: Start audio on `OnEntering()` - **this marks gameplay start!**
- **Lines 357-366**: Stop audio on `OnExiting()`

#### Return to SongSelection
**File**: `SongSelectionScreen.cs`
- **Lines 243-260**: Enhanced `OnResuming()` method
- Restarts track from beginning with `track.Restart()`
- Resets video to loop from start
- Logs state for debugging

## Audio Flow Diagram

```
┌─────────────────┐
│  SongSelection  │  ← Preview music playing, video looping
│                 │
└────────┬────────┘
         │ User presses Play
         │ track.Stop()
         ↓
┌─────────────────┐
│ LoadingScreen   │  ← Silent, 2-second animation
│                 │
└────────┬────────┘
         │ Auto-transition after loading
         ↓
┌─────────────────┐
│   GameScreen    │  ← gameTrack.Start() - GAMEPLAY BEGINS!
│                 │     Timing syncs to audio
└────────┬────────┘
         │ User presses ESC
         │ gameTrack.Stop()
         ↓
┌─────────────────┐
│  SongSelection  │  ← track.Restart() - music/video from start
│   (resumed)     │
└─────────────────┘
```

## Timing Synchronization

### Gameplay Clock
```csharp
// In GameScreen.OnEntering()
gameTrack.Start();                    // Audio begins
gameplayStartClockMs = Clock.CurrentTime; // Capture moment
noteScheduler.TimeOffsetMs = gameplayStartClockMs; // Sync visuals
```

**Result**: Visual cues and judgement timing are synchronized to audio playback.

### Why This Matters
- Rhythm games require **precise timing** between audio and visuals
- Starting audio marks the definitive "game start"
- All note timing is relative to this moment
- Players hear and see notes in perfect sync

## File Structure

### New File
```
TypeBeat.Game/
  LoadingScreen.cs          (NEW - 170 lines)
```

### Modified Files
```
TypeBeat.Game/
  SongSelectionScreen.cs    (Modified - audio stop, resume logic)
  GameScreen.cs             (Modified - audio loading & playback)
```

## User Experience Flow

### 1. Song Selection
- Music preview plays
- Video plays (if available)
- User browses beatmaps

### 2. Start Game (Press Enter)
- Preview music stops immediately
- Screen fades to loading screen
- Loading animation plays (2 seconds)

### 3. Loading Screen
- Shows beatmap information
- Animated progress bar fills
- Silent transition period
- Auto-advances to gameplay

### 4. Gameplay
- **Audio starts playing** - game begins!
- Visual cues spawn and move to music
- Player types along with rhythm
- Timing judged relative to audio

### 5. Exit to Selection (Press ESC)
- Game audio stops
- Returns to song selection
- **Music/video restart from beginning**
- User can select another song

## Technical Details

### Loading Screen Timing
- Fade in: 300ms
- Progress animation: 2000ms
- Auto-transition: 2100ms (slightly after progress complete)
- Fade out: 300ms (on exit)

### Audio Loading
```csharp
// GameScreen loads audio from beatpack ZIP
using (var stream = File.OpenRead(beatpack.FilePath))
{
    var storage = new ZipArchiveResourceStore(stream);
    var trackStore = audioManager.GetTrackStore(storage);
    gameTrack = trackStore.Get(beatpack.MusicPath);
    gameTrack.Looping = false; // Don't loop gameplay music
}
```

### Audio Restart
```csharp
// SongSelection restarts preview music
track.Restart(); // Stops and starts from position 0
```

## Testing Checklist

- [ ] Song selection music plays on load
- [ ] Music stops when pressing Play
- [ ] Loading screen shows correct beatmap info
- [ ] Loading bar animates smoothly
- [ ] Auto-transitions to game after 2 seconds
- [ ] Game audio starts immediately on game screen
- [ ] Visual cues sync with audio
- [ ] Pressing ESC stops game audio
- [ ] Returning to selection restarts music from beginning
- [ ] Video restarts from beginning (if present)

## Customization Options

### Loading Duration
In `LoadingScreen.cs`:
```csharp
// Line ~142
progress += (float)(Clock.ElapsedFrameTime / 2000.0); // Change 2000 for different speed
// Line ~151
Scheduler.AddDelayed(() => { ... }, 2100); // Change 2100 to match
```

### Audio Fade (Future Enhancement)
To add smooth fade instead of instant stop:
```csharp
// In SongSelectionScreen.startGame()
// Would need custom fade logic or audio binding transformations
```

### Loading Screen Style
Modify `LoadingScreen.cs` constructor for:
- Different colors
- Additional information
- Custom animations
- Beatmap preview image

## Notes

- Loading screen is currently a "fake" load (just animation)
- Could be extended to actually load assets if needed
- Audio synchronization is critical for rhythm game feel
- Music restart ensures clean experience when replaying

## Benefits

✅ **Professional flow** - Smooth transitions between screens  
✅ **Clear state changes** - Audio stops/starts mark transitions  
✅ **Rhythm game sync** - Audio start = gameplay start  
✅ **Clean replay** - Music restarts from beginning each time  
✅ **User feedback** - Loading screen shows what's loading  

The game now has a proper rhythm game flow with audio-synchronized gameplay! 🎵
