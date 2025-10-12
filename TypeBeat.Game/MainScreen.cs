using System.IO;
using System.Linq;
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

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio)
        {
            this.host = host;
            audioManager = audio;

            InternalChildren = new Drawable[]
            {
                beatpackManager = new BeatpackManager(),
                backgroundContainer = new Container { RelativeSizeAxes = Axes.Both },
                
            new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new CentralLogo
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Margin = new MarginPadding { Bottom = 20 }
                    },
                    songTitleText = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Font = new FontUsage(size: 10),
                        Margin = new MarginPadding { Bottom = 20 }
                    },
                    new MenuPlayer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        OnNext = () => beatpackManager.Next(),
                        OnPrevious = () => beatpackManager.Previous(),
                        OnTogglePlay = () => togglePause(),
                    },
                }
            }
            };

            beatpackManager.CurrentBeatpack.BindValueChanged(beatpackChanged, true);
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
                        Loop = true,
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

            if (background is Video video)
            {
                // As a reminder, direct video pause/play isn't simple.
                // It's tied to the game clock. This can be implemented later.
            }
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            track?.Start();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            track?.Stop();
            return base.OnExiting(e);
        }
    }
}