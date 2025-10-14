using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.ui;
using System.Collections.Generic;
using osu.Framework.Logging;

namespace TypeBeat.Game
{
    public partial class SongSelectionScreen : Screen
    {
        private readonly BeatpackManager beatpackManager;
        private readonly Container backgroundContainer;
        private readonly MainScreen mainScreen;
        private readonly Header header;
        private readonly Footer footer;
        private readonly Container backgroundLayer;

        public SongSelectionScreen(BeatpackManager beatpackManager, Container backgroundContainer, MainScreen mainScreen)
        {
            this.beatpackManager = beatpackManager;
            this.backgroundContainer = backgroundContainer;
            this.mainScreen = mainScreen;

            InternalChildren = new Drawable[]
            {
                backgroundLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Name = "Background Layer"
                },

                header = new Header { Alpha = 0 },
                footer = new Footer { Alpha = 0 },

                new Container
                {
                    Name = "Song List Container",
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Y,
                    Width = 300,
                    Padding = new MarginPadding { Left = 50, Vertical = 20 },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.Black,
                            Alpha = 0.5f
                        },
                        new BasicScrollContainer
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
                                Padding = new MarginPadding { Horizontal = 20, Vertical = 10 },
                                Direction = FillDirection.Vertical
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

            this.FadeInFromZero(150);

            header.Alpha = 0;
            footer.Alpha = 0;
            var songList = InternalChildren.FirstOrDefault(c => c.Name == "Song List Container");
            if (songList != null) songList.Alpha = 0;

            using (BeginDelayedSequence(100))
            {
                header.FadeIn(350, Easing.OutQuint);
                footer.FadeIn(350, Easing.OutQuint);
                songList?.FadeIn(350, Easing.OutQuint);
            }

            dumpChildrenOrder("OnEntering after addBackground");
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            Logger.Log($"[SongSelection] OnExiting called", LoggingTarget.Runtime, LogLevel.Important);
            
            // Remove the background IMMEDIATELY before any animations
            removeBackground();
            
            // Return the background to MainScreen
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
        }

        private void removeBackground()
        {
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
            var scrollContainer = InternalChildren.OfType<Container>()
                                    .FirstOrDefault(x => x.Name == "Song List Container")?
                                    .Children.OfType<BasicScrollContainer>()
                                    .FirstOrDefault();

            if (scrollContainer == null)
                return;

            var songContainer = (FillFlowContainer)scrollContainer.Child;

            foreach (var beatpack in beatpackManager.Beatpacks)
            {
                songContainer.Add(new SongThumbnail(beatpack, textures)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            // Background should already be removed in OnExiting
            // Just ensure it's cleaned up if something went wrong
            if (isDisposing && backgroundContainer.Parent == backgroundLayer)
            {
                removeBackground();
                mainScreen.AddBackgroundContainer(backgroundContainer);
            }
            
            base.Dispose(isDisposing);
        }
    }
}
