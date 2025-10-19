using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Objects
{
    /// <summary>
    /// Visual pair of mirrored notes (left and right) for a single character. Purely visual; judgement happens in typing logic.
    /// </summary>
    public partial class DrawableNotePair : CompositeDrawable
    {
        private readonly double startTime;
        private readonly double endTime;
    private readonly bool isSpace;
    public double TimeOffsetMs { get; set; } = 0; // gameplay start offset

    private Sprite leftLine = null!;
    private Sprite rightLine = null!;
    private bool hasLoggedFirstUpdate = false;
    private bool wasHit = false; // Track if this note was hit by player

    [Resolved]
    private TextureStore textures { get; set; } = null!;

        private readonly LayoutConfig layout;
        private readonly NoteAppearanceConfig appearance;

        /// <param name="startTime">Absolute time (ms) when the note pair spawns off-screen.</param>
        /// <param name="endTime">Absolute time (ms) when the note pair should arrive at the target line.</param>
        /// <param name="isSpace">Whether this note pair represents the space token ('/'), which will be tinted differently.</param>
        /// <param name="layout">Layout configuration using relative screen fractions for positioning.</param>
        /// <param name="appearance">Colours/tints to apply to letter vs. space notes.</param>
        public DrawableNotePair(double startTime, double endTime, bool isSpace,
            LayoutConfig layout, NoteAppearanceConfig appearance)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.isSpace = isSpace;
            this.layout = layout;
            this.appearance = appearance;

            RelativeSizeAxes = Axes.Both; // use full parent size for positioning
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;

            // Create two sprite cues that will use the LeftToRightNote.png texture
            const float note_scale = 1.5f; // Larger size for better visibility
            InternalChildren = new Drawable[]
            {
                leftLine = new Sprite 
                { 
                    Anchor = Anchor.TopLeft, 
                    Origin = Anchor.Centre, 
                    Scale = new Vector2(note_scale, note_scale) // Normal orientation for left
                },
                rightLine = new Sprite 
                { 
                    Anchor = Anchor.TopLeft, 
                    Origin = Anchor.Centre, 
                    Scale = new Vector2(-note_scale, note_scale) // Flipped horizontally for right
                },
            };

            // Make visible by default
            Alpha = 1;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load the texture for both lines
            var texture = textures.Get("images/LeftToRightNote.png");
            leftLine.Texture = texture;
            rightLine.Texture = texture;
            
            // Apply tint based on note type
            var colour = isSpace ? appearance.SpaceColour : appearance.LetterColour;
            leftLine.Colour = rightLine.Colour = colour;
            
            Logger.Log($"[DrawableNotePair] Loaded texture 'LeftToRightNote.png' start={startTime} end={endTime} (colour={(isSpace ? "space" : "letter")})", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Called when the player hits this note correctly. Makes it disappear immediately.
        /// </summary>
        public void OnHit()
        {
            wasHit = true;
            // Fade out quickly (150ms)
            this.FadeOut(150).Expire();
        }

        protected override void Update()
        {
            base.Update();

            // If already hit, let the fade out animation handle it
            if (wasHit)
                return;

            // Current time from clock; this Drawable uses the parent's clock (GameScreen/Conductor later).
            double t = Clock.CurrentTime - TimeOffsetMs;
            if (t < 0) t = 0;
            float width = DrawSize.X;
            float height = DrawSize.Y;
            var size = new Vector2(width, height);

            if (!hasLoggedFirstUpdate)
            {
                hasLoggedFirstUpdate = true;
                Logger.Log($"[DrawableNotePair] First Update: t={t:F0} startTime={startTime} endTime={endTime} size={width}x{height} TimeOffsetMs={TimeOffsetMs}", LoggingTarget.Runtime, LogLevel.Important);
            }

            var (leftSpawnX, rightSpawnX) = layout.GetSpawnXs(size);
            var (leftDestX, rightDestX) = layout.GetDestinationXs(size);
            float y = layout.GetCenterLineY(size);

            if (t < startTime)
            {
                // Before spawn: keep hidden at spawn positions.
                leftLine.Alpha = 0;
                rightLine.Alpha = 0;
                leftLine.Position = new Vector2(leftSpawnX, y);
                rightLine.Position = new Vector2(rightSpawnX, y);
                return;
            }

            // Normalize progress 0..1.
            double dur = Math.Max(1, endTime - startTime); // avoid divide by zero
            double p = Math.Clamp((t - startTime) / dur, 0, 1);

            // Expire once we've past arrival time
            if (p >= 1)
            {
                Expire();
                return;
            }

            // Make lines visible and fade out as they approach center
            // Fade starts at 80% progress and reaches 0 at 100%
            float fadeProgress = Math.Max(0, (float)((p - 0.8) / 0.2)); // 0 at p=0.8, 1 at p=1.0
            float alpha = 1f - fadeProgress;
            leftLine.Alpha = alpha;
            rightLine.Alpha = alpha;

            float lp = (float)p;
            // Linear interpolation from spawn to destination (manual to avoid MathHelper dependency).
            float lx = (float)(leftSpawnX + (leftDestX - leftSpawnX) * lp);
            float rx = (float)(rightSpawnX + (rightDestX - rightSpawnX) * lp);

            leftLine.Position = new Vector2(lx, y);
            rightLine.Position = new Vector2(rx, y);
            
            // Debug: log every 100ms when visible
            if ((int)(t / 100) != (int)((t - Clock.ElapsedFrameTime) / 100))
            {
                Logger.Log($"[DrawableNotePair] t={t:F0} p={p:F2} leftX={lx:F0} rightX={rx:F0} y={y:F0} size={width:F0}x{height:F0}", LoggingTarget.Runtime, LogLevel.Important);
            }
        }
    }
}