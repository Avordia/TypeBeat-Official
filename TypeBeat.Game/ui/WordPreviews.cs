using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using TypeBeat.Game.Gameplay.Typing;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Shows 3 word previews stacked vertically with gradual size and opacity increase.
    /// Creates depth effect as words approach current position.
    /// </summary>
    public partial class WordPreviews : FillFlowContainer
    {
        private readonly SpriteText word4; // Furthest (smallest, most transparent)
        private readonly SpriteText word3; // Middle
        private readonly SpriteText word2; // Closest (largest, most visible)

        public WordPreviews()
        {
            Direction = FillDirection.Vertical;
            AutoSizeAxes = Axes.Both;
            Spacing = new Vector2(0, 6);

            Children = new Drawable[]
            {
                // Word 4: furthest away (smallest, most transparent)
                word4 = new SpriteText 
                { 
                    Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"), 
                    Colour = Colour4.White, 
                    Alpha = 0.35f,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                // Word 3: middle (medium size, medium transparency)
                word3 = new SpriteText 
                { 
                    Font = new FontUsage("Kodchasan", size: 48, weight: "Bold"), 
                    Colour = Colour4.White, 
                    Alpha = 0.55f,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                // Word 2: closest/next (largest, most visible)
                word2 = new SpriteText 
                { 
                    Font = new FontUsage("Kodchasan", size: 64, weight: "Bold"), 
                    Colour = Colour4.White, 
                    Alpha = 0.85f,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
            };
        }

        public void SetPreviews(string nextWord, string word2After, string word3After)
        {
            // Map words to display positions (bottom to top)
            word2.Text = toPreview(nextWord);      // Largest, most visible (bottom)
            word3.Text = toPreview(word2After);    // Medium (middle)
            word4.Text = toPreview(word3After);    // Smallest, most transparent (top)
        }

        private string toPreview(string word)
        {
            if (string.IsNullOrEmpty(word)) return string.Empty;
            word = word.ToUpperInvariant();
            // Remove slash characters (/ and *) - they look annoying in preview
            word = word.Replace("/", "").Replace("*", "");
            if (word.Length > TypingConstants.PreviewMaxLetters)
                word = word.Substring(0, TypingConstants.PreviewMaxLetters);
            return word;
        }
    }
}
