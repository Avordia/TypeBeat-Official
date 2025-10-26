// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Main dashboard area showing beatpack details and beatmap list.
    /// </summary>
    public partial class EditBeatmapDashboardContainer : Container
    {
        public Action OnAddBeatmapClicked { get; set; }
        public Action<LocalBeatmap> OnEditBeatmap { get; set; }

        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        private LocalBeatpack currentBeatpack;

        private BasicScrollContainer contentContainer;
        private Container emptyStateContainer;
        private SpriteText titleText;
        private SpriteText artistText;
        private FillFlowContainer beatmapGrid;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 20, 255)
                },
                emptyStateContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new SpriteText
                    {
                        Text = "Select or create a beatpack to get started",
                        Font = FontUsage.Default.With(family: "Inter", size: 24),
                        Colour = Color4.Gray,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                },
                contentContainer = new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    ClampExtension = 30,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(30),
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 30),
                            Children = new Drawable[]
                            {
                                // Top section: Beatpack info
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Children = new Drawable[]
                                    {
                                        // Left: Title and Artist
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Width = 0.6f,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 10),
                                            Children = new Drawable[]
                                            {
                                                titleText = new SpriteText
                                                {
                                                    Font = FontUsage.Default.With(family: "Inter-Bold", size: 48),
                                                    Colour = Color4.White
                                                },
                                                artistText = new SpriteText
                                                {
                                                    Font = FontUsage.Default.With(family: "Inter", size: 24),
                                                    Colour = Color4.LightGray
                                                }
                                            }
                                        },
                                        // Right: Action buttons
                                        new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 10),
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Children = new Drawable[]
                                            {
                                                // Row 1: File management
                                                new FillFlowContainer
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(10, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        createFilePickerButton("Change Image", "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files|*.*", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
                                                        createFilePickerButton("Change Video", "Video Files|*.mp4;*.avi;*.mov;*.wmv;*.mkv;*.webm|All Files|*.*", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                                                    }
                                                },
                                                // Row 2: Main actions
                                                new FillFlowContainer
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(10, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        createActionButton("Edit Info", new Color4(100, 100, 100, 255), editBeatpack),
                                                        createActionButton("Export for Testing", new Color4(33, 150, 243, 255), exportForTesting),
                                                        createActionButton("Publish Online", new Color4(76, 175, 80, 255), publishOnline)
                                                    }
                                                },
                                                // Row 3: Delete actions
                                                new FillFlowContainer
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(10, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        createActionButton("Delete Locally", new Color4(255, 152, 0, 255), deleteLocally),
                                                        createActionButton("Delete Online", new Color4(244, 67, 54, 255), deleteOnline)
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                // Middle section: Beatmaps
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Children = new Drawable[]
                                    {
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 15),
                                            Children = new Drawable[]
                                            {
                                                new SpriteText
                                                {
                                                    Text = "Difficulties",
                                                    Font = FontUsage.Default.With(family: "Inter-Bold", size: 32),
                                                    Colour = Color4.White
                                                },
                                                beatmapGrid = new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Full,
                                                    Spacing = new Vector2(15, 15)
                                                }
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

        public void UpdateBeatpack(LocalBeatpack beatpack)
        {
            currentBeatpack = beatpack;

            if (beatpack == null)
            {
                contentContainer.FadeOut(200);
                emptyStateContainer.FadeIn(200);
                return;
            }

            emptyStateContainer.FadeOut(200);
            contentContainer.FadeIn(200);

            // Update texts
            titleText.Text = string.IsNullOrEmpty(beatpack.Title) ? beatpack.Name : beatpack.Title;
            artistText.Text = string.IsNullOrEmpty(beatpack.Artist) ? "Unknown Artist" : beatpack.Artist;

            // Update beatmap grid
            beatmapGrid.Clear();

            if (beatpack.LocalBeatmaps != null)
            {
                foreach (var beatmap in beatpack.LocalBeatmaps)
                {
                    beatmapGrid.Add(new BeatmapDifficultyItem(beatmap, onEditBeatmap, onDeleteBeatmap));
                }
            }

            // Add "Add beatmap" button
            beatmapGrid.Add(createAddBeatmapButton());
        }

        private void onEditBeatmap(LocalBeatmap beatmap)
        {
            // Invoke the callback to let parent handle navigation
            OnEditBeatmap?.Invoke(beatmap);
        }

        private void onDeleteBeatmap(LocalBeatmap beatmap)
        {
            if (currentBeatpack == null) return;

            // Remove beatmap from the beatpack
            currentBeatpack.LocalBeatmaps?.Remove(beatmap);
            
            // Save the beatpack
            beatpackManager.SaveProject(currentBeatpack);
            
            // Refresh the display
            UpdateBeatpack(currentBeatpack);
            
            Logger.Log($"[EditBeatmapDashboard] Deleted beatmap: {beatmap.DifficultyName}", 
                LoggingTarget.Runtime, 
                LogLevel.Important);
        }

        private Container createActionButton(string text, Color4 colour, Action action = null)
        {
            return new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
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
                        Padding = new MarginPadding { Horizontal = 12, Vertical = 8 }
                    },
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = action
                    }
                }
            };
        }

        private FilePickerButton createFilePickerButton(string text, string filter, string initialDirectory)
        {
            return new FilePickerButton(text, filter, initialDirectory);
        }

        private Container createAddBeatmapButton()
        {
            var container = new Container
            {
                Width = 400,
                Height = 60,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(255, 255, 255, 50)
                    },
                    new SpriteText
                    {
                        Text = "+",
                        Font = FontUsage.Default.With(size: 36, weight: "Bold"),
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    },
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = () => OnAddBeatmapClicked?.Invoke()
                    }
                }
            };

            return container;
        }

        private void editBeatpack()
        {
            if (currentBeatpack == null) return;
            
            Logger.Log($"[Dashboard] TODO: Open edit overlay for {currentBeatpack.Name}", LoggingTarget.Runtime, LogLevel.Important);
            // TODO: Open AddBeatpackOverlay in edit mode
        }

        private void exportForTesting()
        {
            if (currentBeatpack == null) return;
            
            Logger.Log($"[Dashboard] Exporting {currentBeatpack.Name} for testing...", LoggingTarget.Runtime, LogLevel.Important);
            bool success = beatpackManager.ExportForTesting(currentBeatpack);
            
            if (success)
            {
                Logger.Log($"[Dashboard] ✓ Exported {currentBeatpack.Name} successfully", LoggingTarget.Runtime, LogLevel.Important);
            }
            else
            {
                Logger.Log($"[Dashboard] ✗ Failed to export {currentBeatpack.Name}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private void publishOnline()
        {
            if (currentBeatpack == null) return;
            
            Logger.Log($"[Dashboard] TODO: Publish {currentBeatpack.Name} online", LoggingTarget.Runtime, LogLevel.Important);
            // TODO: Implement online publishing
        }

        private void deleteLocally()
        {
            if (currentBeatpack == null) return;
            
            Logger.Log($"[Dashboard] Deleting {currentBeatpack.Name} locally...", LoggingTarget.Runtime, LogLevel.Important);
            // TODO: Add confirmation dialog
            beatpackManager.DeleteProject(currentBeatpack.Id);
            Logger.Log($"[Dashboard] ✓ Deleted {currentBeatpack.Name} locally", LoggingTarget.Runtime, LogLevel.Important);
        }

        private void deleteOnline()
        {
            if (currentBeatpack == null) return;
            
            Logger.Log($"[Dashboard] TODO: Delete {currentBeatpack.Name} from online", LoggingTarget.Runtime, LogLevel.Important);
            // TODO: Implement online deletion
        }
    }
}

