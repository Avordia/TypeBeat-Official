using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using TypeBeat.Game.Online;
using TypeBeat.Resources;

namespace TypeBeat.Game
{
    public partial class TypeBeatGame : TypeBeatGameBase
    {
        private ScreenStack screenStack;
        private Storage gameStorage;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            
            // Register authentication service for dependency injection
            dependencies.Cache(new AuthenticationService());
            
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            var dllResourceStore = new DllResourceStore(typeof(TypeBeatResources).Assembly);
            var fontTextureStore = new TextureLoaderStore(new NamespacedResourceStore<byte[]>(dllResourceStore, "Fonts"));

            var fontStore = new FontStore(host.Renderer, fontTextureStore, 96);

            Fonts.AddStore(fontStore);

            gameStorage = host.Storage;
            Child = screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };
        }

        protected override async void LoadComplete()
        {
            base.LoadComplete();
            
            // Initialize backend client
            try
            {
                await BackendClient.InitializeAsync();
            }
            catch (System.Exception ex)
            {
                Logger.Log($"Failed to initialize backend client: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
            
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