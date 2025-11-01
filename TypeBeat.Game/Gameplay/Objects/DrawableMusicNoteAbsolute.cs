using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Appearance;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Objects
{
    /// <summary>
    /// Visual representation of a single scrolling music note (TypeNote style) that uses ABSOLUTE timing.
    /// startAbsMs/endAbsMs are compared directly to Clock.CurrentTime, independent of gameplay offset.
    /// </summary>
    public partial class DrawableMusicNoteAbsolute : CompositeDrawable
    {
        private readonly double startAbsMs;
        private readonly double endAbsMs;
        private readonly string character;
        private readonly int noteYStep;
        private readonly TypeNoteLayoutConfig layout;

        private bool wasHit = false;
        private bool hasLoggedFirstUpdate = false;

        private Sprite noteSprite;
        private SpriteText sharpSymbol;
        private readonly bool isSharp;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public DrawableMusicNoteAbsolute(double startAbsMs, double endAbsMs, string character, TypeNoteLayoutConfig layout)
        {
            this.startAbsMs = startAbsMs;
            this.endAbsMs = endAbsMs;
            this.character = character;
            this.layout = layout;

            // Check if this is a sharp note
            this.isSharp = character.Contains('#');

            // Get the Y-axis step (e.g., C0=0, C#0=1)
            this.noteYStep = TypeNoteLayoutConfig.GetYStep(character);

            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;

            // Create a container to hold both the note sprite and sharp symbol
            var noteContainer = new Container
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    noteSprite = new Sprite
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Scale = new Vector2(1.14f),
                    },
                    sharpSymbol = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "#",
                        Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"),
                        Colour = Colour4.White,
                        Alpha = 0, // Hidden by default, shown only for sharps
                        X = 30, // Position to the right of the note
                        Y = -8 // Slightly up
                    }
                }
            };

            InternalChild = noteContainer;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Extract the natural note (remove the sharp if present)
            // E.g., "C#0" → "C0", "D0" → "D0"
            string naturalNote = character.Replace("#", "");
            
            // Get the mapped texture name for the NATURAL note
            string textureName = NoteTextureMapper.GetTextureName(naturalNote);
            string texturePath = $"images/musicNotes/{textureName}";

            var tex = textures.Get(texturePath);

            if (tex == null)
            {
                tex = textures.Get($"{texturePath}.png");
            }
            
            if (tex == null)
            {
                Logger.Log($"[DrawableMusicNoteAbsolute] Texture not found at path: {texturePath}. Using default.", LoggingTarget.Runtime, LogLevel.Error);
                tex = textures.Get("images/A1");
            }

            noteSprite.Texture = tex;
            
            // Show the sharp symbol only if this is a sharp note
            if (isSharp)
            {
                sharpSymbol.Alpha = 1;
            }
            
            Logger.Log($"[DrawableMusicNoteAbsolute] Loaded texture for note '{character}' startAbs={startAbsMs} endAbs={endAbsMs}", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Called when the note is hit.
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

            double nowAbs = Clock.CurrentTime;
            float width = DrawSize.X;
            float height = DrawSize.Y;
            var size = new Vector2(width, height);

            if (!hasLoggedFirstUpdate)
            {
                hasLoggedFirstUpdate = true;
                Logger.Log($"[DrawableMusicNoteAbsolute] First update: nowAbs={nowAbs:F1} startAbs={startAbsMs:F1} endAbs={endAbsMs:F1}", LoggingTarget.Runtime, LogLevel.Important);
            }

            // Before spawn time, stay hidden
            if (nowAbs < startAbsMs)
            {
                InternalChild.Alpha = 0;
                return;
            }

            // Calculate progress (0.0 at startAbsMs, 1.0 at endAbsMs)
            double duration = System.Math.Max(1, endAbsMs - startAbsMs);
            double p = System.Math.Clamp((nowAbs - startAbsMs) / duration, 0.0, 1.0);

            // Note has passed its end time, expire it
            if (p >= 1)
            {
                Expire();
                return;
            }

            // Fade in at the start
            if (p < 0.1)
                InternalChild.Alpha = (float)(p / 0.1);
            else
                InternalChild.Alpha = 1;

            // Get positions
            float spawnX = layout.GetSpawnX(size);
            float destX = layout.GetDestinationX(size);
            float y = layout.GetNoteY(size, noteYStep);

            // Apply stem direction offset for B0 and higher notes
            float stemOffset = TypeNoteLayoutConfig.GetStemDirectionOffset(character);
            y += stemOffset;

            // Interpolate X position
            float currentX = spawnX + (float)((destX - spawnX) * p);

            InternalChild.Position = new Vector2(currentX, y);
        }
    }
}
