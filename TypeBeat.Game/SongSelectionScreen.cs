using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Ui;
using System.Collections.Generic;
using osu.Framework.Logging;
namespace TypeBeat.Game
{
    public partial class SongSelectionScreen : Screen
    {
        private readonly BeatpackManager beatpackManager;
        private readonly Container backgroundContainer;
        private readonly MainScreen mainScreen;
        private readonly Track track;
        private readonly Header header;
        private readonly Footer footer;
        private readonly Container backgroundLayer;
        private Box darkOverlay;
        private BeatpackPreview beatpackPreview;
        private SongThumbnail selectedThumbnail;
        private DifficultyButton selectedDifficultyButton;

        public SongSelectionScreen(BeatpackManager beatpackManager, Container backgroundContainer, MainScreen mainScreen, Track track)
        {
            this.beatpackManager = beatpackManager;
            this.backgroundContainer = backgroundContainer;
            this.mainScreen = mainScreen;
            this.track = track;

            InternalChildren = new Drawable[]
            {
                backgroundLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Name = "Background Layer"
                },

                header = new Header { Alpha = 0 },
                footer = new Footer { Alpha = 0 },

                beatpackPreview = new BeatpackPreview
                {
                    Name = "Beatpack Preview",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.8f,
                    Width = 0.62f, // Reduced to give space for right container
                },
                // SONG LIST CONTAINER
                new Container
                {
                    Name = "Song List Container",
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.8f,
                    Width = 0.08f, // Thinner (12%)
                    X = 60, // More gap from left edge
                    Masking = true,
                    CornerRadius = 20,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.Black,
                            Alpha = 0.5f
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Left = 5, Vertical = 30 },
                            Child = new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                ScrollbarVisible = false,
                                ClampExtension = 20,
                                Child = new FillFlowContainer
                                {
                                    Name = "Song Thumbnails",
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Spacing = new Vector2(0, 10),
                                    Padding = new MarginPadding { Horizontal = 2, Vertical = 2 },
                                    Direction = FillDirection.Vertical
                                }
                            }
                        }
                    }
                },

                //DIFFICULTY CONTAINER
                new Container
                {
                    Name = "Info Container",
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.8f,
                    Width = 0.1f,
                    X = -60,
                    Children = new Drawable[]
                    {
                        // Top section: Difficulty List (7/8 of height)
                        new Container
                        {
                            Name = "Difficulty Section",
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.Both,
                            Height = 7f / 8f, // 7/8 of the height
                            Masking = true,
                            CornerRadius = 20,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.Black,
                                    Alpha = 0.5f
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 10, Vertical = 10 },
                                    Child = new BasicScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        ScrollbarVisible = false,
                                        ClampExtension = 20,
                                        Child = new FillFlowContainer
                                        {
                                            Name = "Difficulty List",
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Spacing = new Vector2(0, 10),
                                            Direction = FillDirection.Vertical
                                        }
                                    }
                                }
                            }
                        },
                        // Bottom section: Play Button (1/8 of height)
                        new Container
                        {
                            Name = "Play Button Section",
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.Both,
                            Height = 1f / 8f, // 1/8 of the height
                            Masking = true,
                            CornerRadius = 20,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.Black,
                                    Alpha = 0.5f
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(10),
                                    Child = new ClickableContainer
                                    {
                                        Name = "Play Button Container",
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        CornerRadius = 15,
                                        Action = () => startGame(),
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Colour4.FromHex("4CAF50"), // Green color
                                            },
                                            new SpriteText
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Text = "PLAY",
                                                Font = new FontUsage("Kodchasan", size: 32, weight: "Bold"),
                                                Colour = Colour4.White
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            addBackground();

            // Show the currently playing beatpack's background and difficulty
            if (beatpackManager.CurrentBeatpack.Value != null)
            {
                beatpackPreview.ShowBeatpack(beatpackManager.CurrentBeatpack.Value);
                updateDifficultyList(beatpackManager.CurrentBeatpack.Value);
            }

            this.FadeInFromZero(150);

            header.Alpha = 0;
            footer.Alpha = 0;
            var songList = InternalChildren.FirstOrDefault(c => c.Name == "Song List Container");
            var infoContainer = InternalChildren.FirstOrDefault(c => c.Name == "Info Container");
            if (songList != null) songList.Alpha = 0;
            if (infoContainer != null) infoContainer.Alpha = 0;

            using (BeginDelayedSequence(100))
            {
                header.FadeIn(350, Easing.OutQuint);
                footer.FadeIn(350, Easing.OutQuint);
                songList?.FadeIn(350, Easing.OutQuint);
                infoContainer?.FadeIn(350, Easing.OutQuint);
            }

            dumpChildrenOrder("OnEntering after addBackground");
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            
            Logger.Log("[SongSelection] Resuming from GameScreen", LoggingTarget.Runtime, LogLevel.Important);
            
            // Get the current track from MainScreen (in case beatpack changed during gameplay)
            var currentTrack = mainScreen.GetCurrentTrack();
            
            // If track is not playing (because it was stopped when going to LoadingScreen),
            // restart it from the beginning
            if (currentTrack != null && !currentTrack.IsRunning)
            {
                currentTrack.Restart(); // This stops and starts from position 0
                Logger.Log($"[SongSelection] Current track was not running, restarted from beginning", LoggingTarget.Runtime, LogLevel.Important);
            }
            else if (currentTrack != null)
            {
                Logger.Log($"[SongSelection] Current track is already running, continuing playback", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            // Restart video if present
            if (backgroundContainer?.Child is Video video)
            {
                video.Loop = true; // Ensure it loops
                Logger.Log("[SongSelection] Video found and set to loop", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            Logger.Log($"[SongSelection] OnExiting called", LoggingTarget.Runtime, LogLevel.Important);
            

            removeBackground();
            
            Logger.Log($"[SongSelection] Returning background to MainScreen", LoggingTarget.Runtime, LogLevel.Important);
            mainScreen.AddBackgroundContainer(backgroundContainer);
            
            header.FadeOut(250, Easing.OutQuint);
            footer.FadeOut(250, Easing.OutQuint);
            this.FadeOut(300, Easing.OutQuint);

            return base.OnExiting(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                this.Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }

        private void addBackground()
        {
            if (backgroundContainer.Parent != null && backgroundContainer.Parent != backgroundLayer)
            {
                Logger.Log("Background container still has a parent. Skipping add to avoid multi-parent exception.", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            if (backgroundContainer.Parent != backgroundLayer)
                backgroundLayer.Add(backgroundContainer);
            
            // Add darkening overlay on top
            if (darkOverlay == null)
            {
                darkOverlay = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = osuTK.Graphics.Color4.Black,
                    Alpha = 0.8f, // Moderate darkening
                    Depth = -1, // Render above background
                };
                backgroundLayer.Add(darkOverlay);
            }
        }

        private void removeBackground()
        {
            // Remove dark overlay
            if (darkOverlay != null && darkOverlay.Parent == backgroundLayer)
            {
                backgroundLayer.Remove(darkOverlay, false);
                darkOverlay = null;
            }

            // Remove the original background from backgroundLayer 
            // (MainScreen will re-add it safely)
            if (backgroundContainer.Parent == backgroundLayer)
                backgroundLayer.Remove(backgroundContainer, false);
        }

        private void dumpChildrenOrder(string where)
        {
            Logger.Log($"[SongSelection] {where}");
            int i = 0;
            foreach (var d in InternalChildren)
                Logger.Log($"  {i++}: {d.Name ?? d.GetType().Name} depth={d.Depth} alpha={d.Alpha}");
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            // Set the track for beat reaction
            if (track != null)
            {
                beatpackPreview.SetTrack(track);
            }

            var songListContainer = InternalChildren.OfType<Container>()
                                    .FirstOrDefault(x => x.Name == "Song List Container");

            var scrollContainer = songListContainer?.Children.OfType<Container>().FirstOrDefault()
                                    ?.Children.OfType<BasicScrollContainer>()
                                    .FirstOrDefault();

            if (scrollContainer == null)
                return;

            var songContainer = (FillFlowContainer)scrollContainer.Child;

            foreach (var beatpack in beatpackManager.Beatpacks)
            {
                var thumbnail = new SongThumbnail(beatpack, textures)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre
                };

                thumbnail.OnSelected += (selected) =>
                {
                    // Deselect previous thumbnail
                    if (selectedThumbnail != null)
                        selectedThumbnail.IsSelected = false;

                    // Select new thumbnail
                    selectedThumbnail = thumbnail;
                    selectedThumbnail.IsSelected = true;

                    handleSongSelected(selected);
                };

                songContainer.Add(thumbnail);

                // Auto-select the currently playing beatpack
                if (beatpackManager.CurrentBeatpack.Value == beatpack)
                {
                    selectedThumbnail = thumbnail;
                    selectedThumbnail.IsSelected = true;
                }
            }
        }
        
        private void handleSongSelected(Beatpack selectedBeatpack)
        {
            // Stop the current track (from MainScreen) when switching beatpacks
            mainScreen.StopCurrentTrack();
            
            beatpackManager.CurrentBeatpack.Value = selectedBeatpack;
            
            // Get the newly loaded track from MainScreen and start it
            Schedule(() =>
            {
                var newTrack = mainScreen.GetCurrentTrack();
                if (newTrack != null && !newTrack.IsRunning)
                {
                    newTrack.Start();
                    Logger.Log($"[SongSelection] Started new track for: {selectedBeatpack.Beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
                }
            });
            
            if (selectedBeatpack?.Beatmap != null)
            {
                Schedule(() =>
                {
                    beatpackPreview.ShowBeatpack(selectedBeatpack);
                    updateDifficultyList(selectedBeatpack);
                });
            }
            
            Logger.Log($"Selected beatpack: {selectedBeatpack.Beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
        }

        private void updateDifficultyList(Beatpack beatpack)
        {
            Logger.Log($"[updateDifficultyList] Starting update for beatpack: {beatpack?.Beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
            
            var infoContainer = InternalChildren.OfType<Container>()
                                .FirstOrDefault(x => x.Name == "Info Container");
            
            Logger.Log($"[updateDifficultyList] Info Container found: {infoContainer != null}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Navigate to the Difficulty Section, then to its scroll container
            var difficultySection = infoContainer?.Children.OfType<Container>()
                                .FirstOrDefault(x => x.Name == "Difficulty Section");
            
            Logger.Log($"[updateDifficultyList] Difficulty Section found: {difficultySection != null}", LoggingTarget.Runtime, LogLevel.Important);
            
            if (difficultySection != null)
            {
                Logger.Log($"[updateDifficultyList] Difficulty Section has {difficultySection.Children.Count()} children", LoggingTarget.Runtime, LogLevel.Important);
                foreach (var child in difficultySection.Children)
                {
                    Logger.Log($"[updateDifficultyList]   Child: {child.GetType().Name}, Name={child.Name}", LoggingTarget.Runtime, LogLevel.Important);
                }
            }
            
            var paddingContainer = difficultySection?.Children.OfType<Container>().FirstOrDefault();
            Logger.Log($"[updateDifficultyList] Padding Container found: {paddingContainer != null}", LoggingTarget.Runtime, LogLevel.Important);
            
            var scrollContainer = paddingContainer?.Children.OfType<BasicScrollContainer>().FirstOrDefault();
            Logger.Log($"[updateDifficultyList] Scroll Container found: {scrollContainer != null}", LoggingTarget.Runtime, LogLevel.Important);
            
            if (scrollContainer == null)
            {
                Logger.Log("Could not find scroll container - difficulty list cannot be updated", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            var difficultyList = (FillFlowContainer)scrollContainer.Child;
            Logger.Log($"[updateDifficultyList] Difficulty List found, clearing {difficultyList.Children.Count()} existing items", LoggingTarget.Runtime, LogLevel.Important);
            
            difficultyList.Clear();

            if (selectedDifficultyButton != null)
                selectedDifficultyButton.IsSelected = false;

            if (beatpack?.Beatmap != null)
            {
                Logger.Log($"[updateDifficultyList] Creating difficulty button for: {beatpack.Beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
                
                var diffButton = new DifficultyButton(beatpack.Beatmap);
                diffButton.OnSelected += (beatmap) =>
                {
                    if (selectedDifficultyButton != null)
                        selectedDifficultyButton.IsSelected = false;
                    
                    selectedDifficultyButton = diffButton;
                    selectedDifficultyButton.IsSelected = true;
                    
                    handleDifficultySelected(beatmap);
                };
                
                difficultyList.Add(diffButton);
                
                selectedDifficultyButton = diffButton;
                selectedDifficultyButton.IsSelected = true;
                
                Logger.Log($"[updateDifficultyList] ✓ Successfully added difficulty button. List now has {difficultyList.Children.Count()} items", LoggingTarget.Runtime, LogLevel.Important);
            }
            else
            {
                Logger.Log("[updateDifficultyList] Beatpack or Beatmap is null!", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private void handleDifficultySelected(Beatmap selectedBeatmap)
        {
            Logger.Log($"Selected difficulty: {selectedBeatmap.DifficultyName} (★{selectedBeatmap.StarRating})", LoggingTarget.Runtime, LogLevel.Important);
        }

        private void startGame()
        {
            if (beatpackManager.CurrentBeatpack.Value == null || beatpackManager.CurrentBeatpack.Value.Beatmap == null)
            {
                Logger.Log("Cannot start game: No beatpack or beatmap selected", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            Logger.Log($"Starting game with beatpack: {beatpackManager.CurrentBeatpack.Value.Beatmap.Title}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Stop ALL tracks - both the old track reference and MainScreen's current track
            if (track != null && track.IsRunning)
            {
                track.Stop();
            }
            mainScreen.StopCurrentTrack();
            
            // Transition to loading screen
            var loadingScreen = new LoadingScreen(beatpackManager.CurrentBeatpack.Value, beatpackManager.CurrentBeatpack.Value.Beatmap);
            this.Push(loadingScreen);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && backgroundContainer.Parent != null)
            {
                Schedule(() =>
                {
                    removeBackground();
                    mainScreen.AddBackgroundContainer(backgroundContainer);
                });
            }
            
            base.Dispose(isDisposing);
        }
    }
}
