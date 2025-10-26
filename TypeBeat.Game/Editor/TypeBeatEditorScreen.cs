// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Main TypeBeat editor screen for editing .tbmd beatmap files.
    /// </summary>
    public partial class TypeBeatEditorScreen : Screen
    {
        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        private readonly LocalBeatmap beatmap;
        private readonly LocalBeatpack beatpack;
        
        private Track audioTrack;
        private bool hasUnsavedChanges;
        
        // UI Components
        private Container headerContainer;
        private Container timelineControlsContainer;
        private TypeBeatTimeline timeline;
        private TypeBeatPreviewArea previewArea;
        private Container confirmExitOverlay;
        
        // Editor state
        private double currentTime;
        private bool isPlaying;
        private double tempo = 120;
        private int step = 4; // 1/4 by default
        private bool showTail = false;
        private double defaultTailLength = 1.5; // In seconds
        private bool magnetEnabled = true;
        
        public TypeBeatEditorScreen(LocalBeatpack beatpack, LocalBeatmap beatmap)
        {
            this.beatpack = beatpack;
            this.beatmap = beatmap;
            this.tempo = beatmap.BPM;
        }
        
        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                // Background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                // Main content
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        // Header
                        headerContainer = createHeader(),
                        // Timeline controls
                        timelineControlsContainer = createTimelineControls(),
                        // Main editor area
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 120 }, // Header + controls height
                            Children = new Drawable[]
                            {
                                // Timeline container (full height) with horizontal scrolling (wheel only, no drag)
                                new WheelOnlyScrollContainer(Direction.Horizontal)
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    ScrollbarVisible = true,
                                    Child = timeline = new TypeBeatTimeline(beatmap, tempo, step, magnetEnabled, defaultTailLength, showTail)
                                    {
                                        Width = 20000, // Large width for near-infinite scrolling (200 measures)
                                        RelativeSizeAxes = Axes.Y,
                                        Padding = new MarginPadding { Horizontal = 30 }
                                    }
                                }
                            }
                        }
                    }
                },
                // Confirm exit overlay (hidden by default)
                confirmExitOverlay = createConfirmExitOverlay()
            };
            
            // Load audio track
            loadAudioTrack();
            
            // Subscribe to timeline changes to track unsaved changes
            timeline.OnContentChanged += () => hasUnsavedChanges = true;
        }
        
        private Container createHeader()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.9f
                    },
                    // Beatpack name on the right
                    new SpriteText
                    {
                        Text = $"BEATPACK NAME: {beatpack?.Name ?? "Unknown"}",
                        Font = FontUsage.Default.With(family: "Inter", size: 16),
                        Colour = Color4.White,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Margin = new MarginPadding { Right = 30 }
                    },
                    // Left side buttons
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(20, 0),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 30 },
                        Children = new Drawable[]
                        {
                            createHeaderButton("SAVE", save),
                            createHeaderButton("EXIT", tryExit)
                        }
                    },
                    // Play button in center
                    createPlayButton()
                }
            };
        }
        
        private ClickableContainer createHeaderButton(string text, Action action)
        {
            // Use a ClickableContainer as the root and autosize it. Avoid setting RelativeSizeAxes
            // on children inside a FillFlowContainer that is autosizing in X to prevent layout cycles.
            return new ClickableContainer
            {
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = text,
                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 18),
                        Colour = Color4.White,
                        Padding = new MarginPadding { Horizontal = 5 }
                    }
                },
                Action = action
            };
        }
        
        private Container createTimelineControls()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Y = 60, // Below header
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(30, 30, 30, 255)
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(30, 0),
                        Padding = new MarginPadding { Horizontal = 30 },
                        Children = new Drawable[]
                        {
                            // Timestamp
                            createControlItem("0:01:84", 100),
                            // Tempo
                            createControlItem($"TEMPO: {(int)tempo}", 120),
                            // Step (clickable)
                            createClickableControlItem($"STEP: 1/{step}", 120, cycleStep),
                            // Show tail toggle (clickable)
                            createClickableControlItem($"SHOW TAIL: {(showTail ? "TRUE" : "FALSE")}", 180, toggleShowTail),
                            // Default tail length (clickable)
                            createClickableControlItem($"DEFAULT TAIL LENGTH: 1:{(int)(defaultTailLength * 10)}", 280, cycleDefaultTailLength)
                        }
                    }
                }
            };
        }
        
        private Container createControlItem(string text, float width)
        {
            return new Container
            {
                Width = width,
                RelativeSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = text,
                        Font = FontUsage.Default.With(family: "Inter", size: 14),
                        Colour = Color4.White,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft
                    }
                }
            };
        }
        
        private Container createClickableControlItem(string text, float width, Action onClick)
        {
            var spriteText = new SpriteText
            {
                Text = text,
                Font = FontUsage.Default.With(family: "Inter", size: 14),
                Colour = Color4.White,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft
            };
            
            return new Container
            {
                Width = width,
                RelativeSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Transparent
                    },
                    spriteText,
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = onClick,
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                }
            };
        }
        
        private Container createPlayButton()
        {
            return new Container
            {
                Width = 40,
                Height = 40,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                CornerRadius = 5,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(60, 60, 60, 255)
                    },
                    new SpriteText
                    {
                        Text = "▶",
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    },
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = togglePlayback
                    }
                }
            };
        }
        
        private Container createConfirmExitOverlay()
        {
            var overlay = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.8f
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Height = 200,
                        Masking = true,
                        CornerRadius = 10,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = new Color4(40, 40, 40, 255)
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding(20),
                                Spacing = new Vector2(0, 20),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Unsaved Changes",
                                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 24),
                                        Colour = Color4.White,
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre
                                    },
                                    new SpriteText
                                    {
                                        Text = "Do you want to save your changes?",
                                        Font = FontUsage.Default.With(family: "Inter", size: 16),
                                        Colour = Color4.LightGray,
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(10, 0),
                                            Anchor = Anchor.TopCentre,
                                            Origin = Anchor.TopCentre,
                                            Children = new Drawable[]
                                            {
                                                createOverlayButton("Save", Color4.Green, () => { save(); this.Exit(); }),
                                                createOverlayButton("Don't Save", Color4.Orange, () => { hasUnsavedChanges = false; this.Exit(); }),
                                                createOverlayButton("Cancel", Color4.Red, hideConfirmExitOverlay)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            
            return overlay;
        }
        
        private Container createOverlayButton(string text, Color4 colour, Action action)
        {
            return new Container
            {
                Width = 100,
                Height = 40,
                Masking = true,
                CornerRadius = 5,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour
                    },
                    new SpriteText
                    {
                        Text = text,
                        Font = FontUsage.Default.With(family: "Inter", size: 14),
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    },
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = action
                    }
                }
            };
        }
        
        private void loadAudioTrack()
        {
            // TODO: Load audio track from beatpack
            Logger.Log($"[TypeBeatEditor] Loading audio for beatmap: {beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
        }
        
        private void save()
        {
            Logger.Log($"[TypeBeatEditor] Saving beatmap: {beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Update beatmap data from timeline
            beatmap.MapData = timeline.GetMapData();
            
            // Save through beatpack manager
            beatpackManager.SaveBeatmap(beatpack, beatmap);
            
            hasUnsavedChanges = false;
        }
        
        private void tryExit()
        {
            if (hasUnsavedChanges)
            {
                showConfirmExitOverlay();
            }
            else
            {
                this.Exit();
            }
        }
        
        private void showConfirmExitOverlay()
        {
            confirmExitOverlay.FadeIn(200);
        }
        
        private void hideConfirmExitOverlay()
        {
            confirmExitOverlay.FadeOut(200);
        }
        
        private void cycleStep()
        {
            // Cycle through step values: 1, 2, 4, 8, 16
            step = step switch
            {
                1 => 2,
                2 => 4,
                4 => 8,
                8 => 16,
                16 => 1,
                _ => 4
            };
            
            // Recreate timeline with new step
            recreateTimeline();
        }
        
        private void toggleShowTail()
        {
            showTail = !showTail;
            
            // Recreate timeline with new showTail setting
            recreateTimeline();
        }
        
        private void cycleDefaultTailLength()
        {
            // Cycle through common tail lengths: 0.5, 1.0, 1.5, 2.0
            defaultTailLength = defaultTailLength switch
            {
                0.5 => 1.0,
                1.0 => 1.5,
                1.5 => 2.0,
                2.0 => 0.5,
                _ => 1.5
            };
            
            // Recreate timeline with new default tail length
            recreateTimeline();
        }
        
        private void recreateTimeline()
        {
            // Save current map data
            var currentMapData = timeline?.GetMapData();
            if (currentMapData != null)
            {
                beatmap.MapData = currentMapData;
            }
            
            // Update timeline controls text without recreating the container
            updateTimelineControlsText();
            
            // Recreate timeline - find the scroll container and replace timeline
            var scrollContainer = InternalChildren.OfType<Container>()
                .FirstOrDefault(c => c.Children.Any(ch => ch is WheelOnlyScrollContainer));
            
            if (scrollContainer != null)
            {
                var scroll = scrollContainer.Children.OfType<WheelOnlyScrollContainer>().FirstOrDefault();
                if (scroll != null)
                {
                    scroll.Clear();
                    scroll.Add(timeline = new TypeBeatTimeline(beatmap, tempo, step, magnetEnabled, defaultTailLength, showTail)
                    {
                        Width = 20000,
                        RelativeSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = 30 }
                    });
                    
                    timeline.OnContentChanged += () => hasUnsavedChanges = true;
                    
                    // Update tail visibility after timeline is fully loaded
                    Schedule(() => timeline.UpdateAllNoteTails());
                }
            }
        }
        
        private void updateTimelineControlsText()
        {
            // Find and update the text elements without recreating the entire container
            var fillFlow = timelineControlsContainer.Children.OfType<FillFlowContainer>().FirstOrDefault();
            if (fillFlow != null)
            {
                var containers = fillFlow.Children.OfType<Container>().ToList();
                if (containers.Count >= 5)
                {
                    // Update STEP text (index 2)
                    var stepText = containers[2].Children.OfType<SpriteText>().FirstOrDefault();
                    if (stepText != null)
                        stepText.Text = $"STEP: 1/{step}";
                    
                    // Update SHOW TAIL text (index 3)
                    var showTailText = containers[3].Children.OfType<SpriteText>().FirstOrDefault();
                    if (showTailText != null)
                        showTailText.Text = $"SHOW TAIL: {(showTail ? "TRUE" : "FALSE")}";
                    
                    // Update DEFAULT TAIL LENGTH text (index 4)
                    var tailLengthText = containers[4].Children.OfType<SpriteText>().FirstOrDefault();
                    if (tailLengthText != null)
                        tailLengthText.Text = $"DEFAULT TAIL LENGTH: 1:{(int)(defaultTailLength * 10)}";
                }
            }
        }
        
        private void togglePlayback()
        {
            isPlaying = !isPlaying;
            
            if (isPlaying)
            {
                audioTrack?.Start();
                Logger.Log("[TypeBeatEditor] Playback started", LoggingTarget.Runtime, LogLevel.Debug);
            }
            else
            {
                audioTrack?.Stop();
                Logger.Log("[TypeBeatEditor] Playback stopped", LoggingTarget.Runtime, LogLevel.Debug);
            }
        }
        
        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300, Easing.OutQuint);
        }
        
        public override bool OnExiting(ScreenExitEvent e)
        {
            audioTrack?.Stop();
            this.FadeOut(300, Easing.OutQuint);
            return base.OnExiting(e);
        }
        
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Escape)
            {
                tryExit();
                return true;
            }
            
            if (e.Key == osuTK.Input.Key.Space)
            {
                togglePlayback();
                return true;
            }
            
            return base.OnKeyDown(e);
        }
    }
    
    /// <summary>
    /// Custom scroll container that only allows wheel scrolling, not drag scrolling
    /// </summary>
    public partial class WheelOnlyScrollContainer : BasicScrollContainer
    {
        public WheelOnlyScrollContainer(Direction scrollDirection = Direction.Vertical)
            : base(scrollDirection)
        {
        }
        
        protected override bool OnDragStart(DragStartEvent e) => false;
        protected override void OnDrag(DragEvent e) { }
        protected override void OnDragEnd(DragEndEvent e) { }
    }
}
