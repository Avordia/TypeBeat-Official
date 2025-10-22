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
        private readonly BeatpackManager beatpackManager; //
        private readonly Container backgroundContainer;
        private readonly MainScreen mainScreen;
        private readonly Track track;
        private readonly Container backgroundLayer;
        private Box darkOverlay;
        private BeatpackPreview beatpackPreview;
        private SongThumbnail selectedThumbnail;
        private DifficultyButton selectedDifficultyButton;
    private LogoClickableContainer logoContainer;
        private Container infoContainer;
        private BasicScrollContainer songThumbnailList;

    private string currentGamemode = "TypeBeat"; // Default gamemode
    private TextureStore textures;

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

                // TOP-LEFT: TypeBeat Logo with Text (Clickable, Hoverable)
                logoContainer = new LogoClickableContainer
                {
                    Name = "Logo Container",
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Size = new Vector2(350, 105),
                    Margin = new MarginPadding(30),
                    Depth = -1000,
                    X = 60,
                    Action = () =>
                    {
                        // Toggle gamemode
                        if (currentGamemode == "TypeBeat")
                            currentGamemode = "TypeNote";
                        else
                            currentGamemode = "TypeBeat";

                        updateLogoTexture();

                        // Only update the difficulty container
                        if (beatpackManager.CurrentBeatpack.Value != null)
                            updateDifficultyList(beatpackManager.CurrentBeatpack.Value);
                    },
                    Child = new Sprite
                    {
                        Name = "Logo Sprite",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                        Texture = null
                    }
                },

                infoContainer = new Container
                {
                    Name = "Info Container",
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.45f, 
                    Height = 0.07f, 
                    Margin = new MarginPadding(30),
                    Depth = -1000,
                    X = -51.5f, Y = 25,
                    Masking = true, // Enable masking for rounded corners
                    CornerRadius = 25, // Rounded corners
                    
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
                            Padding = new MarginPadding { Horizontal = 8, Vertical = 2 },
                            Child = new BasicScrollContainer(Direction.Horizontal)
                            {
                                RelativeSizeAxes = Axes.Both,
                                ScrollbarVisible = false,
                                ClampExtension = 20,
                                Child = new FillFlowContainer
                                {
                                    Name = "Difficulty List",
                                    RelativeSizeAxes = Axes.Y, // Fill height
                                    AutoSizeAxes = Axes.X, // Expand width based on content
                                    Spacing = new Vector2(8, 0),
                                    Direction = FillDirection.Horizontal
                                }
                            }
                        }
                    }
                },

                beatpackPreview = new BeatpackPreview
                {
                    Name = "Beatpack Preview",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.6f,
                    Width = 0.9f,
                    Y = -20,
                    Depth = 0,
                },
                
                new Container
                {
                    Name = "Song List Container",
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.X,
                    Height = 120,
                    Width = 0.90f,
                    Y = -20,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 20, Vertical = 10 },
                        Child = songThumbnailList = new BasicScrollContainer(Direction.Horizontal)
                        {
                            RelativeSizeAxes = Axes.Both,
                            ScrollbarVisible = false,
                            ClampExtension = 20,
                            Child = new FillFlowContainer
                            {
                                Name = "Song Thumbnails",
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Spacing = new Vector2(15, 0),
                                Padding = new MarginPadding { Horizontal = 2 },
                                Direction = FillDirection.Horizontal
                            }
                        }
                    }
                }
            };
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            Logger.Log("==============================================", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log("=== ENTERING SONG SELECTION SCREEN ===", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log("==============================================", LoggingTarget.Runtime, LogLevel.Important);

            base.OnEntering(e);

            addBackground();

            // Show the currently playing beatpack's background and difficulty
            if (beatpackManager.CurrentBeatpack.Value != null)
            {
                Logger.Log($"[SongSelection] Current beatpack: {beatpackManager.CurrentBeatpack.Value.Beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
                beatpackPreview.ShowBeatpack(beatpackManager.CurrentBeatpack.Value);
                updateDifficultyList(beatpackManager.CurrentBeatpack.Value);
            }
            else
            {
                Logger.Log("[SongSelection] No current beatpack!", LoggingTarget.Runtime, LogLevel.Important);
            }

            this.FadeInFromZero(150);

            Logger.Log($"[SongSelection] Logo - Pos: {logoContainer.Position}, Size: {logoContainer.Size}, Alpha: {logoContainer.Alpha}, Depth: {logoContainer.Depth}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[SongSelection] Info - Pos: {infoContainer.Position}, Size: {infoContainer.Size}, Alpha: {infoContainer.Alpha}, Depth: {infoContainer.Depth}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Start these visible or fade them in
            logoContainer.FadeIn(0); // Immediately visible
            infoContainer.FadeIn(0); // Immediately visible
            songThumbnailList.Parent.Alpha = 0; // Parent is the Song List Container

            using (BeginDelayedSequence(100))
            {
                songThumbnailList.Parent.FadeIn(350, Easing.OutQuint);
            }

            dumpChildrenOrder("OnEntering after addBackground");
            Logger.Log("=== SONG SELECTION ENTRY COMPLETE ===", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            
            Logger.Log("[SongSelection] Resuming from GameScreen", LoggingTarget.Runtime, LogLevel.Important);
            var currentTrack = mainScreen.GetCurrentTrack();
            if (currentTrack != null && !currentTrack.IsRunning)
            {
                currentTrack.Restart(); 
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
            this.textures = textures;
            updateLogoTexture();

            // Wire up the play button in BeatpackPreview
            beatpackPreview.SetPlayButtonAction(() => startGame());

            // Get the song container (FillFlowContainer inside the scroll container)
            var songContainer = (FillFlowContainer)songThumbnailList.Child;

            foreach (var beatpack in beatpackManager.Beatpacks)
            {
                var thumbnail = new SongThumbnail(beatpack, textures)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft
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

        private void updateLogoTexture()
        {
            if (logoContainer?.Child is Sprite logoSprite && textures != null)
            {
                string texPath = currentGamemode == "TypeBeat" ? "images/logo/LogoWithText" : "images/logo/TypeNoteLogo";
                var texture = textures.Get(texPath);
                logoSprite.Texture = texture;
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
            Logger.Log($"[updateDifficultyList] Starting update - showing beatmaps for selected beatpack: {beatpack?.Beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);

            // Navigate through the structure: Info Container -> Box (skipped) -> Container (padding) -> BasicScrollContainer
            var paddingContainer = infoContainer.Children.OfType<Container>().FirstOrDefault();
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

            if (beatpack == null)
            {
                Logger.Log("[updateDifficultyList] Beatpack is null!", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            // Get all beatmaps from the selected beatpack
            var beatmapsToShow = new List<Beatmap>();

            // Add beatmaps from the Beatmaps list (if any)
            if (beatpack.Beatmaps != null && beatpack.Beatmaps.Any())
            {
                beatmapsToShow.AddRange(beatpack.Beatmaps);
            }
            // Fallback to single Beatmap for backward compatibility
            else if (beatpack.Beatmap != null)
            {
                beatmapsToShow.Add(beatpack.Beatmap);
            }

            if (!beatmapsToShow.Any())
            {
                Logger.Log("[updateDifficultyList] No beatmaps found in this beatpack!", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            Logger.Log($"[updateDifficultyList] Found {beatmapsToShow.Count} beatmap(s) in selected beatpack", LoggingTarget.Runtime, LogLevel.Important);

            // Filter beatmaps by current gamemode
            string filterGamemode = currentGamemode;
            var filtered = beatmapsToShow.Where(b =>
                (string.IsNullOrEmpty(b.Gamemode) && filterGamemode == "TypeBeat") ||
                (b.Gamemode?.Equals(filterGamemode, System.StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            if (!filtered.Any())
            {
                // Show message if no beatmaps for this gamemode
                difficultyList.Add(new SpriteText
                {
                    Text = $"No {filterGamemode} Beatmap found for this beatpack.",
                    Colour = Colour4.Red,
                    Font = FontUsage.Default.With(size: 28, weight: "Bold"),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding { Left = 10 }
                });
                Logger.Log($"[updateDifficultyList] No {filterGamemode} beatmaps found.", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            // Create difficulty buttons for each filtered beatmap
            foreach (var beatmap in filtered)
            {
                Logger.Log($"[updateDifficultyList] Creating difficulty button for: {beatmap.DifficultyName} (★{beatmap.StarRating})", LoggingTarget.Runtime, LogLevel.Important);

                var diffButton = new DifficultyButton(beatmap);
                diffButton.OnSelected += (selectedBeatmap) =>
                {
                    if (selectedDifficultyButton != null)
                        selectedDifficultyButton.IsSelected = false;

                    selectedDifficultyButton = diffButton;
                    selectedDifficultyButton.IsSelected = true;

                    handleDifficultySelected(selectedBeatmap);
                };

                difficultyList.Add(diffButton);

                // Auto-select the first beatmap or the current one
                if (selectedDifficultyButton == null || beatmap == beatpack.Beatmap)
                {
                    selectedDifficultyButton = diffButton;
                    selectedDifficultyButton.IsSelected = true;
                }
            }

            Logger.Log($"[updateDifficultyList] ✓ Successfully added {difficultyList.Children.Count()} difficulty buttons", LoggingTarget.Runtime, LogLevel.Important);
        }
    // Subclass for clickable, hoverable logo
    public class LogoClickableContainer : ClickableContainer
    {
        private const float hoverScale = 1.1f;
        private const float animDuration = 120;

        protected override bool OnHover(osu.Framework.Input.Events.HoverEvent e)
        {
            this.ScaleTo(hoverScale, animDuration, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(osu.Framework.Input.Events.HoverLostEvent e)
        {
            this.ScaleTo(1f, animDuration, Easing.OutQuint);
            base.OnHoverLost(e);
        }
    }

        private void handleDifficultySelected(Beatmap selectedBeatmap)
        {
            Logger.Log($"Selected difficulty: {selectedBeatmap.DifficultyName} (★{selectedBeatmap.StarRating})", LoggingTarget.Runtime, LogLevel.Important);
            
            // Update the current beatpack's selected beatmap
            if (beatpackManager.CurrentBeatpack.Value != null)
            {
                beatpackManager.CurrentBeatpack.Value.Beatmap = selectedBeatmap;
                Logger.Log($"Updated current beatpack's active beatmap to: {selectedBeatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
                
                // Update the preview to show the new difficulty's info
                beatpackPreview.ShowBeatpack(beatpackManager.CurrentBeatpack.Value);
            }
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
