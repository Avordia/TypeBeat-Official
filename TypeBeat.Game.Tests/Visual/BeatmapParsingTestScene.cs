using System.IO;
using osu.Framework.Testing;
using TypeBeat.Game.fileHandling; 

namespace TypeBeat.Game.Tests.Visual
{
    public partial class BeatmapParsingTestScene : TypeBeatTestScene
    {
        public BeatmapParsingTestScene()
        {
            AddStep("Parse beatmap with parser", () =>
            {
                //TEMPORARYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY
                string filePath = @"C:\Users\ACER\Desktop\MyFirstSong.tbmd"; // Change this to your actual file path

                if (!File.Exists(filePath))
                {
                    AddLabel("File not found! Check the path in the test scene.");
                    return;
                }

                Beatmap beatmap = TbmdParser.Parse(filePath);

                AddAssert("Beatmap is not null", () => beatmap != null);
                AddAssert("Title is correct", () => beatmap.Title == "My First Song");
                AddAssert("Has 2 segments in MapData", () => beatmap.MapData.Count == 2);
                AddAssert("First segment has 5 notes", () => beatmap.MapData[0].Notes.Count == 5);
                AddAssert("Last note of first segment is /", () => beatmap.MapData[0].Notes[4].Character == "/");
                
                AddLabel($"Successfully loaded '{beatmap.Title}' using TbmdParser.");
            });
        }
    }
}