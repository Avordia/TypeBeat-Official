using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Filehandling;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class BeatpackImportTestScene : TypeBeatTestScene
    {
        private Storage gameStorage;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            gameStorage = host.Storage;
        }

        public BeatpackImportTestScene()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Padding = new MarginPadding(10),
            };
            Add(flow);

            AddStep("Import beatpack using importer", () =>
            {
                flow.Clear();

                // Provide a direct path to a .tbbp file for this simple visual test
                string beatpackFilePath = @"C:\Users\ACER\Desktop\beatmap\example.tbbp";

                if (!System.IO.File.Exists(beatpackFilePath))
                {
                    flow.Add(new SpriteText { Text = "ERROR: .tbbp file not found! Check path.", Colour = Colour4.Red });
                    return;
                }

                var importer = new BeatpackImporter();
                var beatpack = importer.Import(beatpackFilePath);

                if (beatpack != null)
                {
                    flow.Add(new SpriteText { Text = $"Successfully imported beatpack: {System.IO.Path.GetFileName(beatpackFilePath)}", Colour = Colour4.Green });
                }
                else
                {
                    flow.Add(new SpriteText { Text = "Import returned null beatpack.", Colour = Colour4.Red });
                }
            });
        }
    }
}