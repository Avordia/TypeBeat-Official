using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Colour;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Online;
using TypeBeat.Game.Online.Models;

#nullable enable

namespace TypeBeat.Game.Ui
{
    public partial class Footer : Container
    {
        private readonly Box background;
        private readonly FillFlowContainer content;
        private readonly Container leftContent;
        private readonly Container rightContent;
        private readonly SpriteText clockText;
        private readonly LoginButtonStyled loginButton;
        private readonly Action onLoginRequested;
        private AuthenticationService? authService;
        private UserProfileButton? userProfileButton;

        public Footer(Action onLoginRequested)
        {
            this.onLoginRequested = onLoginRequested;
            
            RelativeSizeAxes = Axes.X;
            Height = 50;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Alpha = 0;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                },
                content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 20 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(20, 0),
                    Children = new Drawable[]
                    {
                        leftContent = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Children = new Drawable[]
                            {
                                loginButton = new LoginButtonStyled
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(150, 35)
                                }
                            }
                        },
                        rightContent = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Children = new Drawable[]
                            {
                                clockText = new SpriteText
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Text = DateTime.Now.ToString("h:mm:ss tt"),
                                    Font = new FontUsage(size: 16),
                                    Colour = Color4.White,
                                    Y = 1 // Slight adjustment for better visual centering
                                }
                            }
                        }
                    }
                }
            };

            loginButton.Action = onLoginRequested;
        }

        [BackgroundDependencyLoader]
        private void load(AuthenticationService authService)
        {
            this.authService = authService;
            
            // Subscribe to authentication state changes
            authService.CurrentUser.BindValueChanged(onUserChanged, true);
        }

        private void onUserChanged(ValueChangedEvent<UserProfile?> e)
        {
            if (e.NewValue != null)
            {
                // User is logged in
                loginButton.Hide();
                
                if (userProfileButton == null)
                {
                    userProfileButton = new UserProfileButton(e.NewValue);
                    leftContent.Add(userProfileButton);
                }
                else
                {
                    userProfileButton.Show();
                }
            }
            else
            {
                // User is not logged in
                loginButton.Show();
                userProfileButton?.Hide();
            }
        }

        protected override void Update()
        {
            base.Update();
            clockText.Text = DateTime.Now.ToString("h:mm:ss tt");
        }

        public new void Show()
        {
            this.FadeIn(300, Easing.OutQuint);
        }

        public new void Hide()
        {
            this.FadeOut(300, Easing.OutQuint);
        }

        // Nested class for styled login button matching MenuButton design
        private partial class LoginButtonStyled : ClickableContainer
        {
            private readonly Box background;
            private readonly Container trapezoidContainer;
            private readonly Container trapezoidShape;

            public LoginButtonStyled()
            {
                Masking = true;
                CornerRadius = 16f;
                BorderThickness = 6f;
                BorderColour = Color4.Black;

                var buttonColor = new Color4(102, 102, 255, 255); // Purple/Blue

                Children = new Drawable[]
                {
                    // Background with gradient
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(
                            lightenColor(buttonColor, 0.2f),
                            darkenColor(buttonColor, 0.2f)
                        )
                    },
                    // Animated hazard stripe pattern
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
                        Child = new SpriteText
                        {
                            Text = string.Join(" ", "LOGIN".ToCharArray()),
                            Font = new FontUsage("Kodchasan-Bold", size: 18),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = Color4.White,
                            Shadow = true,
                            ShadowColour = new Color4(0, 0, 0, 100)
                        }
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                startTrapezoidAnimation();
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

                return base.OnClick(e);
            }
        }
    }
}
