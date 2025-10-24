using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
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
        private readonly ClickableContainer loginButton;
        private readonly Action onLoginRequested;
        private AuthenticationService? authService;
        private UserProfileButton? userProfileButton;

        public Footer(Action onLoginRequested)
        {
            this.onLoginRequested = onLoginRequested;
            
            RelativeSizeAxes = Axes.X;
            Height = 35;
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
                                loginButton = new ClickableContainer
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(120, 25),
                                    Masking = true,
                                    CornerRadius = 5f,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Purple
                                        },
                                        new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "LOGIN",
                                            Font = new FontUsage(size: 16),
                                            Colour = Color4.White
                                        }
                                    }
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
                                    Colour = Color4.White
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
    }
}
