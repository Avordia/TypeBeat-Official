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
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Overlay for creating new beatpacks.
    /// </summary>
    public partial class AddBeatpackOverlay : FocusedOverlayContainer
    {
        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        private BasicTextBox nameTextBox;
        private BasicTextBox titleTextBox;
        private BasicTextBox artistTextBox;
        private BasicTextBox descriptionTextBox;
        private BasicTextBox tagsTextBox;
        private FilePickerButton musicFilePicker;
        private FilePickerButton backgroundImagePicker;
        private FilePickerButton videoFilePicker;
        
        private string selectedMusicPath = "";
        private string selectedBackgroundPath = "";
        private string selectedVideoPath = "";

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
                    Size = new Vector2(600, 700),
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
                                        Text = "Create New Beatpack",
                                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 28),
                                        Colour = Color4.White
                                    },
                                    createLabel("Beatpack Name*"),
                                    nameTextBox = createTextBox("Enter beatpack name"),
                                    createLabel("Song Title*"),
                                    titleTextBox = createTextBox("Enter song title"),
                                    createLabel("Artist*"),
                                    artistTextBox = createTextBox("Enter artist name"),
                                    createLabel("Description"),
                                    descriptionTextBox = createTextBox("Enter description"),
                                    createLabel("Tags (comma-separated)"),
                                    tagsTextBox = createTextBox("e.g., rock, fast, anime"),
                                    createLabel("Music File*"),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 40,
                                        Child = musicFilePicker = new FilePickerButton(
                                            "Select Audio File",
                                            "Audio Files|*.mp3;*.wav;*.ogg;*.flac;*.m4a|All Files|*.*",
                                            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                                            "Select Music File")
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        }
                                    },
                                    createLabel("Background Image"),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 40,
                                        Child = backgroundImagePicker = new FilePickerButton(
                                            "Select Background Image",
                                            "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files|*.*",
                                            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                                            "Select Background Image")
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        }
                                    },
                                    createLabel("Video (Optional)"),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 40,
                                        Child = videoFilePicker = new FilePickerButton(
                                            "Select Video File",
                                            "Video Files|*.mp4;*.avi;*.mov;*.wmv;*.mkv;*.webm|All Files|*.*",
                                            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                                            "Select Video File")
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        }
                                    },
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
                                                    Text = "Create Beatpack",
                                                    Font = FontUsage.Default.With(family: "Inter", size: 18),
                                                    Colour = Color4.White,
                                                    Padding = new MarginPadding { Horizontal = 20, Vertical = 12 }
                                                },
                                                new ClickableContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Action = createBeatpack
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

            // Wire up file picker callbacks
            musicFilePicker.OnFileSelected = path => selectedMusicPath = path;
            backgroundImagePicker.OnFileSelected = path => selectedBackgroundPath = path;
            videoFilePicker.OnFileSelected = path => selectedVideoPath = path;
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

        private void createBeatpack()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                Logger.Log("[AddBeatpackOverlay] Beatpack name is required", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            if (string.IsNullOrWhiteSpace(titleTextBox.Text))
            {
                Logger.Log("[AddBeatpackOverlay] Song title is required", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            if (string.IsNullOrWhiteSpace(artistTextBox.Text))
            {
                Logger.Log("[AddBeatpackOverlay] Artist name is required", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedMusicPath))
            {
                Logger.Log("[AddBeatpackOverlay] Music file is required", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }

            // Parse tags
            var tags = string.IsNullOrWhiteSpace(tagsTextBox.Text)
                ? new List<string>()
                : tagsTextBox.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            try
            {
                // Create the beatpack
                var newBeatpack = beatpackManager.CreateNewProject(
                    name: nameTextBox.Text.Trim(),
                    title: titleTextBox.Text.Trim(),
                    artist: artistTextBox.Text.Trim(),
                    description: descriptionTextBox.Text?.Trim() ?? "",
                    tags: tags,
                    musicFilePath: selectedMusicPath,
                    backgroundImagePath: selectedBackgroundPath ?? "",
                    videoPath: selectedVideoPath ?? ""
                );

                // Save the project
                if (newBeatpack != null)
                {
                    beatpackManager.SaveProject(newBeatpack);
                    Logger.Log($"[AddBeatpackOverlay] Created beatpack: {newBeatpack.Name}", LoggingTarget.Runtime, LogLevel.Important);
                }
                else
                {
                    Logger.Log("[AddBeatpackOverlay] Failed to create beatpack - returned null", LoggingTarget.Runtime, LogLevel.Error);
                    return;
                }

                // Clear form
                clearForm();

                // Close overlay
                Hide();
            }
            catch (Exception ex)
            {
                Logger.Log($"[AddBeatpackOverlay] Error creating beatpack: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private void clearForm()
        {
            nameTextBox.Text = "";
            titleTextBox.Text = "";
            artistTextBox.Text = "";
            descriptionTextBox.Text = "";
            tagsTextBox.Text = "";
            selectedMusicPath = "";
            selectedBackgroundPath = "";
            selectedVideoPath = "";
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
