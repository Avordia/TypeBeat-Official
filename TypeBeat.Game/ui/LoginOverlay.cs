using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using Colour4 = osu.Framework.Graphics.Colour4;
using TypeBeat.Game.Online;

namespace TypeBeat.Game.Ui
{
    public partial class LoginOverlay : FocusedOverlayContainer
    {
        private readonly AuthenticationService authService;
        private readonly BasicTextBox usernameTextBox;
        private readonly BasicTextBox emailTextBox;
        private readonly PasswordTextBox passwordTextBox;
        private readonly SpriteText errorText;
        private readonly Container loginTab;
        private readonly Container registerTab;
        private readonly ClickableContainer submitButton;
        private readonly SpriteText submitText;
        private readonly Container mainContainer;
        private readonly Container emailFieldContainer;
        private readonly SpriteText emailLabel;
        
        private bool isLoginMode = true;

        public LoginOverlay(AuthenticationService authService)
        {
            this.authService = authService;
            
            RelativeSizeAxes = Axes.Both;
            Depth = float.MinValue + 1;
            Alpha = 0;

            Children = new Drawable[]
            {
                // Semi-transparent background overlay
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                    Alpha = 0.7f
                },
                    // Main container with folder-tab style
                    mainContainer = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(600, 400),
                    Children = new Drawable[]
                    {
                        // Tab buttons (folder tabs)
                        new Container
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.BottomLeft,
                            Y = -5,
                            Size = new Vector2(600, 50),
                            Children = new Drawable[]
                            {
                                loginTab = new Container
                                {
                                    Size = new Vector2(250, 50),
                                    Masking = true,
                                    CornerRadius = 15f,
                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                    {
                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                        Colour = Colour4.Black.Opacity(0.5f),
                                        Radius = 5
                                    },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.Orange
                                        },
                                        new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "LOGIN",
                                            Font = new FontUsage(size: 20, weight: "Bold"),
                                            Colour = Colour4.White
                                        }
                                    }
                                },
                                registerTab = new Container
                                {
                                    X = 260,
                                    Size = new Vector2(250, 50),
                                    Masking = true,
                                    CornerRadius = 15f,
                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                    {
                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                        Colour = Colour4.Black.Opacity(0.5f),
                                        Radius = 5
                                    },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.FromHex("#EE1144")
                                        },
                                        new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "REGISTER",
                                            Font = new FontUsage(size: 20, weight: "Bold"),
                                            Colour = Colour4.White
                                        }
                                    }
                                }
                            }
                        },
                        // Main content box
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 25f,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Colour4.Black.Opacity(0.8f),
                                Radius = 15
                            },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.FromHex("#2A2A2A")
                                },
                                // Form fields container
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(500, 250),
                                    Children = new Drawable[]
                                    {
                                        // Username label
                                        new SpriteText
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Y = 20,
                                            Text = "Username:",
                                            Font = new FontUsage(size: 20),
                                            Colour = Colour4.White
                                        },
                                        // Username textbox
                                        new Container
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Position = new Vector2(200, 10),
                                            Size = new Vector2(290, 40),
                                            Masking = true,
                                            CornerRadius = 10f,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Colour4.FromHex("#C0C0C0")
                                                },
                                                usernameTextBox = new BasicTextBox
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    PlaceholderText = "Username or Email",
                                                    CommitOnFocusLost = true,
                                                    ReleaseFocusOnCommit = false
                                                }
                                            }
                                        },
                                        // Password label
                                        new SpriteText
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Y = 90,
                                            Text = "Password:",
                                            Font = new FontUsage(size: 20),
                                            Colour = Colour4.White
                                        },
                                        // Password textbox
                                        new Container
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Position = new Vector2(200, 80),
                                            Size = new Vector2(290, 40),
                                            Masking = true,
                                            CornerRadius = 10f,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Colour4.FromHex("#C0C0C0")
                                                },
                                                passwordTextBox = new PasswordTextBox
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    PlaceholderText = "****",
                                                    CommitOnFocusLost = true,
                                                    ReleaseFocusOnCommit = false
                                                }
                                            }
                                        },
                                        // Email label (for registration only)
                                        emailLabel = new SpriteText
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Y = 160,
                                            Text = "Email:",
                                            Font = new FontUsage(size: 20),
                                            Colour = Colour4.White,
                                            Alpha = 0 // Hidden by default (login mode)
                                        },
                                        // Email textbox (for registration only)
                                        emailFieldContainer = new Container
                                        {
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            Position = new Vector2(200, 150),
                                            Size = new Vector2(290, 40),
                                            Masking = true,
                                            CornerRadius = 10f,
                                            Alpha = 0, // Hidden by default (login mode)
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Colour4.FromHex("#C0C0C0")
                                                },
                                                emailTextBox = new BasicTextBox
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    PlaceholderText = "jane.doe@example.com",
                                                    CommitOnFocusLost = true,
                                                    ReleaseFocusOnCommit = false
                                                }
                                            }
                                        },
                                        // Submit button
                                        submitButton = new ClickableContainer
                                        {
                                            Anchor = Anchor.BottomRight,
                                            Origin = Anchor.BottomRight,
                                            Size = new Vector2(200, 45),
                                            Masking = true,
                                            CornerRadius = 10f,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Colour4.FromHex("#7B68EE")
                                                },
                                                submitText = new SpriteText
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Text = "L O G I N",
                                                    Font = new FontUsage(size: 18, weight: "Bold"),
                                                    Colour = Colour4.White
                                                }
                                            }
                                        },
                                        // Error text
                                        errorText = new SpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Text = "",
                                            Font = new FontUsage(size: 14),
                                            Colour = Colour4.Red,
                                            Alpha = 0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Wire up tab clicks - make tabs clickable
            loginTab.Add(new ClickableContainer
            {
                RelativeSizeAxes = Axes.Both,
                Action = () => setMode(true)
            });
            
            registerTab.Add(new ClickableContainer
            {
                RelativeSizeAxes = Axes.Both,
                Action = () => setMode(false)
            });
            
            submitButton.Action = onSubmit;
        }

        private void setMode(bool login)
        {
            isLoginMode = login;

            if (login)
            {
                // Animate LOGIN tab to front (folder style)
                loginTab.Children[0].FadeColour(Colour4.Orange, 300, Easing.OutQuint);
                registerTab.Children[0].FadeColour(Colour4.FromHex("#AA0022"), 300, Easing.OutQuint);
                
                // Move LOGIN tab up (appears on top like a folder tab)
                loginTab.MoveToY(0, 300, Easing.OutQuint);
                registerTab.MoveToY(10, 300, Easing.OutQuint);
                
                // Scale up LOGIN tab slightly to appear in front
                loginTab.ScaleTo(1.05f, 300, Easing.OutQuint);
                registerTab.ScaleTo(1.0f, 300, Easing.OutQuint);
                
                // Hide email field for login
                emailLabel.FadeOut(300, Easing.OutQuint);
                emailFieldContainer.FadeOut(300, Easing.OutQuint);
                
                submitText.Text = "L O G I N";
            }
            else
            {
                // Animate REGISTER tab to front (folder style)
                loginTab.Children[0].FadeColour(Colour4.FromHex("#CC5500"), 300, Easing.OutQuint);
                registerTab.Children[0].FadeColour(Colour4.FromHex("#EE1144"), 300, Easing.OutQuint);
                
                // Move REGISTER tab up (appears on top like a folder tab)
                loginTab.MoveToY(10, 300, Easing.OutQuint);
                registerTab.MoveToY(0, 300, Easing.OutQuint);
                
                // Scale up REGISTER tab slightly to appear in front
                loginTab.ScaleTo(1.0f, 300, Easing.OutQuint);
                registerTab.ScaleTo(1.05f, 300, Easing.OutQuint);
                
                // Show email field for registration
                emailLabel.FadeIn(300, Easing.OutQuint);
                emailFieldContainer.FadeIn(300, Easing.OutQuint);
                
                submitText.Text = "R E G I S T E R";
            }
        }

        private async void onSubmit()
        {
            var usernameOrEmail = usernameTextBox.Text;
            var password = passwordTextBox.Text;

            if (isLoginMode)
            {
                // Login mode - username or email can be used
                if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
                {
                    showError("Please fill in all fields");
                    return;
                }

                var (success, message) = await authService.LoginAsync(usernameOrEmail, password);

                if (success)
                {
                    Hide();
                }
                else
                {
                    showError(message);
                }
            }
            else
            {
                // Registration mode - need username, email, and password
                var email = emailTextBox.Text;
                
                if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    showError("Please fill in all fields");
                    return;
                }

                // For registration, the first field should be username only
                var (success, message) = await authService.RegisterAsync(usernameOrEmail, email, password);

                if (success)
                {
                    Hide();
                }
                else
                {
                    showError(message);
                }
            }
        }

        private void showError(string message)
        {
            errorText.Text = message;
            errorText.FadeIn(200).Then().Delay(3000).FadeOut(200);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Escape)
            {
                Hide();
                return true;
            }
            return base.OnKeyDown(e);
        }

        protected override void PopIn()
        {
            this.FadeIn(200);
            Schedule(() => GetContainingFocusManager()?.ChangeFocus(usernameTextBox));
        }

        protected override void PopOut()
        {
            this.FadeOut(200);
        }
    }

    // Custom password textbox that displays asterisks
    public partial class PasswordTextBox : BasicTextBox
    {
        protected override Drawable GetDrawableCharacter(char c) => new FallingDownContainer
        {
            AutoSizeAxes = Axes.Both,
            Child = new SpriteText { Text = "*", Font = FrameworkFont.Condensed.With(size: 30) }
        };
    }
}
