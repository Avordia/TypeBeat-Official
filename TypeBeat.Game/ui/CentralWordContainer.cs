using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using TypeBeat.Game.Gameplay.Typing;
using System.Linq;

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
            Width = 0.35f;
            Height = 64; // fixed height for visibility

            Masking = true;
            CornerRadius = 32; // More rounded corners to match Figma design

            InternalChildren = new Drawable[]
            {
                // Red gradient background
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("#E63946"), // Red color from Figma
                    Alpha = 1.0f
                },
                // Animated trapezoid container (masked by parent)
                trapezoidContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = trapezoidShape = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(150, 64), // Trapezoid size
                        X = -200, // Start off-screen to the left
                        Shear = new Vector2(0.3f, 0), // Creates trapezoid shape
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.White,
                            Alpha = 0.1f // 10% opacity
                        }
                    }
                },
                wordText = new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = new FontUsage("Kodchasan", size: 40, weight: "Bold"),
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

        private void startTrapezoidAnimation()
        {
            // Animate trapezoid moving from left to right continuously
            trapezoidShape.Loop(d => d
                .MoveTo(new Vector2(-200, 0), 0) // Start position (off-screen left)
                .Then()
                .MoveTo(new Vector2(DrawWidth + 200, 0), 3000, Easing.InOutSine) // Move to right (off-screen)
            );
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
