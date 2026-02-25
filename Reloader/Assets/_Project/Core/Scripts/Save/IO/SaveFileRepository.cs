using System;
using System.IO;
using Newtonsoft.Json;

namespace Reloader.Core.Save.IO
{
    public sealed class SaveFileRepository
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public void WriteEnvelope(string absolutePath, SaveEnvelope envelope)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("Save path is required.", nameof(absolutePath));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var directory = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Could not resolve save directory for path '{absolutePath}'.");
            }

            Directory.CreateDirectory(directory);

            var tempPath = absolutePath + ".tmp";
            var backupPath = absolutePath + ".bak";
            var json = JsonConvert.SerializeObject(envelope, SerializerSettings);

            try
            {
                File.WriteAllText(tempPath, json);

                if (File.Exists(absolutePath))
                {
                    File.Copy(absolutePath, backupPath, true);
                    File.Delete(absolutePath);
                }

                File.Move(tempPath, absolutePath);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                if (!File.Exists(absolutePath) && File.Exists(backupPath))
                {
                    File.Move(backupPath, absolutePath);
                }

                throw;
            }
        }

        public SaveEnvelope ReadEnvelope(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("Save path is required.", nameof(absolutePath));
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Save file does not exist.", absolutePath);
            }

            var json = File.ReadAllText(absolutePath);

            try
            {
                var envelope = JsonConvert.DeserializeObject<SaveEnvelope>(json);
                if (envelope == null)
                {
                    throw new InvalidDataException("Save file JSON deserialized to null envelope.");
                }

                envelope.Modules ??= new System.Collections.Generic.Dictionary<string, ModuleSaveBlock>();
                envelope.FeatureFlags ??= new SaveFeatureFlags();
                return envelope;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Save file contains invalid JSON.", ex);
            }
        }
    }
}
