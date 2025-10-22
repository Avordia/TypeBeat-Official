using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Objects
{
    /// <summary>
    /// Visual representation of a single scrolling music note (TypeNote style).
    /// </summary>
    public partial class DrawableMusicNote : CompositeDrawable
    {
        private readonly double startTime;
        private readonly double endTime;
        private readonly string character;
        private readonly int noteYStep;
        private readonly TypeNoteLayoutConfig layout;

        public double TimeOffsetMs { get; set; } = 0;
        private bool wasHit = false;

        private Sprite noteSprite;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public DrawableMusicNote(Note note, TypeNoteLayoutConfig layout, double timeOffsetMs)
        {
            this.startTime = note.StartTime;
            this.endTime = note.EndTime;
            this.character = note.Character;
            this.layout = layout;
            this.TimeOffsetMs = timeOffsetMs;

            // Get the Y-axis step (e.g., C0=0, C#0=1)
            this.noteYStep = TypeNoteLayoutConfig.GetYStep(character);

            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;

            InternalChild = noteSprite = new Sprite
            {
                Anchor = Anchor.TopLeft, // We will control position manually
                Origin = Anchor.Centre,  // Origin at the center of the sprite
                Alpha = 0
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // --- THIS IS THE UPDATED LOGIC ---
            
            // 1. Get the mapped texture name (e.g., "D#0-A#0") from our new mapper
            string textureName = NoteTextureMapper.GetTextureName(character);

            // 2. Build the full path
            string texturePath = $"images/musicNotes/{textureName}";
            Logger.Log($"[DrawableMusicNote] Mapping note '{character}' to texture name '{textureName}'. Attempting to load: {texturePath}", LoggingTarget.Runtime, LogLevel.Debug);

            var tex = textures.Get(texturePath);

            if (tex == null)
            {
                // Try fallback with .png (in case it's needed)
                tex = textures.Get($"{texturePath}.png");
            }
            
            if (tex == null)
            {
                // Fallback for missing textures
                Logger.Log($"[DrawableMusicNote] Texture not found at path: {texturePath}. Using default.", LoggingTarget.Runtime, LogLevel.Error);
                tex = textures.Get("images/A1"); // Use A1.png as a fallback
            }

            noteSprite.Texture = tex;
        }

        /// <summary>
        /// Called by the scheduler when the note is hit.
        /// </summary>
        public void OnHit()
        {
            wasHit = true;
            this.FadeOut(100, Easing.Out).Expire();
        }

        protected override void Update()
        {
            base.Update();

            if (wasHit) return;

            double t = Clock.CurrentTime - TimeOffsetMs;

            // Before spawn time, stay hidden
            if (t < startTime)
            {
                noteSprite.Alpha = 0;
                return;
            }

            // Calculate progress (0.0 at startTime, 1.0 at endTime)
            double duration = System.Math.Max(1, endTime - startTime);
            double p = System.Math.Clamp((t - startTime) / duration, 0.0, 1.0);

            // Note has passed its end time, expire it
            if (p >= 1)
            {
                // We don't fade, it just expires (missed)
                Expire();
                return;
            }

            // Fade in at the start
            if (p < 0.1)
                noteSprite.Alpha = (float)(p / 0.1);
            else
                noteSprite.Alpha = 1;

            // Get positions
            float spawnX = layout.GetSpawnX(DrawSize);
            float destX = layout.GetDestinationX(DrawSize); // This is the "hit line"
            float y = layout.GetNoteY(DrawSize, noteYStep);

            // Interpolate X position
            float currentX = spawnX + (float)((destX - spawnX) * p);

            noteSprite.Position = new Vector2(currentX, y);
        }
    }
}