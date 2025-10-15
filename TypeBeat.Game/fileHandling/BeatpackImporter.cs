using System.IO;
using osu.Framework.Platform;

namespace TypeBeat.Game.Filehandling
{
    // The class is now correctly named BeatpackImporter
    public static class BeatpackImporter
    {
        public static int ImportFromFolder(Storage gameStorage, string sourceFolderPath)
        {
            if (!Directory.Exists(sourceFolderPath))
                throw new DirectoryNotFoundException($"Source folder not found: {sourceFolderPath}");

            string[] beatpackFiles = Directory.GetFiles(sourceFolderPath, "*.tbbp", SearchOption.AllDirectories);

            if (beatpackFiles.Length == 0)
                return 0;

            Storage songsStorage = gameStorage.GetStorageForDirectory("Songs");
            int importCount = 0;

            foreach (var sourcePath in beatpackFiles)
            {
                string fileName = Path.GetFileName(sourcePath);
                string destinationPath = songsStorage.GetFullPath(fileName);

                if (!File.Exists(destinationPath))
                {
                    File.Copy(sourcePath, destinationPath);
                    importCount++;
                }
            }

            return importCount;
        }
    }
}