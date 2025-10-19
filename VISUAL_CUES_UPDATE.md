# Visual Cues - Using Custom PNG Images

## What Changed

Updated `DrawableNotePair` to use your custom arc image (`LeftToRightNote.png`) instead of simple box shapes.

## Implementation Details

### Texture Loading
- **Left line**: Uses `images/LeftToRightNote.png` with normal orientation
- **Right line**: Uses the same texture but **horizontally flipped** (negative X scale)
- Both sprites are scaled by `1.5f` (adjustable in code)

### Color Tinting
- **Letter notes**: Tinted with `NoteAppearanceConfig.LetterColour` (default: White)
- **Space notes**: Tinted with `NoteAppearanceConfig.SpaceColour` (default: Gold)

### Positioning
- Sprites spawn off-screen and move toward center
- Use `Origin = Anchor.Centre` so rotation/scaling happens around sprite center
- Movement controlled by `LayoutConfig` (gap, spawn distance, etc.)

## Customization Options

### 1. Adjust Size
In `DrawableNotePair.cs`, line ~54:
```csharp
const float note_scale = 1.5f; // Change this number
// 1.0f = original size
// 2.0f = double size
// 0.5f = half size
```

### 2. Use Different Images for Space Notes
If you have a separate image for spaces, modify the `load()` method:
```csharp
var textureName = isSpace ? "images/SpaceNote.png" : "images/LeftToRightNote.png";
var texture = textures.Get(textureName);
```

### 3. Add Rotation During Movement
In the `Update()` method, add rotation based on progress:
```csharp
leftLine.Rotation = (float)(p * 360); // Rotate 360 degrees during travel
rightLine.Rotation = (float)(p * -360); // Opposite direction
```

### 4. Adjust Gap Between Lines
In `GameScreen.cs`:
```csharp
private readonly LayoutConfig layoutConfig = new LayoutConfig
{
    HalfGapXFraction = 0.12f // Increase to make lines stop further apart
};
```

## File Structure

Your image should be at:
```
TypeBeat.Resources/images/LeftToRightNote.png
```

The texture is loaded via the framework's `TextureStore` with the path:
```
"images/LeftToRightNote.png"
```

## Notes

- The curved arc shape will now move from left/right edges to center
- The texture is automatically flipped for the right side (no separate image needed)
- Both sprites are tinted the same color based on note type
- Debug logging still active - check console for "Loaded texture" messages

## Next Steps

1. Build and run to see the curved arcs in action
2. Adjust `note_scale` if they're too big/small
3. Tweak `HalfGapXFraction` to adjust convergence distance
4. Consider adding rotation or other effects during movement

## Optional Enhancements

- **Fade in/out**: Add smooth alpha transitions instead of instant show/hide
- **Scale animation**: Make sprites grow/shrink as they approach center
- **Glow effect**: Add a second sprite layer with additive blending
- **Particle trail**: Spawn small particles behind the moving arcs
