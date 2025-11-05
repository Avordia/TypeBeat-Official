using System.IO;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Filehandling;
using osuTK.Graphics;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class BeatpackParsingTestScene : TypeBeatTestScene
    {
        public BeatpackParsingTestScene()
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

            AddStep("Parse and Display .tbbp", () =>
            {
                flow.Clear();
                //TEMPOOOOORARRRYYYY
                string filePath = @"C:\Users\ACER\Desktop\Dream Lantern.tbbp";

                if (!File.Exists(filePath))
                {
                    flow.Add(new SpriteText { Text = "ERROR: .tbbp file not found! Check path.", Colour = Colour4.Red });
                    return;
                }
                
                Beatpack beatpack = BeatmapParser.ParseBeatpack(filePath);

                if (beatpack == null)
                {
                    flow.Add(new SpriteText { Text = "ERROR: Beatpack failed to parse.", Colour = Colour4.Red });
                    return;
                }

                flow.Add(new SpriteText { Text = "Beatpack Loaded Successfully!", Font = FontUsage.Default.With(size: 28) });
                flow.Add(new SpriteText { Text = $"\nMusic File Found: {!string.IsNullOrEmpty(beatpack.MusicPath)}" });
                flow.Add(new SpriteText { Text = $"Background File Found: {!string.IsNullOrEmpty(beatpack.BackgroundImagePath)}" });
                flow.Add(new SpriteText { Text = $"Video File Found: {!string.IsNullOrEmpty(beatpack.VideoPath)}" });

                if (beatpack.Beatmap != null)
                {
                    flow.Add(new SpriteText { Text = "\n--- Contained Beatmap Metadata ---", Font = FontUsage.Default.With(size: 24) });
                    flow.Add(new SpriteText { Text = $"  Title: {beatpack.Beatmap.Title}" });
                    flow.Add(new SpriteText { Text = $"  Artist: {beatpack.Beatmap.Artist}" });
                    flow.Add(new SpriteText { Text = $"  Difficulty: {beatpack.Beatmap.DifficultyName}" });
                    flow.Add(new SpriteText { Text = $"  Creators: {string.Join(", ", beatpack.Beatmap.Creators)}" });
                    flow.Add(new SpriteText { Text = $"  BPM: {beatpack.Beatmap.Bpm}" });
                    flow.Add(new SpriteText { Text = $"  Source: {beatpack.Beatmap.Source}" });
                    flow.Add(new SpriteText { Text = $"  Tags: {string.Join(", ", beatpack.Beatmap.Tags)}" });
                    flow.Add(new SpriteText { Text = $"  Preview Time: {beatpack.Beatmap.PreviewTime}ms" });
                    flow.Add(new SpriteText { Text = $"  Background: {beatpack.Beatmap.BackgroundImage}" });
                    flow.Add(new SpriteText { Text = $"  Video: {beatpack.Beatmap.Video ?? "None"}" });
                    flow.Add(new SpriteText { Text = $"  Map Data Segments: {beatpack.Beatmap.MapData?.Count ?? 0}" });
                }   
            });
        }
    }
}