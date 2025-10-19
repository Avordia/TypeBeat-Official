using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using TypeBeat.Game.Gameplay.Typing;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays the current word to type in uppercase. Shrinks in width as letters are consumed.
    /// Turns gold when only the space token remains.
    /// </summary>
    public partial class CentralWordContainer : Container
    {
        private readonly Box background;
        private readonly Container trapezoidContainer;
        private readonly Container trapezoidShape;
        private readonly SpriteText wordText;

        private string fullWord = string.Empty;
        private int consumed = 0;

        public CentralWordContainer()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            // Keep a consistent bar height even if text is empty (e.g., when '/' is hidden)
            AutoSizeAxes = Axes.None;
            RelativeSizeAxes = Axes.X;
            Width = 0.45f; // Bigger container (was 0.35f)
            Height = 80; // Taller height (was 64)

            Masking = true;
            CornerRadius = 40; // Adjusted for new size

            InternalChildren = new Drawable[]
            {
                // Red gradient background
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("#E63946"), // Red color from Figma
                    Alpha = 1.0f
                },
                // Animated trapezoid container (masked by parent) - Hazard pattern
                trapezoidContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = trapezoidShape = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Y,
                        Width = 2000, // Wide enough to hold multiple trapezoids
                        X = -1000, // Start off-screen to the left
                        Child = new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Spacing = new Vector2(10, 0), // Gap between trapezoids
                            Children = createHazardPattern()
                        }
                    }
                },
                // White border (bolder)
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 40,
                    BorderThickness = 6, // Bolder border (was 3)
                    BorderColour = Colour4.White,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    }
                },
                wordText = new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = new FontUsage("Kodchasan", size: 52, weight: "Bold"), // Bigger text (was 40)
                    Colour = Colour4.White,
                    Spacing = new Vector2(0.1f, 0) // 10% spacing
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            startTrapezoidAnimation();
        }

        private Drawable[] createHazardPattern()
        {
            // Create multiple trapezoids for hazard stripe pattern
            var trapezoids = new List<Drawable>();
            const int trapezoidCount = 20; // Enough to fill the width multiple times
            const float trapezoidWidth = 60f;
            
            for (int i = 0; i < trapezoidCount; i++)
            {
                trapezoids.Add(new Container
                {
                    Size = new Vector2(trapezoidWidth, 80), // Match new height
                    Shear = new Vector2(0.4f, 0), // Creates trapezoid/stripe shape
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.White,
                        Alpha = 0.1f // 10% opacity
                    }
                });
            }
            
            return trapezoids.ToArray();
        }

        private void startTrapezoidAnimation()
        {
            // Animate hazard pattern moving from left to right continuously (slower)
            trapezoidShape.Loop(d => d
                .MoveTo(new Vector2(-1000, 0), 0) // Start position (off-screen left)
                .Then()
                .MoveTo(new Vector2(0, 0), 12000, Easing.InOutSine) // Slower movement (was 4000ms, now 8000ms)
            );
        }

        public void PlayBounceEffect()
        {
            // Subtle bounce effect on key press
            this.ScaleTo(1.05f, 100, Easing.OutQuint) // Scale up slightly
                .Then()
                .ScaleTo(1.0f, 300, Easing.OutElastic); // Bounce back with elastic easing
        }

        public void SetWord(string wordUppercase)
        {
            fullWord = wordUppercase ?? string.Empty;
            consumed = 0;
            updateText();
            updateWidth();
            updateColour();
        }

        public void ConsumeNext()
        {
            if (consumed < fullWord.Length)
            {
                consumed++;
                updateText();
                updateWidth();
                updateColour();
            }
        }

        private void updateText()
        {
            var remaining = consumed >= fullWord.Length ? string.Empty : fullWord[consumed..];
            // Do not display the space token ('/') in the center word.
            string display = remaining.Replace(TypingConstants.SpaceToken.ToString(), string.Empty);
            wordText.Text = display;
        }

        private void updateWidth()
        {
            // Fixed width when only the space token ('/') remains, or when the whole word is just '/'.
            const float fixed_slash_width = 0.20f; // tweakable: a pleasant width for the gold state

            string remaining = consumed >= fullWord.Length ? string.Empty : fullWord[consumed..];
            bool onlySpace = remaining.Length == 1 && remaining[0] == TypingConstants.SpaceToken;
            bool isOnlySpaceWord = fullWord.Length == 1 && fullWord[0] == TypingConstants.SpaceToken;

            if (onlySpace || isOnlySpaceWord)
            {
                Width = fixed_slash_width;
                return;
            }

            // Otherwise shrink based on remaining LETTER count (exclude '/').
            remaining = consumed >= fullWord.Length ? string.Empty : fullWord[consumed..];
            int remainingCount = remaining.Count(ch => ch != TypingConstants.SpaceToken);
            // Minimum and maximum width ratios.
            float min = 0.15f;
            float max = 0.40f;
            // Map remaining letters (0..10) into [min,max]. Clamp for sanity.
            float t = remainingCount / 10f;
            if (t < 0) t = 0; if (t > 1) t = 1;
            Width = min + (max - min) * t;
        }

        private void updateColour()
        {
            // Turn gold if only the space token remains (or if the full word is exactly the space token).
            string remaining = consumed >= fullWord.Length ? string.Empty : fullWord[consumed..];
            bool onlySpace = remaining.Length == 1 && remaining[0] == TypingConstants.SpaceToken;
            bool isOnlySpaceWord = fullWord.Length == 1 && fullWord[0] == TypingConstants.SpaceToken;
            if (onlySpace || isOnlySpaceWord)
                background.Colour = Colour4.Gold;
            else
                background.Colour = Colour4.FromHex("#E63946"); // Red color from Figma
        }
    }
}
