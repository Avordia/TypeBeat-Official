using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using osu.Framework.IO.Stores;

namespace TypeBeat.Game.Filehandling
{
    public class ZipArchiveResourceStore : IResourceStore<byte[]>, IDisposable
    {
        private readonly ZipArchive archive;

        public ZipArchiveResourceStore(Stream stream)
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        }

        public byte[] Get(string name)
        {
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry == null) return null;
            using (var entryStream = entry.Open())
            using (var ms = new MemoryStream())
            {
                entryStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Get(name));
        }

        public IEnumerable<string> GetAvailableResources()
        {
            return archive.Entries.Select(e => e.FullName);
        }

        public Stream GetStream(string name)
        {
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry == null) return null;

            // Use a threshold (e.g., 10MB) to decide when to extract to disk
            const long large_file_threshold = 10 * 1024 * 1024;

            if (entry.Length > large_file_threshold)
            {
                string tempPath = Path.GetTempFileName();
                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenWrite(tempPath))
                    entryStream.CopyTo(fileStream);

                return new TempFileStream(tempPath);
            }
            else
            {
                // For small files, use MemoryStream
                var ms = new MemoryStream();
                using (var entryStream = entry.Open())
                    entryStream.CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
        }

        public bool Exists(string name)
        {
            return archive.Entries.Any(e => e.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            archive.Dispose();
        }
    }

    class TempFileStream : FileStream
    {
        private readonly string tempPath;
        public TempFileStream(string path)
            : base(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            tempPath = path;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { File.Delete(tempPath); } catch { }
        }
    }
}
