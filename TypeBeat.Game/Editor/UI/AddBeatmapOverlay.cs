// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Configuration;
using TypeBeat.Game.Online;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Overlay for adding beatmaps to a beatpack.
    /// </summary>
    public partial class AddBeatmapOverlay : FocusedOverlayContainer
    {
        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        [Resolved]
        private AuthenticationService authService { get; set; }

        private BasicTextBox difficultyNameTextBox;
        private BasicTextBox bpmTextBox;
        private BasicDropdown<string> gamemodeDropdown;
        private BasicTextBox starRatingTextBox;
        private BasicTextBox sourceTextBox;
        private BasicTextBox tagsTextBox;
        private BasicTextBox previewTimeTextBox;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                // Semi-transparent background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0, 0, 0, 180)
                },
                new ClickableContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Action = Hide
                },
                // Modal container
                new Container
                {
                    Size = new Vector2(600, 650),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 15,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(42, 42, 42, 255)
                        },
                        new BasicScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ClampExtension = 30,
                            Child = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 15),
                                Padding = new MarginPadding(30),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Add Beatmap Difficulty",
                                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 28),
                                        Colour = Color4.White
                                    },
                                    createLabel("Difficulty Name*"),
                                    difficultyNameTextBox = createTextBox("e.g., Easy, Normal, Hard"),
                                    createLabel("BPM*"),
                                    bpmTextBox = createTextBox("Beats per minute (e.g., 120)"),
                                    createLabel("Gamemode*"),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 40,
                                        Child = gamemodeDropdown = new BasicDropdown<string>
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Items = Gamemodes.Available
                                        }
                                    },
                                    createLabel("Star Rating (Optional)"),
                                    starRatingTextBox = createTextBox("Difficulty rating (e.g., 3.5)"),
                                    createLabel("Source (Optional)"),
                                    sourceTextBox = createTextBox("Original source"),
                                    createLabel("Tags (Optional, comma-separated)"),
                                    tagsTextBox = createTextBox("Additional tags"),
                                    createLabel("Preview Time (Optional, ms)"),
                                    previewTimeTextBox = createTextBox("Preview start time in milliseconds"),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 50,
                                        Padding = new MarginPadding { Top = 15 },
                                        Child = new Container
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Masking = true,
                                            CornerRadius = 8,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = new Color4(76, 175, 80, 255)
                                                },
                                                new SpriteText
                                                {
                                                    Text = "Add Beatmap",
                                                    Font = FontUsage.Default.With(family: "Inter", size: 18),
                                                    Colour = Color4.White,
                                                    Padding = new MarginPadding { Horizontal = 20, Vertical = 12 }
                                                },
                                                new ClickableContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Action = addBeatmap
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

            // Set default values
            gamemodeDropdown.Current.Value = Gamemodes.Default;
        }

        private SpriteText createLabel(string text)
        {
            return new SpriteText
            {
                Text = text,
                Font = FontUsage.Default.With(family: "Inter", size: 14),
                Colour = Color4.LightGray
            };
        }

        private BasicTextBox createTextBox(string placeholder)
        {
            return new BasicTextBox
            {
                RelativeSizeAxes = Axes.X,
                Height = 40,
                PlaceholderText = placeholder,
                CornerRadius = 5
            };
        }

        private void addBeatmap()
        {
            // Check if a beatpack is selected
            if (beatpackManager.CurrentBeatpack.Value == null)
                return;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(difficultyNameTextBox.Text))
                return;

            if (string.IsNullOrWhiteSpace(bpmTextBox.Text) || !double.TryParse(bpmTextBox.Text, out double bpm))
                return;

            // Parse optional fields
            float.TryParse(starRatingTextBox.Text, out float starRating);
            int.TryParse(previewTimeTextBox.Text, out int previewTime);

            var tags = string.IsNullOrWhiteSpace(tagsTextBox.Text)
                ? new List<string>()
                : tagsTextBox.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            // Get creator from logged-in user
            var creatorUsername = authService.CurrentUser.Value?.Username ?? "Unknown";

            // Create the beatmap
            var beatmap = new LocalBeatmap
            {
                DifficultyName = difficultyNameTextBox.Text,
                BPM = bpm,
                Gamemode = gamemodeDropdown.Current.Value ?? Gamemodes.Default,
                StarRating = starRating,
                Creators = new List<string> { creatorUsername }, // Single creator - the logged-in user
                Source = sourceTextBox.Text ?? "",
                Tags = tags,
                PreviewTime = previewTime,
                MapData = new List<WordSegment>(),
                // Inherit from beatpack
                Artist = beatpackManager.CurrentBeatpack.Value.Artist,
                Title = beatpackManager.CurrentBeatpack.Value.Title,
                Audio = beatpackManager.CurrentBeatpack.Value.MusicFilePath,
                BackgroundImage = beatpackManager.CurrentBeatpack.Value.BackgroundImagePath,
                Video = beatpackManager.CurrentBeatpack.Value.VideoPath
            };

            // Add to current beatpack
            beatpackManager.CurrentBeatpack.Value.LocalBeatmaps.Add(beatmap);

            // Save the project
            beatpackManager.SaveProject(beatpackManager.CurrentBeatpack.Value);

            // Trigger update by re-setting the value
            var temp = beatpackManager.CurrentBeatpack.Value;
            beatpackManager.CurrentBeatpack.Value = null;
            beatpackManager.CurrentBeatpack.Value = temp;

            // Clear form
            clearForm();

            // Close overlay
            Hide();
        }

        private void clearForm()
        {
            difficultyNameTextBox.Text = "";
            bpmTextBox.Text = "";
            gamemodeDropdown.Current.Value = Gamemodes.Default;
            starRatingTextBox.Text = "";
            sourceTextBox.Text = "";
            tagsTextBox.Text = "";
            previewTimeTextBox.Text = "";
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(200, Easing.OutQuint);
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
    }
}

