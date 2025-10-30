using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using TypeBeat.Game.Online;
using TypeBeat.Game.Editor;
using TypeBeat.Game.Updates;
using TypeBeat.Game.Overlays;
using TypeBeat.Resources;

namespace TypeBeat.Game
{
    public partial class TypeBeatGame : TypeBeatGameBase
    {
        private ScreenStack screenStack;
        private Storage gameStorage;
        private LocalBeatpackManager beatpackManager;
        private UpdateManager updateManager;
        private UpdateNotificationOverlay updateNotification;
        private TypeBeat.Game.Ui.LoginOverlay loginOverlay;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            
            // Register authentication service for dependency injection
            var authService = new AuthenticationService();
            dependencies.Cache(authService);
            
            // Register score submission service for dependency injection
            dependencies.Cache(new ScoreSubmissionService());
            
            // Register login overlay for dependency injection
            dependencies.Cache(loginOverlay = new TypeBeat.Game.Ui.LoginOverlay(authService));
            
            // Register local beatpack manager for dependency injection
            dependencies.Cache(beatpackManager = new LocalBeatpackManager());
            
            // Register update manager for dependency injection
            dependencies.Cache(updateManager = new UpdateManager());
            
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
            
            Children = new Drawable[]
            {
                beatpackManager,
                screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both },
                updateNotification = new UpdateNotificationOverlay(),
                loginOverlay // Add login overlay as global overlay
            };
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
            
            // Check for updates after initialization
            checkForUpdates();
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

            Storage editorStorage = gameStorage.GetStorageForDirectory("EditorProjects");
            Logger.Log($"EditorProjects directory initialized at: {editorStorage.GetFullPath(string.Empty)}", LoggingTarget.Runtime, LogLevel.Debug);
        }
        
        private async void checkForUpdates()
        {
            try
            {
                Logger.Log("Checking for updates...", LoggingTarget.Runtime, LogLevel.Debug);
                var updateInfo = await updateManager.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    Schedule(() => updateNotification.ShowUpdateNotification());
                }
            }
            catch (System.Exception ex)
            {
                Logger.Log($"Update check failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }
    }
}