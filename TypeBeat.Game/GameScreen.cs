using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;
using osu.Framework.Logging;
using osu.Framework.IO.Stores;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Ui;
using TypeBeat.Game.Gameplay.Judgement;
using TypeBeat.Game.Gameplay.Input;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Scheduling;
using TypeBeat.Game.Filehandling;

namespace TypeBeat.Game
{
    public partial class GameScreen : Screen
    {
        private readonly Beatpack beatpack;
        private readonly Beatmap beatmap;
        private readonly Sprite backgroundSprite;
    private readonly SpriteText accuracyText;
    private readonly SpriteText comboText;
        private float currentAccuracy = 100.0f;
        private readonly Ui.CentralWordContainer centralWord;
        private readonly Ui.WordPreviews wordPreviews;
        private readonly Container playfield;
        private readonly LayoutConfig layoutConfig = new LayoutConfig
        {
            HalfGapXFraction = 0.12f // Increase this to make lines stop FURTHER APART
            // Default is 0.06 (6% of screen width)
            // 0.12 = 12% (twice as wide)
            // 0.20 = 20% (much wider)
        };
        private readonly NoteAppearanceConfig appearanceConfig = new NoteAppearanceConfig();
        private readonly NoteScheduler noteScheduler;
    private readonly SpriteText debugText;

    private readonly TypingManager typing = new TypingManager();
    private readonly HitWindows hitWindows = new HitWindows();
    private readonly TypeBeat.Game.Gameplay.Scoring.ScoreProcessor score = new TypeBeat.Game.Gameplay.Scoring.ScoreProcessor();
    private int currentSegmentIndex = 0;
    private Beatmaps.WordSegment[] segmentsArr = System.Array.Empty<Beatmaps.WordSegment>();

    private Container pauseOverlay;
    private bool isPaused = false;
    private double gameplayStartClockMs = 0;
    private Track gameTrack;

        [Resolved]
        private IRenderer renderer { get; set; }

        [Resolved]
        private AudioManager audioManager { get; set; }

