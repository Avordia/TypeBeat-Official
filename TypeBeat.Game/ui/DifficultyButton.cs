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
            
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            AutoSizeAxes = Axes.Both; 

            Children = new Drawable[]
            {
                // Outer container for border
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 14,
                    Children = new Drawable[]
                    {
                        // Border box
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.FromHex("#1C1C1C"),
                        },
                        // Content container with inner background
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Margin = new MarginPadding(2), // Border thickness
                            Masking = true,
                            CornerRadius = 12,
                            Children = new Drawable[]
                            {
                                background = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.FromHex("#373737"),
                                },
                                difficultyText = new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Font = new FontUsage("Inter-Bold", size: 18),
                                    Colour = Colour4.White,
                                    Spacing = new Vector2(0.25f, 0),
                                    Margin = new MarginPadding { Horizontal = 30, Vertical = 8 },
                                    Text = (beatmap.DifficultyName ?? "Unknown").ToUpperInvariant()
                                }
                            }
                        }
                    }
                }
            };
        }

        private Colour4 getStarRatingColour(double starRating)
        {
            // Based on HealthBar gradient colors (reversed)
            // Every 2.5 star rating increase = new color
            // 0-2.5 = Purple/Blue, 2.5-5.0 = Pink/Magenta, 5.0-7.5 = Orange, 7.5+ = Red
            
            if (starRating < 2.5)
                return Colour4.FromHex("#6666DD"); // Purple/Blue
            else if (starRating < 5.0)
                return Colour4.FromHex("#CC6699"); // Pink/Magenta
            else if (starRating < 7.5)
                return Colour4.FromHex("#FF8033"); // Orange
            else
                return Colour4.FromHex("#FF3333"); // Red
        }

        private void updateSelectionState()
        {
            if (isSelected)
            {
                // Use star rating-based color when selected
                background.FadeColour(getStarRatingColour(beatmap.StarRating), 200, Easing.OutQuint);
            }
            else
            {
                background.FadeColour(Colour4.FromHex("#373737"), 200, Easing.OutQuint);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (!isSelected)
                background.FadeColour(Colour4.FromHex("#4C4C4C"), 200, Easing.OutQuint);
            
            this.ScaleTo(1.05f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (!isSelected)
                background.FadeColour(Colour4.FromHex("#373737"), 200, Easing.OutQuint);
            
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