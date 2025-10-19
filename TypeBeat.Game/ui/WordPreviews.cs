using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using TypeBeat.Game.Gameplay.Typing;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Shows three preview lines stacked vertically: top smallest, middle medium, bottom largest (current).
    /// Each preview is capped to TypingConstants.PreviewMaxLetters and maps '/' to '*'.
    /// </summary>
    public partial class WordPreviews : FillFlowContainer
    {
        private readonly SpriteText top;
        private readonly SpriteText middle;
        private readonly SpriteText bottom;

        public WordPreviews()
        {
            Direction = FillDirection.Vertical;
            AutoSizeAxes = Axes.Both;
            Spacing = new Vector2(0, 4);

            Children = new Drawable[]
            {
                top = new SpriteText { Font = new FontUsage("Kodchasan", size: 16, weight: "Bold"), Colour = Colour4.White, Alpha = 0.8f },
                middle = new SpriteText { Font = new FontUsage("Kodchasan", size: 22, weight: "Bold"), Colour = Colour4.White, Alpha = 0.9f },
                bottom = new SpriteText { Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White },
            };
        }

        public void SetPreviews(string topWord, string middleWord, string bottomWord)
        {
            top.Text = toPreview(topWord);
            middle.Text = toPreview(middleWord);
            bottom.Text = toPreview(bottomWord);
        }

        private string toPreview(string word)
        {
            if (string.IsNullOrEmpty(word)) return string.Empty;
            word = word.ToUpperInvariant();
            // Map '/' to preview glyph and limit length.
            word = word.Replace(TypingConstants.SpaceToken.ToString(), TypingConstants.PreviewSpaceGlyph.ToString());
            if (word.Length > TypingConstants.PreviewMaxLetters)
                word = word.Substring(0, TypingConstants.PreviewMaxLetters);
            return word;
        }
    }
}
