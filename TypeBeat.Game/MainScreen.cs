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
using TypeBeat.Game.fileHandling;
using TypeBeat.Game.ui;

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
        
        private bool isInMenuMode;
    private bool isMenuTransitioning;
        private Dictionary<Drawable, float> initialPositions;
        private const float menu_offset_x = 350f;  
        private const float animation_duration = 400;
        private const float header_peek_y = -8f;
        private const float footer_peek_y = 8f;

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio, TextureStore textures)
        {
            this.host = host;
            audioManager = audio;
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
                footer = new Footer
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = footer_peek_y,
                    Alpha = 0,
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
                            Child = new MenuButton("Play", Colour4.Orange, 15f, dimensions: new Vector2(200, 50), onClick: () => 
                            {
                                mainLogo.FadeOut(400);
                                songTitleText.FadeOut(400);
                                menuPlayer.FadeOut(400);
                                frameworkCredit.FadeOut(400);

                                var menuButtonsContainer = InternalChildren.OfType<Container>().FirstOrDefault(c => c.Name == "Menu Buttons");
                                menuButtonsContainer?.FadeOut(400);

                                this.Delay(400).Schedule(() =>
                                {
                                    RemoveInternal(backgroundContainer, false);
                                    var songSelection = new SongSelectionScreen(beatpackManager, backgroundContainer, this);
                                    this.Push(songSelection);
                                });
                            })
                        },
                        new Container
                        {
                            Y = 60,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Create", Colour4.YellowGreen, 15f, dimensions: new Vector2(200, 50))
                        },
                        new Container
                        {
                            Y = 120,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Explore", Colour4.DeepSkyBlue, 15f, dimensions: new Vector2(200, 50))
                        },
                        new Container
                        {
                            Y = 180,
                            AutoSizeAxes = Axes.Both,
                            Child = new MenuButton("Options", Colour4.HotPink, 15f, dimensions: new Vector2(200, 50))
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
                background?.Expire();
                backgroundContainer.Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };

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
                background?.Expire();

                if (!string.IsNullOrEmpty(newBeatpack.VideoPath) && beatmapAssetStorage.Exists(newBeatpack.VideoPath))
                {
                    background = new Video(beatmapAssetStorage.GetStream(newBeatpack.VideoPath))
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Loop = true, //
                    };
                }
                else if (!string.IsNullOrEmpty(newBeatpack.BackgroundImagePath) && beatmapAssetStorage.Exists(newBeatpack.BackgroundImagePath))
                {
                    var textureStore = new TextureStore(host.Renderer, new TextureLoaderStore(beatmapAssetStorage));
                    background = new Sprite
                    {   
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        FillMode = FillMode.Fill, 
                        Texture = textureStore.Get(newBeatpack.BackgroundImagePath)
                    };
                }
                else
                {
                    background = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };
                }

                backgroundContainer.Child = background;

                track?.Stop();
                var trackStore = audioManager.GetTrackStore(beatmapAssetStorage);
                if (!string.IsNullOrEmpty(newBeatpack.MusicPath))
                {
                    track = trackStore.Get(newBeatpack.MusicPath);
                    if (track != null)
                    {
                        track.Looping = true;
                        mainLogo.SetTrack(track);
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

        public override bool OnExiting(ScreenExitEvent e)
        {
            track?.Stop();
            return base.OnExiting(e);
        }

        public void AddBackgroundContainer(Container container)
        {
            Logger.Log($"[MainScreen] AddBackgroundContainer called. Container.Parent = {container.Parent?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[MainScreen] Container.Child = {container.Child?.GetType().Name ?? "null"}", LoggingTarget.Runtime, LogLevel.Important);
            
            if (container.Parent != null)
                RemoveInternal(container, false);
            
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
                frameworkCredit.FadeIn(animation_duration / 2);
                header.FadeOut(animation_duration / 2);
                footer.FadeOut(animation_duration / 2);
                header.MoveToY(header_peek_y, animation_duration, Easing.InQuint);
                footer.MoveToY(footer_peek_y, animation_duration, Easing.InQuint);
            }

            if (menuButtons != null)
            {
                menuButtons.ClearTransforms(true);
                if (isInMenuMode)
                    menuButtons.FadeTo(1, 0);

                const float button_entry_offset = 600f; 
                const double button_stagger = 100;     
                const double button_anim_duration = 500; 
                const double button_exit_duration = 300; 

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
                                .FadeOut(button_exit_duration);
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