using osu.Framework.Allocation;
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
            Resources.AddStore(new DllResourceStore(typeof(TypeBeatResources).Assembly));
            dependencies.CacheAs(new TextureStore(Host.Renderer, new TextureLoaderStore(Resources)));
            Resources.AddStore(new DllResourceStore(TypeBeatResources.ResourceAssembly));
            AddFont(Resources, "Fonts/Kodchasan");
        }
    }
}