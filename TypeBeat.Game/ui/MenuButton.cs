using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;

namespace TypeBeat.Game.Ui
{
    public partial class MenuButton : ClickableContainer
    {
        private readonly Box background;
        private readonly Container trapezoidContainer;
        private readonly Container trapezoidShape;
        private readonly SpriteText text;
        private readonly Screen targetScreen;
        private readonly Action customAction;
        private readonly Container outerContainer;

        public Color4 ButtonColor
        {
            set => background.FadeColour(value, 200);
        }

        public string Text
        {
            get => text.Text.ToString();
            set => text.Text = addLetterSpacing(value);
        }

        public float TextSize
        {
            set => text.Font = text.Font.With(size: value);
        }

        public MenuButton(string buttonText, Color4 color, float size = 30f, Screen target = null, Action onClick = null, Vector2? dimensions = null)
        {
            targetScreen = target;
            customAction = onClick;

            if (dimensions.HasValue)
            {
                Width = dimensions.Value.X;
                Height = dimensions.Value.Y;
                RelativeSizeAxes = Axes.None;
            }
            else
            {
                AutoSizeAxes = Axes.Both;
            }

            // Outer container for button shape
            Child = outerContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 16f,
                BorderThickness = 6f, // Thicker border
                BorderColour = Color4.Black,
                Children = new Drawable[]
                {
                    // Background with gradient (lighter on left, darker on right)
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(
                            lightenColor(color, 0.2f), // Lighter on left
                            darkenColor(color, 0.2f)   // Darker on right
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
                        RelativeSizeAxes = Axes.Both,
                        Child = text = new SpriteText
                        {
                            Text = addLetterSpacing(buttonText),
                            Font = new FontUsage("Kodchasan-Bold", size: size),
                            Padding = new MarginPadding { Horizontal = 60, Vertical = 6 },
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

            if (targetScreen != null)
            {
                var current = Parent;
                while (current != null && !(current is Screen))
                    current = current.Parent;

                if (current is Screen screen)
                    screen.Push(targetScreen);
            }

            customAction?.Invoke();
            return base.OnClick(e);//
        }
    }
}