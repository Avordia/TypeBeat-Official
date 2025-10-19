# Visual Cues Sync with Judgement

## What Changed

The visual cue lines now **disappear immediately when judgement happens**, whether from:
1. Player pressing a key (correct or incorrect)
2. Auto-miss when the note passes without input

## Implementation Details

### New Components

#### DrawableNotePair.cs
- Added `wasHit` flag to track if note was consumed
- Added `OnHit()` method that triggers immediate fade out (150ms) and expiration
- Update loop skips position updates once hit

#### NoteScheduler.cs
- Tracks all active note pairs in order
- Maintains `currentNoteIndex` to know which note should be hit next
- `HitCurrentNote()` method notifies the current note to disappear

#### GameScreen.cs
- Calls `noteScheduler.HitCurrentNote()` immediately after consuming input
- Also calls it for auto-missed notes

## Behavior Flow

### When Player Presses Key:
```
1. Key pressed → TypingManager.HandleKeyPress()
2. If within active window → Consumed = true
3. GameScreen calls noteScheduler.HitCurrentNote()
4. Current note pair's OnHit() is called
5. Note fades out (150ms) and expires
6. Score/combo/accuracy updated
```

### When Note Auto-Misses:
```
1. Update loop detects note passed late window
2. TypingManager.AutoConsumeMisses() returns count
3. For each auto-miss:
   - Call noteScheduler.HitCurrentNote()
   - Apply miss judgement
   - Update UI
```

## Timing Alignment

✅ **Visual and Judgement Perfectly Synced**
- Input only accepted: `StartTime` → `EndTime + late_window`
- Visual disappears: Immediately when input consumed
- Auto-miss triggers: After `EndTime + late_window`
- Visual disappears: Immediately when auto-missed

## Visual Feedback

### Successful Hit:
- Player presses correct key
- Lines fade out instantly (150ms animation)
- Clean visual confirmation of hit

### Wrong Key:
- Player presses wrong key (still counts as Miss)
- Lines still disappear (consumed the note)
- Reinforces that input was registered

### Missed Note:
- Note passes without input
- Auto-miss triggered
- Lines disappear to clear screen for next notes

## Quality of Life Benefits

✅ **Clear Visual Feedback** - See immediately when you've hit a note  
✅ **No Clutter** - Missed/hit notes disappear, screen stays clean  
✅ **Timing Confirmation** - Visual confirms your input was registered  
✅ **Reduced Confusion** - Lines don't linger after being consumed  

## Technical Notes

### Fade Duration
Current: 150ms (fast but smooth)
```csharp
// In DrawableNotePair.OnHit()
this.FadeOut(150).Expire();
```

Adjust if needed:
- `50` = very fast (almost instant)
- `150` = quick and smooth (current)
- `300` = slower, more noticeable fade

### Note Tracking
Notes are consumed in order (first-in-first-out):
- Spawned notes added to `activePairs` list
- `currentNoteIndex` tracks which note is active
- Increments when `HitCurrentNote()` is called

### Edge Cases Handled
✅ **Multiple rapid hits** - Index increments correctly  
✅ **Auto-miss + manual hit** - Both call HitCurrentNote()  
✅ **Segment completion** - Index resets on LoadSegment()  
✅ **Paused game** - Notes don't disappear until unpaused  

## Testing Checklist

- [ ] Lines disappear when correct key pressed
- [ ] Lines disappear when wrong key pressed (Miss)
- [ ] Lines disappear when auto-missed
- [ ] No lines linger on screen after being consumed
- [ ] Multiple rapid keypresses work correctly
- [ ] Fade animation looks smooth
- [ ] Performance is good with many notes

## Customization

### Faster Disappearance
```csharp
// In DrawableNotePair.OnHit()
this.FadeOut(50).Expire(); // Almost instant
```

### Instant Disappearance (No Fade)
```csharp
// In DrawableNotePair.OnHit()
wasHit = true;
Expire(); // Remove immediately, no animation
```

### Different Fade for Hit vs Miss
Requires passing judgement type to OnHit():
```csharp
public void OnHit(JudgementType judgement)
{
    wasHit = true;
    if (judgement == JudgementType.Miss)
        this.FadeOut(50).Expire(); // Fast for miss
    else
        this.FadeOut(200).Expire(); // Slower for hit
}
```

## Summary

Visual cues and judgement are now **perfectly synchronized**:
- Lines only appear during active note window (StartTime → EndTime + late)
- Lines disappear immediately when judgement happens
- Clean, responsive visual feedback for every input
- No visual clutter from old notes

The game now provides clear, immediate feedback that aligns perfectly with the timing system! 🎮
