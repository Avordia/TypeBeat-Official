using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures; // Add this using directive
using osu.Framework.IO.Stores;
using osuTK;
using TypeBeat.Resources;

namespace TypeBeat.Game
{
    public partial class TypeBeatGameBase : osu.Framework.Game
    {                
        private DependencyContainer dependencies;
        protected override Container<Drawable> Content { get; }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        protected TypeBeatGameBase()
        {
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                TargetDrawSize = new Vector2(1366, 768)
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load embedded resources from TypeBeat.Resources.dll
            var resourceStore = new DllResourceStore(typeof(TypeBeatResources).Assembly);
            Resources.AddStore(resourceStore);
            dependencies.CacheAs(new TextureStore(Host.Renderer, new TextureLoaderStore(Resources)));
            Resources.AddStore(new DllResourceStore(TypeBeatResources.ResourceAssembly));
            
            // Note: Audio.Samples automatically uses Resources store
            
            // Add all font families
            // Kodchasan regular and bold variants
            AddFont(Resources, "Fonts/Kodchasan");
            AddFont(Resources, "Fonts/Kodchasan-Bold");
            // Inter font
            AddFont(Resources, "Fonts/Inter");
            AddFont(Resources, "Fonts/Inter-Bold");

            AddFont(Resources, "Fonts/Gunship");
        }
    }
}
