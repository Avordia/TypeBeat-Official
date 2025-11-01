using osu.Framework.Platform;
using osu.Framework;
using TypeBeat.Game;
using System;

namespace TypeBeat.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            // Register file associations for .tbbp files on Windows
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    if (!FileAssociations.IsFileAssociationRegistered())
                    {
                        FileAssociations.RegisterFileAssociation();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not register file associations: {ex.Message}");
                }
            }

            using (GameHost host = Host.GetSuitableDesktopHost(@"TypeBeat"))
            using (osu.Framework.Game game = new TypeBeatGame())
                host.Run(game);
        }
    }
}
