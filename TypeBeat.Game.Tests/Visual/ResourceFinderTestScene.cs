using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Resources;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class ResourceFinderTestScene : TypeBeatTestScene
    {
        public ResourceFinderTestScene()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Padding = new MarginPadding(10),
            };
            Add(flow);

            AddStep("List all embedded resources", () =>
            {
                flow.Clear();
                flow.Add(new SpriteText { Text = "Finding all resources in TypeBeat.Resources.dll:", Font = FontUsage.Default.With(size: 24)});

                var resources = new DllResourceStore(typeof(TypeBeatResources).Assembly);
                var resourceNames = resources.GetAvailableResources();

                if (!resourceNames.Any())
                {
                    flow.Add(new SpriteText { Text = "ERROR: No embedded resources found!"});
                    return;
                }

                foreach (var name in resourceNames)
                {
                    bool isBeatmap = name.EndsWith(".tbbp");
                    
                    flow.Add(new SpriteText
                    {
                        Text = name,
                        Colour = isBeatmap ? Colour4.Yellow : Colour4.White
                    });
                }
            });
        }
    }
}