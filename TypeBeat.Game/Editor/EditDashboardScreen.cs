// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Editor.UI;
using TypeBeat.Game.Online;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Main editor dashboard screen showing beatpack list and beatmap dashboard.
    /// </summary>
    public partial class EditDashboardScreen : Screen
    {
        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        [Resolved]
        private AuthenticationService authService { get; set; }

        [Resolved]
        private GameHost host { get; set; }

        private EditHeader header;
        private EditBeatpackListContainer beatpackListContainer;
        private EditBeatmapDashboardContainer beatmapDashboardContainer;
        private AddBeatpackOverlay addBeatpackOverlay;
        private AddBeatmapOverlay addBeatmapOverlay;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                // Background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 20, 255)
                },
                // Main content
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        // Header
                        header = new EditHeader
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 60,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        // Content area below header
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 60 },
                            Children = new Drawable[]
                            {
                                // Left sidebar (beatpack list)
                                beatpackListContainer = new EditBeatpackListContainer
                                {
                                    Width = 350,
                                    RelativeSizeAxes = Axes.Y,
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.TopLeft,
                                    OnAddBeatpackClicked = () => addBeatpackOverlay?.Show()
                                },
                                // Main area (beatmap dashboard)
                                beatmapDashboardContainer = new EditBeatmapDashboardContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Left = 350 },
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    OnAddBeatmapClicked = () => addBeatmapOverlay?.Show(),
                                    OnEditBeatmap = openBeatmapEditor
                                }
                            }
                        }
                    }
                },
                // Overlays (drawn on top)
                addBeatpackOverlay = new AddBeatpackOverlay(),
                addBeatmapOverlay = new AddBeatmapOverlay()
            };

            // Subscribe to beatpack selection changes
            beatpackManager.CurrentBeatpack.BindValueChanged(e =>
            {
                beatmapDashboardContainer.UpdateBeatpack(e.NewValue);
            }, true);
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            // Fade in animation
            this.FadeInFromZero(300, Easing.OutQuint);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // Save current state before exiting
            if (beatpackManager.CurrentBeatpack.Value != null)
            {
                beatpackManager.SaveProject(beatpackManager.CurrentBeatpack.Value);
            }

            this.FadeOut(300, Easing.OutQuint);
            return base.OnExiting(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Escape)
            {
                this.Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }

        private void openBeatmapEditor(LocalBeatmap beatmap)
        {
            var beatpack = beatpackManager.CurrentBeatpack.Value;
            if (beatpack == null) return;

            // Navigate to appropriate editor based on gamemode
            if (beatmap.Gamemode == "TypeBeat" || string.IsNullOrEmpty(beatmap.Gamemode))
            {
                // Open TypeBeat editor
                this.Push(new TypeBeatEditorScreen(beatpack, beatmap));
            }
            else if (beatmap.Gamemode == "TypeNote")
            {
                // TODO: Open TypeNote editor in future patches
                osu.Framework.Logging.Logger.Log("[EditDashboard] TypeNote editor not implemented yet", 
                    osu.Framework.Logging.LoggingTarget.Runtime, 
                    osu.Framework.Logging.LogLevel.Important);
            }
        }
    }
}

