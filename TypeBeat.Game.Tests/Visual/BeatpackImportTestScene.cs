using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.fileHandling; 
using osuTK.Graphics;

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

            AddStep("Import beatpacks using importer", () =>
            {
                flow.Clear();

                string sourceFolderPath = @"C:\Users\ACER\Desktop\beatmap";

                try
                {
                    int importCount = BeatpackImporter.ImportFromFolder(gameStorage, sourceFolderPath);

                    if (importCount > 0)
                    {
                        flow.Add(new SpriteText { Text = $"Successfully imported {importCount} new beatpack(s)!", Colour = Colour4.Green });
                    }
                    else
                    {
                        flow.Add(new SpriteText { Text = "No new beatpacks were found to import.", Colour = Colour4.Yellow });
                    }

                    flow.Add(new SpriteText { Text = $"\nBeatpacks are located in: {gameStorage.GetStorageForDirectory("Songs").GetFullPath(".")}" });
                }
                catch (DirectoryNotFoundException ex)
                {
                    flow.Add(new SpriteText { Text = $"ERROR: {ex.Message}", Colour = Colour4.Red });
                }
            });
        }
    }
}