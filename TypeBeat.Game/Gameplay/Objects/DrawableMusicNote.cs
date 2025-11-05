using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Shapes;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;
using osu.Framework.Logging;
using TypeBeat.Game.Gameplay.Judgement;
using TypeBeat.Game.Gameplay.Config;

namespace TypeBeat.Game.Gameplay.Objects
{
    public partial class DrawableMusicNote : CompositeDrawable
    {
    // visual linger is controlled via GameplayVisualSettings
        private readonly double startTime;
        private readonly double endTime;
        private readonly string character;
        private readonly int noteYStep;
        private readonly TypeNoteLayoutConfig layout;

        public double TimeOffsetMs { get; set; } = 0;
        private bool wasHit = false;

    private Sprite noteSprite;
        private SpriteText sharpSymbol;
        private readonly bool isSharp;
        private readonly bool isHighOctave; // C1 or higher
    private Container noteContainer;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public DrawableMusicNote(Note note, TypeNoteLayoutConfig layout, double timeOffsetMs)
        {
            this.startTime = note.StartTime;
            this.endTime = note.EndTime;
            this.character = note.Character;
            this.layout = layout;
            this.TimeOffsetMs = timeOffsetMs;

            // Check if this is a sharp note
            this.isSharp = character.Contains('#');

            // Check if this is C1 or higher octave
            this.isHighOctave = IsHighOctaveNote(character);

            // Get the Y-axis step (e.g., C0=0, C#0=1)
            this.noteYStep = TypeNoteLayoutConfig.GetYStep(character);

            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;

            // Create a container to hold both the note sprite and sharp symbol
            noteContainer = new Container
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
                        Y = -8 
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

            // Build the full path
            string texturePath = $"images/musicNotes/{textureName}";
            Logger.Log($"[DrawableMusicNote] Note '{character}' → natural '{naturalNote}' → texture '{textureName}'. Path: {texturePath}", LoggingTarget.Runtime, LogLevel.Debug);

            var tex = textures.Get(texturePath);

            if (tex == null)
            {
                // Try fallback with .png
                tex = textures.Get($"{texturePath}.png");
            }
            
            if (tex == null)
            {
                // Fallback for missing textures
                Logger.Log($"[DrawableMusicNote] Texture not found at path: {texturePath}. Using default.", LoggingTarget.Runtime, LogLevel.Error);
                tex = textures.Get("images/A1");
            }

            noteSprite.Texture = tex;
            
            // Apply gold tint to C1 and higher notes
            if (isHighOctave)
            {
                noteSprite.Colour = Colour4.FromHex("#FFD700"); // Gold color
                sharpSymbol.Colour = Colour4.FromHex("#FFD700"); // Gold sharp symbol too
            }
            
            // Show the sharp symbol only if this is a sharp note
            if (isSharp)
            {
                sharpSymbol.Alpha = 1;
            }
        }

        /// <summary>
        /// Checks if a note is C1 or higher octave.
        /// </summary>
        private static bool IsHighOctaveNote(string noteChar)
        {
            if (string.IsNullOrEmpty(noteChar)) return false;
            
            // Extract octave number from note string (e.g., "C1", "D#1", "A2")
            // Remove sharp if present
            string cleanNote = noteChar.Replace("#", "");
            
            // Get the last character which should be the octave number
            if (cleanNote.Length > 0 && char.IsDigit(cleanNote[cleanNote.Length - 1]))
            {
                int octave = int.Parse(cleanNote[cleanNote.Length - 1].ToString());
                return octave >= 1;
            }
            
            return false;
        }

        /// <summary>
        /// Called by the scheduler when the note is hit with a known judgement.
        /// </summary>
        public void OnHit(JudgementType judgement)
        {
            wasHit = true;

            // Tint the note based on judgement quality (use centralised colour mapping)
            var tint = JudgementColors.Get(judgement);
            noteSprite.Colour = tint;

            // Burst effect at the note position
            var burst = new Circle
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.Centre,
                Size = new Vector2(12),
                Colour = tint,
                Alpha = 0.6f,
                Position = noteContainer.Position
            };
            // Add burst to the noteContainer to keep CompositeDrawable's InternalChildren count at 1
            noteContainer.Add(burst);
            burst.ScaleTo(1f, 0)
                 .Then()
                 .ScaleTo(4f, 220, Easing.OutQuint)
                 .FadeOut(220, Easing.OutQuint)
                 .Finally(_ => burst.Expire());

            // Add a brief flash of the note texture tinted by the judgement colour
            var hitFlash = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = noteSprite.Texture,
                Colour = tint,
                Alpha = 0.35f,
                Scale = noteSprite.Scale,
            };
            noteContainer.Add(hitFlash);
            var targetScale = new Vector2(noteSprite.Scale.X * 1.15f, noteSprite.Scale.Y * 1.15f);
            hitFlash.ScaleTo(targetScale, 180, Easing.OutQuint)
                    .FadeOut(180, Easing.OutQuint)
                    .Finally(_ => hitFlash.Expire());

            // Play a quick disappearance animation: pop slightly and float up while fading
            this.ScaleTo(1.0f, 0)
                .Then()
                .ScaleTo(0.92f, 120, Easing.OutQuint);

            noteContainer.MoveToOffset(new Vector2(0, -24), 160, Easing.OutQuint);
            this.FadeOut(160, Easing.OutQuint).Expire();
        }

        /// <summary>
        /// Backward-compatible hit without explicit judgement.
        /// </summary>
        public void OnHit()
        {
            OnHit(JudgementType.Good100);
        }

        protected override void Update()
        {
            base.Update();

            if (wasHit) return;

            double t = Clock.CurrentTime - TimeOffsetMs;

            // Before spawn time, stay hidden
            if (t < startTime)
            {
                noteContainer.Alpha = 0;
                return;
            }

            double duration = System.Math.Max(1, endTime - startTime);
            double p = System.Math.Clamp((t - startTime) / duration, 0.0, 1.0);

            if (p >= 1)
            {
                p = 1;
                if (!GameplayVisualSettings.enableLateVisualLinger ||
                    Clock.CurrentTime - TimeOffsetMs >= endTime + GameplayVisualSettings.lateVisualLingerMs)
                {
                    Expire();
                    return;
                }
            }

            if (p < 0.1)
                noteContainer.Alpha = (float)(p / 0.1);
            else
                noteContainer.Alpha = 1;

            float spawnX = layout.GetSpawnX(DrawSize);
            float destX = layout.GetDestinationX(DrawSize); 
            float y = layout.GetNoteY(DrawSize, noteYStep);

            float stemOffset = TypeNoteLayoutConfig.GetStemDirectionOffset(character);
            y += stemOffset;

            // Interpolate X position
            float currentX = spawnX + (float)((destX - spawnX) * p);

            noteContainer.Position = new Vector2(currentX, y);
        }
    }
}