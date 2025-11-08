using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK; 
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Filehandling;
using TypeBeat.Game.Ui;
using TypeBeat.Game.Online;
using TypeBeat.Game.Editor;

namespace TypeBeat.Game
{
    public partial class MainScreen : Screen
    {
        private Track track;
        private Drawable background;
        private Container backgroundContainer;
        private BeatpackManager beatpackManager;
        private AudioManager audioManager;
        private GameHost host;
        private SpriteText songTitleText;
        private Sprite frameworkCredit;
        private BeatReactiveSprite mainLogo;
        private MenuPlayer menuPlayer;
        private Header header;
        private Footer footer;
        private LoginOverlay loginOverlay;
        private Container inDevelopmentOverlay;
        private AuthenticationService authService;
        
        private bool isInMenuMode;
        private bool isMenuTransitioning;
        private bool shouldAutoPlayTrack = true; // Control whether beatpackChanged should auto-start track
        private Dictionary<Drawable, float> initialPositions;
        private const float menu_offset_x = 350f;  
        private const float animation_duration = 400;
        private const float header_peek_y = -8f;
        private const float footer_peek_y = 8f;

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio, TextureStore textures, AuthenticationService authService)
        {
            this.host = host;
            audioManager = audio;
            this.authService = authService;
            InternalChildren = new Drawable[]
            {
                beatpackManager = new BeatpackManager(),
                backgroundContainer = new Container { RelativeSizeAxes = Axes.Both },
                header = new Header
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Y = header_peek_y,
                    Alpha = 0,
                },
                footer = new Footer(() => loginOverlay.Show())
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = footer_peek_y,
                    Alpha = 0,
                },
                loginOverlay = new LoginOverlay(authService)
                {
                    Depth = float.MinValue + 1,
                    Alpha = 0
                },
                inDevelopmentOverlay = new Container
                {
                    Name = "In Development Overlay",
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MinValue,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(0, 0, 0, 200)
                        },
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(500, 300),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = new Color4(30, 30, 30, 255)
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(20),
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Anchor = Anchor.TopCentre,
                                            Origin = Anchor.TopCentre,
                                            Text = "🚧 IN DEVELOPMENT 🚧",
                                            Font = new FontUsage("Inter", size: 32, weight: "Bold"),
                                            Colour = new Color4(255, 165, 0, 255),
                                            Y = 60
                                        },
                                        new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "The Editor is coming soon!",
                                            Font = new FontUsage("Inter", size: 20),
                                            Colour = Color4.White,
                                            Y = 20
                                        },
                                        new MenuButton("Close", new Color4(100, 100, 100, 255), 20f, dimensions: new Vector2(150, 40), onClick: () =>
                                        {
                                            inDevelopmentOverlay.FadeOut(200);
                                        })
                                        {
                                            Anchor = Anchor.BottomCentre,
                                            Origin = Anchor.BottomCentre,
                                            Y = -20
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new Container
                {
                    Name = "Menu Buttons",
                    Alpha = 0, 
                    Position = new Vector2(250, 250), 
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Y = 0,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Solo", new Color4(255, 136, 0, 255), 24f, dimensions: new Vector2(220, 35), onClick: () => 
                            {
                                mainLogo.FadeOut(400);
                                songTitleText.FadeOut(400);
                                menuPlayer.FadeOut(400);
                                frameworkCredit.FadeOut(400);

                                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                                menuButtonsContainer?.FadeOut(400);

                                this.Delay(400).Schedule(() =>
                                {
                                    // Disable auto-play when navigating away
                                    shouldAutoPlayTrack = false;
                                    
                                    RemoveInternal(backgroundContainer, false);
                                    var songSelection = new SongSelectionScreen(beatpackManager, backgroundContainer, this, track);
                                    this.Push(songSelection);
                                });
                            })
                        },
                        new Container
                        {
                            Y = 50,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Multi", new Color4(135, 206, 235, 255), 24f, dimensions: new Vector2(220, 35), onClick: () =>
                            {
                                // Check if user is logged in
                                if (!authService.IsLoggedIn)
                                {
                                    loginOverlay.Show();
                                    return;
                                }
                                
                                mainLogo.FadeOut(400);
                                songTitleText.FadeOut(400);
                                menuPlayer.FadeOut(400);
                                frameworkCredit.FadeOut(400);

                                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                                menuButtonsContainer?.FadeOut(400);

                                this.Delay(400).Schedule(() =>
                                {
                                    // Disable auto-play when navigating away
                                    shouldAutoPlayTrack = false;
                                    
                                    // Navigate to multiplayer screen
                                    this.Push(new MultiplayerScreen());
                                });
                            })
                        },
                        new Container
                        {
                            Y = 100,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Create", new Color4(100, 100, 100, 255), 24f, dimensions: new Vector2(220, 35), onClick: () => 
                            {
                                // Show In Development overlay
                                inDevelopmentOverlay.FadeIn(200);
                            })
                        },
                        new Container
                        {
                            Y = 150,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Explore", new Color4(255, 85, 85, 255), 24f, dimensions: new Vector2(220, 35), onClick: () =>
                            {
                                mainLogo.FadeOut(400);
                                songTitleText.FadeOut(400);
                                menuPlayer.FadeOut(400);
                                frameworkCredit.FadeOut(400);

                                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                                menuButtonsContainer?.FadeOut(400);

                                this.Delay(400).Schedule(() =>
                                {
                                    // Disable auto-play when navigating away
                                    shouldAutoPlayTrack = false;
                                    
                                    // Navigate to exploration screen
                                    this.Push(new ExplorationScreen());
                                });
                            })
                        },
                        new Container
                        {
                            Y = 200,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Options", new Color4(102, 102, 255, 255), 24f, dimensions: new Vector2(220, 35))
                        }
                    }
                },
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new HoverContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                mainLogo = new BeatReactiveSprite(new Sprite
                                {
                                    Texture = textures.Get("images/logo/LogoWithText.png"),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                })
                                {
                                    Scale = new Vector2(1.2f),
                                    Y = -35f,
                                    MaxScalePercentage = 1.12f,
                                    OnClickAction = toggleMenuMode
                                },

                                songTitleText = new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Y = -15, X = 24,
                                    Font = new FontUsage(size: 12)
                                },

                                menuPlayer = new MenuPlayer
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    OnNext = () => beatpackManager.Next(),
                                    OnPrevious = () => beatpackManager.Previous(),
                                    OnTogglePlay = () => togglePause(),
                                    X = -12, Y= 24.5f, 
                                    Scale= new Vector2(1.1f)
                                },
                            }
                        },

                        frameworkCredit = new Sprite
                        {
                            Texture = textures.Get("images/Osu!Framework.png"),
                            Scale = new Vector2(1.2f),
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.Centre,
                            Y = -60, X = 30
                        },
                    }
                },

            };//

            beatpackManager.CurrentBeatpack.BindValueChanged(beatpackChanged, true);

            ChangeInternalChildDepth(header, float.MinValue);
            ChangeInternalChildDepth(footer, float.MinValue);
        }

        private void beatpackChanged(ValueChangedEvent<Beatpack> e)
        {
            var newBeatpack = e.NewValue;
            if (newBeatpack?.Beatmap == null || string.IsNullOrEmpty(newBeatpack.FilePath))
            {
                track?.Stop();
                // Don't expire - just replace the child to avoid parent conflicts
                background = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };
                backgroundContainer.Child = background;

                songTitleText.Text = string.Empty;
                return;
            }

            var title = newBeatpack.Beatmap?.Title;
            var artist = newBeatpack.Beatmap?.Artist;
            songTitleText.Text = string.Join(" - ", new[] { artist, title }.Where(s => !string.IsNullOrEmpty(s)));

            var fullPath = newBeatpack.FilePath;
            using (var stream = File.OpenRead(fullPath))
            using (var beatmapAssetStorage = new ZipArchiveResourceStore(stream))
            {
                // Don't expire - we'll replace via Child assignment to avoid parent conflicts
                background = null;

                // Try to load video (support both new and old formats)
                string videoPath = newBeatpack.GetVideoPathForBeatmap(newBeatpack.Beatmap);
                
                if (!string.IsNullOrEmpty(videoPath))
                {
                    Logger.Log($"[MainScreen] Attempting to load video: {videoPath}");
                    
                    if (beatmapAssetStorage.Exists(videoPath))
                    {
                        var videoStream = beatmapAssetStorage.GetStream(videoPath);
                        if (videoStream != null)
                        {
                            Logger.Log($"[MainScreen] Video loaded successfully: {videoPath}");
                            background = new Video(videoStream)
                            {
                                RelativeSizeAxes = Axes.Both,
                                FillMode = FillMode.Fill,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Loop = true,
                            };
                        }
                    }
                    else
                    {
                        Logger.Log($"[MainScreen] Video not found in beatpack: {videoPath}");
                    }
                }
                
                if (background == null)
                {
                    // Try to load background image (old format or new format)
                    var textureStore = new TextureStore(host.Renderer, new TextureLoaderStore(beatmapAssetStorage));
                    Texture backgroundTexture = null;
                    
                    // Try BackgroundImagePath first (old format)
                    if (!string.IsNullOrEmpty(newBeatpack.BackgroundImagePath) && beatmapAssetStorage.Exists(newBeatpack.BackgroundImagePath))
                    {
                        backgroundTexture = textureStore.Get(newBeatpack.BackgroundImagePath);
                    }
                    
                    // Fallback to cover.jpg (new format)
                    if (backgroundTexture == null && beatmapAssetStorage.Exists("cover.jpg"))
                    {
                        backgroundTexture = textureStore.Get("cover.jpg");
                    }
                    
                    if (backgroundTexture != null)
                    {
                        background = new Sprite
                        {   
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill, 
                            Texture = backgroundTexture
                        };
                    }
                    else
                    {
                        background = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };
                    }
                }

                backgroundContainer.Child = background;

                track?.Stop();
                var trackStore = audioManager.GetTrackStore(beatmapAssetStorage);
                
                // Try MusicPath first (old format or manifest-specified path)
                if (!string.IsNullOrEmpty(newBeatpack.MusicPath))
                {
                    track = trackStore.Get(newBeatpack.MusicPath);
                }
                
                // Fallback to audio.mp3 (new format default)
                if (track == null)
                {
                    track = trackStore.Get("audio.mp3");
                }
                
                if (track != null)
                {
                    track.Looping = true;
                    mainLogo.SetTrack(track);
                    
                    // Only auto-start if we should (i.e., if MainScreen is active)
                    if (shouldAutoPlayTrack)
                    {
                        track.Start();
                    }
                }

            }
        }

        private void togglePause()
        {
            if (track == null) return;

            if (track.IsRunning)
                track.Stop();
            else
                track.Start();

            if (background is Video)
            {
                // As a reminder, direct video pause/play isn't simple.
                // It's tied to the game clock. This can be implemented later.
            }
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            
            // Start with header and footer hidden unless in menu mode
            if (!isInMenuMode)
            {
                header.FadeOut(0);
                footer.FadeOut(0);
            }
            
            track?.Start();
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            
            if (e.Last is EditDashboardScreen)
            {
                // Reset to initial main screen state when returning from editor
                isInMenuMode = false;
                shouldAutoPlayTrack = true;
                
                mainLogo.FadeIn(300);
                songTitleText.FadeIn(300);
                menuPlayer.FadeIn(300);
                frameworkCredit.FadeIn(300);
                
                header.FadeOut(0);
                footer.FadeOut(0);
                header.MoveToY(header_peek_y, 0);
                footer.MoveToY(footer_peek_y, 0);
                
                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                menuButtonsContainer?.FadeOut(0);
                
                var centerContainer = mainLogo.Parent?.Parent as Container;
                centerContainer?.MoveToX(0, 0);
                
                // Restart the music
                track?.Start();
            }
            else
            {
                // Re-enable auto-play when returning to MainScreen
                shouldAutoPlayTrack = true;
                
                Logger.Log($"[MainScreen] OnResuming called", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log($"[MainScreen] backgroundContainer.Parent = {backgroundContainer?.Parent?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log($"[MainScreen] backgroundContainer.Child = {backgroundContainer?.Child?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log($"[MainScreen] isInMenuMode = {isInMenuMode}", LoggingTarget.Runtime, LogLevel.Important);
                
                
                mainLogo.FadeIn(300);
                songTitleText.FadeIn(300);
                menuPlayer.FadeIn(300);
                
                if (isInMenuMode)
                {
                    header.FadeIn(200);
                    footer.FadeIn(200);
                    header.MoveToY(0, 0);
                    footer.MoveToY(0, 0);
                }
                else
                {
                    header.FadeOut(0);
                    footer.FadeOut(0);
                    header.MoveToY(header_peek_y, 0);
                    footer.MoveToY(footer_peek_y, 0);
                }
                
                track?.Start();
                
                if (backgroundContainer?.Child is Video video)
                {
                    Logger.Log($"[MainScreen] Found video in OnResuming. Loop = {video.Loop}, IsAlive = {video.IsAlive}", LoggingTarget.Runtime, LogLevel.Important);

                    video.Loop = true;
                }
                else
                {
                    Logger.Log($"[MainScreen] No video found in OnResuming!", LoggingTarget.Runtime, LogLevel.Important);
                }
                
                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                if (menuButtonsContainer != null && isInMenuMode)
                {
                    menuButtonsContainer.FadeIn(300);
                }            
                if (!isInMenuMode)
                    toggleMenuMode();
            }
            
            // Re-enable auto-play when returning to MainScreen
            shouldAutoPlayTrack = true;
            
            track?.Start();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            track?.Stop();
            return base.OnExiting(e);
        }

        public void StopCurrentTrack()
        {
            track?.Stop();
            Logger.Log("[MainScreen] Track stopped via StopCurrentTrack()", LoggingTarget.Runtime, LogLevel.Important);
        }

        public Track GetCurrentTrack()
        {
            return track;
        }

        public void AddBackgroundContainer(Container container)
        {
            Logger.Log($"[MainScreen] AddBackgroundContainer called. Container.Parent = {container.Parent?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[MainScreen] Container.Child = {container.Child?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Only add if not already our child
            if (container.Parent != this)
                AddInternal(container);
            
            ChangeInternalChildDepth(container, float.MaxValue);
            
            Logger.Log($"[MainScreen] After adding: Container.Parent = {container.Parent?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[MainScreen] backgroundContainer == container: {backgroundContainer == container}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[MainScreen] backgroundContainer.Depth = {container.Depth}", LoggingTarget.Runtime, LogLevel.Important);
            
            if (container.Child is Video video)
            {
                Logger.Log($"[MainScreen] Video found in container. Loop = {video.Loop}", LoggingTarget.Runtime, LogLevel.Important);
                video.Loop = true;
            }
            else
            {
                Logger.Log($"[MainScreen] No video in container. Child type: {container.Child?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        public void EnterMenuMode()
        {
            mainLogo.FadeIn(300);
            songTitleText.FadeIn(300);
            menuPlayer.FadeIn(300);
            frameworkCredit.FadeIn(300);
            
            // Always ensure we're in menu mode when resuming
            if (!isInMenuMode)
                toggleMenuMode();
        }

        private void toggleMenuMode()
        {
            if (isMenuTransitioning)
                return;

            isInMenuMode = !isInMenuMode;

            var hoverContainer = mainLogo.Parent as HoverContainer;
            
            if (isInMenuMode)
                hoverContainer?.FadeColour(Colour4.White, animation_duration).Then().OnComplete(d => d.Alpha = 1f);
            else
                hoverContainer?.FadeColour(Colour4.White, animation_duration).Then().OnComplete(d => d.Alpha = 1f);

            initialPositions ??= new Dictionary<Drawable, float>
            {
                { mainLogo, mainLogo.X },
                { songTitleText, songTitleText.X },
                { menuPlayer, menuPlayer.X }
            };

            var menuButtons = InternalChildren
                .OfType<Container>()
                .FirstOrDefault(c => c.Name == "Menu Buttons");

            if (mainLogo.Parent?.Parent is Container centerContainer)
            {
                centerContainer.ClearTransforms(true);
                centerContainer.MoveToX(isInMenuMode ? menu_offset_x : 0, animation_duration, Easing.OutExpo);
            }

            if (isInMenuMode)
            {
                frameworkCredit.FadeOut(animation_duration / 2);
                header.FadeIn(animation_duration / 2);
                footer.FadeIn(animation_duration / 2);
                header.MoveToY(0, animation_duration, Easing.OutQuint);
                footer.MoveToY(0, animation_duration, Easing.OutQuint);
            }
            else
            {
                frameworkCredit.FadeIn(animation_duration / 3); // Faster fade-in
                header.FadeOut(animation_duration / 4); // Snappier fade-out
                footer.FadeOut(animation_duration / 4); // Snappier fade-out
                header.MoveToY(header_peek_y, animation_duration, Easing.InQuint);
                footer.MoveToY(footer_peek_y, animation_duration, Easing.InQuint);
            }

            if (menuButtons != null)
            {
                menuButtons.ClearTransforms();
                if (isInMenuMode)
                    menuButtons.FadeTo(1, 0);

                const float button_entry_offset = 600f; 
                const double button_stagger = 50;    
                const double button_anim_duration = 500; 
                const double button_exit_duration = 250; 
                int index = 0;
                foreach (var drawable in menuButtons.Children)
                {
                    drawable.ClearTransforms();

                    if (isInMenuMode)
                    {
                        drawable.X = -button_entry_offset;
                        drawable.FadeTo(0, 0);
                        drawable.Delay(button_stagger * index)
                                .FadeIn(button_anim_duration)
                                .MoveToX(0, button_anim_duration, Easing.OutBack);
                    }
                    else
                    {
                        drawable.Delay(button_stagger * index)
                                .FadeOut(button_exit_duration, Easing.OutQuint); // Added easing for snappy feel
                    }

                    index++;
                }

                if (!isInMenuMode)
                {
                    var total = button_exit_duration + button_stagger * (menuButtons.Children.Count - 1);
                    menuButtons.Delay(total).FadeTo(0, 0); 
                }

                var maxButtonsDuration = isInMenuMode
                    ? button_anim_duration + button_stagger * (menuButtons.Children.Count - 1)
                    : button_exit_duration + button_stagger * (menuButtons.Children.Count - 1);
                var totalDuration = Math.Max(animation_duration, maxButtonsDuration);
                isMenuTransitioning = true;
                this.Delay(totalDuration).Schedule(() => isMenuTransitioning = false);
            }
        }
    }
}