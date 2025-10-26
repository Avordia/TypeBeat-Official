// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Online;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Header component for the editor screen.
    /// </summary>
    public partial class EditHeader : Container
    {
        [Resolved]
        private AuthenticationService authService { get; set; }

        private SpriteText usernameText;

        [BackgroundDependencyLoader]
        private void load()
        {
            Masking = true;
            CornerRadius = 10;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 20, Vertical = 10 },
                    Children = new Drawable[]
                    {
                        // Left: Logo/Title
                        new SpriteText
                        {
                            Text = "TypeBeat Editor",
                            Font = FontUsage.Default.With(family: "Inter-Bold", size: 24),
                            Colour = Color4.White,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft
                        },
                        // Center: Username
                        usernameText = new SpriteText
                        {
                            Font = FontUsage.Default.With(family: "Inter", size: 18),
                            Colour = Color4.White,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        }
                    }
                }
            };

            // Subscribe to user changes
            authService.CurrentUser.BindValueChanged(e =>
            {
                usernameText.Text = e.NewValue != null ? $"User: {e.NewValue.Username}" : "User: Not logged in";
            }, true);
        }
    }
}

