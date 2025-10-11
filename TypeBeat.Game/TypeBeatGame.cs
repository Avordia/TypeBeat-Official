using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using TypeBeat.Resources;

namespace TypeBeat.Game
{
    public partial class TypeBeatGame : TypeBeatGameBase
    {
        private ScreenStack screenStack;
        private Storage gameStorage;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            var dllResourceStore = new DllResourceStore(typeof(TypeBeatResources).Assembly);
            var fontTextureStore = new TextureLoaderStore(new NamespacedResourceStore<byte[]>(dllResourceStore, "Fonts"));

            var fontStore = new FontStore(host.Renderer, fontTextureStore, 20);

            Fonts.AddStore(fontStore);

            gameStorage = host.Storage;
            Child = screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            performFirstRunImport();
            screenStack.Push(new MainScreen());
        }

        private void performFirstRunImport()
        {
            Storage songsStorage = gameStorage.GetStorageForDirectory("Songs");
            const string default_beatmap = "DreamLantern.tbbp";

            if (songsStorage.Exists(default_beatmap)) return;

            using (var stream = Resources.GetStream("initializer/DreamLantern.tbbp"))
            {
                if (stream == null) return;

                using (var writeStream = songsStorage.GetStream(default_beatmap, FileAccess.Write))
                {
                    stream.CopyTo(writeStream);
                }
            }
        }
    }
}