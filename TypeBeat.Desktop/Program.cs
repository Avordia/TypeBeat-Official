using osu.Framework.Platform;
using osu.Framework;
using TypeBeat.Game;

namespace TypeBeat.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"TypeBeat"))
            using (osu.Framework.Game game = new TypeBeatGame())
                host.Run(game);
        }
    }
}
