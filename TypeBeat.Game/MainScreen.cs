using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.fileHandling;

namespace TypeBeat.Game
{
    public partial class MainScreen : Screen
    {
        private Track track;

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio)
        {
            Storage songsStorage = host.Storage.GetStorageForDirectory("Songs");
            var beatpackFiles = songsStorage.GetFiles(".", "*.tbbp");

            if (beatpackFiles.Any())
            {
                var firstBeatpackFile = beatpackFiles.First();
                
                // This storage points to the folder containing the beatmap (e.g., /Songs/DreamLantern/)
                var beatmapAssetStorage = songsStorage.GetStorageForDirectory(Path.GetDirectoryName(firstBeatpackFile));
                
                var currentBeatpack = BeatmapParser.ParseBeatpack(songsStorage.GetFullPath(firstBeatpackFile));

                Drawable background = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black };

                if (currentBeatpack != null && currentBeatpack.Beatmap != null)
                {
                    // --- CORRECTED MUSIC LOADING ---
                    // We wrap our storage in a StorageBackedResourceStore, which the TrackStore can use.
                    var trackStore = audio.GetTrackStore(new StorageBackedResourceStore(beatmapAssetStorage));
                    if (!string.IsNullOrEmpty(currentBeatpack.MusicPath))
                    {
                        track = trackStore.Get(currentBeatpack.MusicPath);
                        if (track != null)
                            track.Looping = true;
                    }

                    // --- CORRECTED TEXTURE/VIDEO LOADING ---
                    // We wrap our storage in a TextureLoaderStore for the TextureStore.
                    var textureStore = new TextureStore(host.Renderer, new TextureLoaderStore(new StorageBackedResourceStore(beatmapAssetStorage)));

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

                InternalChildren = new Drawable[] { background };
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