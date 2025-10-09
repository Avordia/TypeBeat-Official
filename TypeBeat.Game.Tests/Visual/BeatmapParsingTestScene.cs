using System.IO;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.fileHandling;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class BeatmapParsingTestScene : TypeBeatTestScene
    {
        public BeatmapParsingTestScene()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Padding = new MarginPadding(10)
            };
            Add(flow);

            AddStep("Parse and Display .tbmd", () =>
            {
                flow.Clear(); 

                string filePath = @"C:\Users\ACER\Desktop\MyFirstSong.tbmd";

                if (!File.Exists(filePath))
                {
                    flow.Add(new SpriteText { Text = "ERROR: File not found! Check path.", Colour = Colour4.Red });
                    return;
                }

                Beatmap beatmap = BeatmapParser.ParseTbmd(filePath);

                if (beatmap == null)
                {
                    flow.Add(new SpriteText { Text = "ERROR: Beatmap failed to parse.", Colour = Colour4.Red });
                    return;
                }

                flow.Add(new SpriteText { Text = "Beatmap Metadata:", Font = FontUsage.Default.With(size: 24) });
                flow.Add(new SpriteText { Text = $"  Title: {beatmap.Title}" });
                flow.Add(new SpriteText { Text = $"  Artist: {beatmap.Artist}" });
                flow.Add(new SpriteText { Text = $"  BPM: {beatmap.BPM}" });
                flow.Add(new SpriteText { Text = $"  Creators: {string.Join(", ", beatmap.Creators)}" });
                flow.Add(new SpriteText { Text = $"  Source: {beatmap.Source}" });
                flow.Add(new SpriteText { Text = $"  Tags: {string.Join(", ", beatmap.Tags)}" });
                flow.Add(new SpriteText { Text = $"  Preview Time: {beatmap.PreviewTime}ms" });
                flow.Add(new SpriteText { Text = $"  Difficulty: {beatmap.DifficultyName}" });
                flow.Add(new SpriteText { Text = $"  Background: {beatmap.BackgroundImage}" });
                flow.Add(new SpriteText { Text = $"  Video: {beatmap.Video ?? "None"}" });

                flow.Add(new SpriteText { Text = "Map Data:", Font = FontUsage.Default.With(size: 24) });

                for (int i = 0; i < beatmap.MapData.Count; i++)
                {
                    var segment = beatmap.MapData[i];
                    flow.Add(new SpriteText { Text = $"  Segment {i + 1}:", Font = FontUsage.Default.With(weight: "Bold") });

                    foreach (var note in segment.Notes)
                    {
                        flow.Add(new SpriteText
                        {
                            Text = $"    Note: '{note.Character}'  (Start: {note.StartTime}ms, End: {note.EndTime}ms)",
                            Margin = new MarginPadding { Left = 20 }
                        });
                    }
                }
            });
        }
    }
}