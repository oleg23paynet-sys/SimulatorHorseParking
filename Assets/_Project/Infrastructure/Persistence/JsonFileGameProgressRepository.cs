#nullable enable

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using HorseParking.Application.Progress;

namespace HorseParking.Infrastructure.Persistence
{
    /// <summary>Single-slot, replace-on-success JSON storage outside the Unity project.</summary>
    public sealed class JsonFileGameProgressRepository : IGameProgressRepository
    {
        private readonly string saveFilePath;
        private readonly DataContractJsonSerializer serializer =
            new(typeof(GameProgressData));

        public JsonFileGameProgressRepository(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
                throw new ArgumentException("Save file path is required.", nameof(saveFilePath));
            this.saveFilePath = Path.GetFullPath(saveFilePath);
        }

        public bool Exists => File.Exists(saveFilePath);

        public void Save(GameProgressData progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            var directory = Path.GetDirectoryName(saveFilePath)
                ?? throw new InvalidOperationException("Save path has no parent directory.");
            Directory.CreateDirectory(directory);

            var temporaryPath = saveFilePath + ".tmp";
            var backupPath = saveFilePath + ".bak";
            try
            {
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None))
                {
                    serializer.WriteObject(stream, progress);
                    stream.Flush(true);
                }

                if (File.Exists(saveFilePath))
                {
                    File.Replace(temporaryPath, saveFilePath, backupPath);
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                }
                else
                {
                    File.Move(temporaryPath, saveFilePath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        public bool TryLoad(out GameProgressData? progress)
        {
            progress = null;
            if (!Exists) return false;
            using var stream = new FileStream(
                saveFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            progress = serializer.ReadObject(stream) as GameProgressData;
            return progress != null;
        }
    }
}
