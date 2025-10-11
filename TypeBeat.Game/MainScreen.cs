using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.fileHandling; // Using your namespace
using TypeBeat.Game.ui;

namespace TypeBeat.Game
{
    public partial class MainScreen : Screen
    {
        private Track track;

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio, TextureStore textures)
        {
            
            Storage songsStorage = host.Storage.GetStorageForDirectory("Songs");
            var beatpackFiles = songsStorage.GetFiles(".", "*.tbbp");

            if (beatpackFiles.Any())
            {
                var firstBeatpackFile = beatpackFiles.First();
                var currentBeatpack = BeatmapParser.ParseBeatpack(songsStorage.GetFullPath(firstBeatpackFile));

                using (var stream = songsStorage.GetStream(firstBeatpackFile, FileAccess.Read, FileMode.Open))
                using (var beatmapAssetStorage = new ZipArchiveResourceStore(stream))
                {
                    Drawable background = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };

                    if (currentBeatpack != null && currentBeatpack.Beatmap != null)
                    {
                        var textureStore = new TextureStore(host.Renderer, new TextureLoaderStore(beatmapAssetStorage));
                        var trackStore = audio.GetTrackStore(beatmapAssetStorage);

                        if (!string.IsNullOrEmpty(currentBeatpack.MusicPath))
                        {
                            track = trackStore.Get(currentBeatpack.MusicPath);
                            if (track != null)
                                track.Looping = true;
                        }

                        if (!string.IsNullOrEmpty(currentBeatpack.VideoPath) && beatmapAssetStorage.Exists(currentBeatpack.VideoPath))
                        {
                            background = new Video(beatmapAssetStorage.GetStream(currentBeatpack.VideoPath))
                            {
                                RelativeSizeAxes = Axes.Both,
                                FillMode = FillMode.Fill,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Loop = true,
                            };
                        }
                        else if (!string.IsNullOrEmpty(currentBeatpack.BackgroundImagePath) && beatmapAssetStorage.Exists(currentBeatpack.BackgroundImagePath))
                        {
                            background = new Sprite
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                FillMode = FillMode.Fill,
                                Texture = textureStore.Get(currentBeatpack.BackgroundImagePath)
                            };
                        }
                    }

                    InternalChildren = new Drawable[]
                    {
                        background,
                        new CentralLogo()
                    };
                }
            }
            else
            {
                InternalChildren = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black },
                    new SpriteText
                    {
                        Text = "No beatmaps found! Please add some to the Songs folder.",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
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