using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Ui
{
    public partial class DifficultyButton : ClickableContainer
    {
        private readonly Box background;
        private readonly Container selectionIndicator;
        private readonly SpriteText difficultyText;
        private readonly Beatmap beatmap;
        
        public event Action<Beatmap> OnSelected;

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                updateSelectionState();
            }
        }

        public DifficultyButton(Beatmap beatmap)
        {
            this.beatmap = beatmap;
            
            RelativeSizeAxes = Axes.Y; // Fill the height of the container
            AutoSizeAxes = Axes.X; // Width based on text content
            Masking = true;
            CornerRadius = 15;

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("#2C2C2C"), // Dark gray
                    Alpha = 1
                },
                difficultyText = new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = new FontUsage("Inter", size: 24, weight: "Bold"),
                    Colour = Colour4.White,
                    Spacing = new Vector2(0.25f, 0), // 25% spacing
                    Margin = new MarginPadding { Horizontal = 30, Vertical = 15 },
                    Text = (beatmap.DifficultyName ?? "Unknown").ToUpperInvariant()
                },
                // Bottom border for selection indicator
                selectionIndicator = new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.X,
                    Height = 4,
                    Alpha = 0,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.White
                    }
                }
            };
        }

        private void updateSelectionState()
        {
            if (isSelected)
            {
                background.FadeColour(Colour4.FromHex("#3C3C3C"), 200, Easing.OutQuint);
                selectionIndicator.FadeIn(200, Easing.OutQuint);
            }
            else
            {
                background.FadeColour(Colour4.FromHex("#2C2C2C"), 200, Easing.OutQuint);
                selectionIndicator.FadeOut(200, Easing.OutQuint);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (!isSelected)
                background.FadeColour(Colour4.FromHex("#3C3C3C"), 200, Easing.OutQuint);
            
            this.ScaleTo(1.05f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (!isSelected)
                background.FadeColour(Colour4.FromHex("#2C2C2C"), 200, Easing.OutQuint);
            
            this.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.95f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            OnSelected?.Invoke(beatmap);

            return base.OnClick(e);
        }
    }
}