        public GameScreen(Beatpack beatpack, Beatmap beatmap)
        {
            this.beatpack = beatpack;
            this.beatmap = beatmap;

            InternalChildren = new Drawable[]
            {
                // Background
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0.5f // Dimmed for gameplay visibility
                },
                // Debug overlay (top-left)
                new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(10),
                    Child = debugText = new SpriteText
                    {
                        Font = new FontUsage("Kodchasan", size: 16, weight: "Bold"),
                        Colour = Colour4.Yellow,
                        Shadow = true,
                        ShadowColour = Colour4.Black,
                        Text = "debug..."
                    }
                },
                // HitDetector at center
                new HitDetector(),
                // Playfield (notes layer) between background/hit detector and UI
                playfield = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Children = new Drawable[]
                    {
                        noteScheduler = new NoteScheduler(layoutConfig, appearanceConfig)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft
                        }
                    }
                },
                // Center word container (UI layer)
                centralWord = new Ui.CentralWordContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = 0
                },
                // Word previews stacked above the word container
                wordPreviews = new Ui.WordPreviews
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = -80
                },
                // Top-right UI container
                new Container
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(30),
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Combo:",
                                        Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                        Colour = Colour4.White,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    },
                                    comboText = new SpriteText
                                    {
                                        Text = "0",
                                        Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                        Colour = Colour4.White,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Accuracy:",
                                        Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                        Colour = Colour4.White,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    },
                                    accuracyText = new SpriteText
                                    {
                                        Text = "100.0%",
                                        Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                        Colour = Colour4.Lime,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Pause overlay (hidden by default)
            AddInternal(pauseOverlay = createPauseOverlay());
            pauseOverlay.Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load game audio from beatpack
            if (!string.IsNullOrEmpty(beatpack.MusicPath))
            {
                try
                {
                    using (var stream = File.OpenRead(beatpack.FilePath))
                    {
                        var beatmapAssetStorage = new ZipArchiveResourceStore(stream);
                        var trackStore = audioManager.GetTrackStore(beatmapAssetStorage);
                        gameTrack = trackStore.Get(beatpack.MusicPath);
                        
                        if (gameTrack != null)
                        {
                            gameTrack.Looping = false; // Don't loop gameplay music
                            Logger.Log($"[GameScreen] Loaded audio track: {beatpack.MusicPath}", LoggingTarget.Runtime, LogLevel.Important);
                        }
                        else
                        {
                            Logger.Log($"[GameScreen] Failed to load audio track: {beatpack.MusicPath}", LoggingTarget.Runtime, LogLevel.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load game audio");
                }
            }
            
            // Load background image
            if (!string.IsNullOrEmpty(beatpack.BackgroundImagePath))
            {
                Schedule(() =>
                {
                    try
                    {
                        using (var stream = File.OpenRead(beatpack.FilePath))
                        using (var archive = new ZipArchive(stream))
                        {
                            var entry = archive.GetEntry(beatpack.BackgroundImagePath);
                            if (entry != null)
                            {
                                using (var imageStream = entry.Open())
                                using (var memoryStream = new MemoryStream())
                                {
                                    imageStream.CopyTo(memoryStream);
                                    memoryStream.Position = 0;
                                    var texture = Texture.FromStream(renderer, memoryStream);
                                    backgroundSprite.Texture = texture;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to load background image");
                    }
                });
            }

            // Initialize center word and previews from beatmap
            try
            {
                var segments = beatmap?.MapData;
                if (segments != null && segments.Any())
                {
                    segmentsArr = segments.ToArray();
                    currentSegmentIndex = 0;
                    typing.SetSegment(segmentsArr[currentSegmentIndex]);

                    string word0 = toWord(segmentsArr.ElementAtOrDefault(0));
                    string word1 = toWord(segmentsArr.ElementAtOrDefault(1));
                    string word2 = toWord(segmentsArr.ElementAtOrDefault(2));

                    // Center word is current (index 0)
                    centralWord.SetWord(word0);
                    // Preview above should show only the NEXT word
                    wordPreviews.SetPreviews(string.Empty, word1, string.Empty);

                    // Spawn visuals for the first segment
                    noteScheduler.LoadSegment(segmentsArr.ElementAtOrDefault(0));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize word previews");
            }

            Logger.Log($"GameScreen loaded: {beatmap?.Title} - {beatmap?.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"Map has {beatmap?.MapData?.Count ?? 0} segments", LoggingTarget.Runtime, LogLevel.Important);
        }

        private static string toWord(Beatmaps.WordSegment segment)
        {
            if (segment?.Notes == null)
                return string.Empty;

            // Concatenate characters, force uppercase. '/' is kept as '/'.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var n in segment.Notes)
            {
                if (string.IsNullOrEmpty(n.Character)) continue;
                sb.Append(n.Character.ToUpperInvariant());
            }
            return sb.ToString();
        }

        public void UpdateAccuracy(float accuracy)
        {
            currentAccuracy = accuracy;
            accuracyText.Text = $"{accuracy:F1}%";
            
            // Color code based on accuracy
            if (accuracy >= 95)
                accuracyText.Colour = Colour4.Lime;
            else if (accuracy >= 90)
                accuracyText.Colour = Colour4.Yellow;
            else if (accuracy >= 80)
                accuracyText.Colour = Colour4.Orange;
            else
                accuracyText.Colour = Colour4.Red;
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            
            // Start the game audio - this marks the beginning of gameplay
            if (gameTrack != null)
            {
                gameTrack.Start();
                Logger.Log("[GameScreen] Started gameplay audio - game has begun!", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            // Capture when gameplay starts to make all timing relative to audio start
            gameplayStartClockMs = Clock.CurrentTime;
            noteScheduler.TimeOffsetMs = gameplayStartClockMs;
            Logger.Log($"GameScreen entered with beatmap: {beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // Stop and dispose the game audio when exiting
            if (gameTrack != null)
            {
                if (gameTrack.IsRunning)
                {
                    gameTrack.Stop();
                    Logger.Log("[GameScreen] Stopped gameplay audio", LoggingTarget.Runtime, LogLevel.Important);
                }
                gameTrack.Dispose();
                gameTrack = null;
                Logger.Log("[GameScreen] Disposed gameplay audio track", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        protected override void Update()
        {
            base.Update();

            if (isPaused) return;

            // Auto-miss overdue notes (beyond late window) without key presses
            double nowRel = Clock.CurrentTime - gameplayStartClockMs;
            if (nowRel < 0) nowRel = 0;
            int autoMissed = typing.AutoConsumeMisses(nowRel, hitWindows, out bool segCompleted);
            if (autoMissed > 0)
            {
                // Apply misses and update HUD for each consumed character
                for (int i = 0; i < autoMissed; i++)
                {
                    noteScheduler.HitCurrentNote(); // Make missed notes disappear too
                    score.Apply(JudgementType.Miss);
                    centralWord.ConsumeNext();
                }
                UpdateAccuracy(score.GetAccuracyPercent());
                comboText.Text = score.Combo.ToString();
            }

            if (segCompleted)
            {
                currentSegmentIndex++;
                if (currentSegmentIndex < segmentsArr.Length)
                {
                    var seg = segmentsArr[currentSegmentIndex];
                    typing.SetSegment(seg);
                    string w0 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex));
                    string w1 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 1));
                    centralWord.SetWord(w0);
                    // Show only the next word above
                    wordPreviews.SetPreviews(string.Empty, w1, string.Empty);
                    noteScheduler.LoadSegment(seg);
                }
            }

            // Update debug HUD
            double rel = Clock.CurrentTime - gameplayStartClockMs; if (rel < 0) rel = 0;
            debugText.Text = $"t={rel:F0}ms seg={currentSegmentIndex+1}/{segmentsArr.Length} spawned={noteScheduler.SpawnedCount} drawChildren={playfield.Children.Count()}";
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                // Toggle pause overlay
                isPaused = !isPaused;
                if (isPaused)
                    pauseOverlay.FadeIn(150);
                else
                    pauseOverlay.FadeOut(150);
                return true;
            }

            // Ignore inputs while paused
            if (isPaused)
                return true;

            // Accept only A-Z and Space
            char? keyChar = null;
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                int offset = (int)e.Key - (int)Key.A;
                keyChar = (char)('A' + offset);
            }
            else if (e.Key == Key.Space)
            {
                // Map Space to the slash token for typing logic
                keyChar = '/';
            }

            if (keyChar == null)
                return base.OnKeyDown(e);

            // Handle typing
            double now = Clock.CurrentTime - gameplayStartClockMs; // gameplay-relative time
            if (now < 0) now = 0;
            var res = typing.HandleKeyPress(keyChar.Value, now, hitWindows);
            
            // Only process if the input was consumed (within active note window)
            if (!res.Consumed)
                return true; // Ignore keypresses outside note windows (QoL for ADHD players)
            
            // Key was consumed - make the visual note disappear
            noteScheduler.HitCurrentNote();
            
            score.Apply(res.Judgement);
            UpdateAccuracy(score.GetAccuracyPercent());
            comboText.Text = score.Combo.ToString();
            centralWord.ConsumeNext();

            if (res.SegmentCompleted)
            {
                // Advance to next segment (previews shift here only)
                currentSegmentIndex++;
                if (currentSegmentIndex < segmentsArr.Length)
                {
                    var seg = segmentsArr[currentSegmentIndex];
                    typing.SetSegment(seg);
                    string w0 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex));
                    string w1 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 1));
                    centralWord.SetWord(w0);
                    // Show only the next word above
                    wordPreviews.SetPreviews(string.Empty, w1, string.Empty);
                    noteScheduler.LoadSegment(seg);
                }
            }

            return true;
        }

        private Container createPauseOverlay()
        {
            var overlay = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0.0f,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black, Alpha = 0.6f },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new SpriteText { Text = "Paused", Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"), Colour = Colour4.White },
                            new ClickableContainer
                            {
                                Action = () => pauseOverlay.FadeOut(150),
                                AutoSizeAxes = Axes.Both,
                                Child = new SpriteText { Text = "Resume", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White }
                            },
                            new ClickableContainer
                            {
                                Action = () => this.Exit(),
                                AutoSizeAxes = Axes.Both,
                                Child = new SpriteText { Text = "Exit", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White }
                            }
                        }
                    }
                }
            };

            return overlay;
        }
    }
}