namespace TypeBeat.Game.Beatmaps
{
    public class Beatpack
    {
        public string FilePath { get; set; }
        public Beatmap Beatmap { get; set; }
        public string MusicPath { get; set; }
        public string BackgroundImagePath { get; set; }
        public string VideoPath { get; set; }
        public string KeyPressSoundPath { get; set; }
        public string SpacePressSoundPath { get; set; }
    }
}