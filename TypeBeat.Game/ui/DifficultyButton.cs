using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Colour;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Ui
{
    public partial class DifficultyButton : ClickableContainer
    {
        private readonly Box background;
        private readonly Container trapezoidContainer;
        private readonly Container trapezoidShape;
        private readonly SpriteText difficultyText;
        private readonly Beatmap beatmap;
        private readonly Container outerContainer;

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

            // Get the base color for this difficulty
            var baseColor = getStarRatingColour(beatmap.StarRating);

            Child = outerContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 16f,
                BorderThickness = 6f,
                BorderColour = Color4.Black,
                Children = new Drawable[]
                {
                    // Background with gradient (lighter on left, darker on right)
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(
                            lightenColor(baseColor, 0.2f),
                            darkenColor(baseColor, 0.2f)
                        )
                    },
                    // Animated hazard stripe pattern container
                    trapezoidContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = trapezoidShape = new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.Y,
                            Width = 2000,
                            X = -1000,
                            Child = new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Spacing = new Vector2(10, 0),
                                Children = createHazardPattern()
                            }
                        }
                    },
                    // Text
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Child = difficultyText = new SpriteText
                        {
                            Text = addLetterSpacing(beatmap.DifficultyName ?? "Unknown"),
                            Font = new FontUsage("Kodchasan-Bold", size: 18),
                            Padding = new MarginPadding { Horizontal = 30, Vertical = 8 },
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = Color4.White,
                            Shadow = true,
                            ShadowColour = new Color4(0, 0, 0, 100)
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            startTrapezoidAnimation();
        }

        private string addLetterSpacing(string text)
        {
            return string.Join(" ", text.ToUpper().ToCharArray());
        }

        private Color4 lightenColor(Color4 color, float amount)
        {
            return new Color4(
                Math.Min(color.R + amount, 1.0f),
                Math.Min(color.G + amount, 1.0f),
                Math.Min(color.B + amount, 1.0f),
                color.A
            );
        }

        private Color4 darkenColor(Color4 color, float amount)
        {
            return new Color4(
                Math.Max(color.R - amount, 0.0f),
                Math.Max(color.G - amount, 0.0f),
                Math.Max(color.B - amount, 0.0f),
                color.A
            );
        }

        private Drawable[] createHazardPattern()
        {
            var trapezoids = new List<Drawable>();
            const int trapezoidCount = 25;
            const float trapezoidWidth = 50f;

            for (int i = 0; i < trapezoidCount; i++)
            {
                trapezoids.Add(new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = trapezoidWidth,
                    Shear = new Vector2(0.4f, 0),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0.25f
                    }
                });
            }

            return trapezoids.ToArray();
        }

        private void startTrapezoidAnimation()
        {
            trapezoidShape.Loop(d => d
                .MoveTo(new Vector2(-1000, 0), 0)
                .Then()
                .MoveTo(new Vector2(0, 0), 6000)
            );
        }

        private Color4 getStarRatingColour(double starRating)
        {
            // Every 2.5 star rating increase = new color
            // 0-2.5 = Purple/Blue, 2.5-5.0 = Magenta, 5.0-7.5 = Orange, 7.5+ = Red

            if (starRating < 2.5)
                return new Color4(102, 102, 255, 255); // Purple/Blue
            else if (starRating < 5.0)
                return new Color4(255, 0, 255, 255); // Magenta
            else if (starRating < 7.5)
                return new Color4(255, 136, 0, 255); // Orange
            else
                return new Color4(255, 85, 85, 255); // Red
        }

        private void updateSelectionState()
        {
            var baseColor = getStarRatingColour(beatmap.StarRating);

            if (isSelected)
            {
                // Use star rating-based color gradient when selected
                background.FadeColour(
                    ColourInfo.GradientHorizontal(
                        lightenColor(baseColor, 0.2f),
                        darkenColor(baseColor, 0.2f)
                    ), 200, Easing.OutQuint);
            }
            else
            {
                // Darker/desaturated when not selected
                var desaturatedColor = new Color4(
                    baseColor.R * 0.4f,
                    baseColor.G * 0.4f,
                    baseColor.B * 0.4f,
                    baseColor.A
                );
                background.FadeColour(
                    ColourInfo.GradientHorizontal(
                        lightenColor(desaturatedColor, 0.1f),
                        darkenColor(desaturatedColor, 0.1f)
                    ), 200, Easing.OutQuint);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            this.ScaleTo(1.1f, 200, Easing.OutQuint);
            background.FadeTo(0.8f, 200);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.ScaleTo(1f, 200, Easing.OutQuint);
            background.FadeTo(1f, 200);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.9f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            OnSelected?.Invoke(beatmap);

            return base.OnClick(e);
        }
    }
}