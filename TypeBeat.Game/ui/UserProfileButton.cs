using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Online.Models;

#nullable enable

namespace TypeBeat.Game.Ui
{
    public partial class UserProfileButton : ClickableContainer
    {
        private readonly UserProfile userProfile;
        private readonly Sprite avatarSprite;
        private readonly SpriteText usernameText;
        private readonly Container avatarContainer;

        public UserProfileButton(UserProfile profile)
        {
            userProfile = profile;
            
            Logger.Log($"[DEBUG] ✓ Creating UserProfileButton for: {profile.Username}", LoggingTarget.Runtime, LogLevel.Important);
            
            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 5f;

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10, 0),
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        avatarContainer = new CircularContainer
                        {
                            Size = new Vector2(35, 35),
                            Masking = true,
                            BorderThickness = 2,
                            BorderColour = Color4.White,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Gray,
                                    Alpha = 0.5f
                                },
                                avatarSprite = new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    FillMode = FillMode.Fill,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre
                                }
                            }
                        },
                        usernameText = new SpriteText
                        {
                            Text = profile.Username,
                            Font = new FontUsage(size: 18, weight: "Bold"),
                            Colour = Color4.White,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft
                        }
                    }
                }
            };

            loadAvatar();
        }

        private TextureStore? textureStore;
        private IRenderer? renderer;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures, IRenderer gameRenderer)
        {
            textureStore = textures;
            renderer = gameRenderer;
            // Load default avatar if custom avatar fails
            loadDefaultAvatar();
        }

        private async void loadAvatar()
        {
            try
            {
                if (string.IsNullOrEmpty(userProfile.AvatarUrl))
                {
                    Logger.Log($"[DEBUG] No avatar URL for user {userProfile.Username}, using default", LoggingTarget.Runtime, LogLevel.Important);
                    loadDefaultAvatar();
                    return;
                }

                Logger.Log($"[DEBUG] Loading avatar from URL: {userProfile.AvatarUrl}", LoggingTarget.Runtime, LogLevel.Important);

                // Download the image from the URL
                using (var httpClient = new HttpClient())
                {
                var imageBytes = await httpClient.GetByteArrayAsync(userProfile.AvatarUrl);
                    Logger.Log($"[DEBUG] ✓ Downloaded avatar image: {imageBytes.Length} bytes", LoggingTarget.Runtime, LogLevel.Important);

                    // Convert to texture
                    Schedule(() =>
                    {
                        if (renderer != null)
                        {
                            // Create a temporary stream from the bytes
                            using (var stream = new MemoryStream(imageBytes))
                            {
                                var texture = Texture.FromStream(renderer, stream);
                avatarSprite.Texture = texture;
                                avatarSprite.Alpha = 1.0f; // Make sure it's visible
                                Logger.Log($"[DEBUG] ✓ Avatar texture loaded and applied. Sprite alpha: {avatarSprite.Alpha}, Texture size: {texture.Width}x{texture.Height}", LoggingTarget.Runtime, LogLevel.Important);
                            }
                        }
                        else
                        {
                            Logger.Log($"[DEBUG] ✗ Renderer is null, cannot create texture", LoggingTarget.Runtime, LogLevel.Error);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[DEBUG] ✗ Failed to load avatar: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                loadDefaultAvatar();
            }
        }

        private void loadDefaultAvatar()
        {
            // Create a simple gradient circle as default avatar
            Schedule(() =>
            {
                if (textureStore != null)
                {
                    var texture = textureStore.Get("Textures/logo");
                    if (texture != null)
                    {
                        avatarSprite.Texture = texture;
                    }
                    else
                    {
                        // Fallback to colored circle
                        avatarSprite.Colour = Color4.Purple;
                    }
                }
                else
                {
            avatarSprite.Colour = Color4.Purple;
                }
            });
        }

        protected override bool OnHover(HoverEvent e)
        {
            this.ScaleTo(1.05f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.95f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);
            
            return base.OnClick(e);
        }
    }
}
