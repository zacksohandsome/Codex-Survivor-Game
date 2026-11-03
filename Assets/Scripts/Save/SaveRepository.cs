using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Survivor.Save
{
    public interface ISaveStorage
    {
        bool Exists(string path);
        string Read(string path);
        void WriteDurable(string path, string contents);
        void Copy(string source, string destination);
        void Replace(string temporary, string destination);
    }

    public sealed class FileSaveStorage : ISaveStorage
    {
        public bool Exists(string path) => File.Exists(path);
        public string Read(string path) => File.ReadAllText(path, Encoding.UTF8);
        public void WriteDurable(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(contents);
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
        }
        public void Copy(string source, string destination) => File.Copy(source, destination, true);
        public void Replace(string temporary, string destination)
        {
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
    }

    // The digest detects accidental corruption; this is not an anti-cheat or authentication format.
    public sealed class SaveRepository
    {
        [Serializable] private sealed class Envelope { public int format = 1; public string payload; public string digest; }
        private readonly ISaveStorage storage;
        public string Path { get; }
        public bool IsReadOnly { get; private set; }
        public string Notice { get; private set; }
        public SaveRepository(string directory, ISaveStorage storage = null)
        {
            Path = System.IO.Path.Combine(directory, "progress.json");
            this.storage = storage ?? new FileSaveStorage();
        }
        public SaveData Load()
        {
            Notice = null; IsReadOnly = false;
            try
            {
                if (storage.Exists(Path) && TryDecode(storage.Read(Path), out var current)) return current;
                bool mainInvalid = storage.Exists(Path);
                if (mainInvalid) storage.Copy(Path, Path + ".corrupt." + Guid.NewGuid().ToString("N"));
                for (int i = 1; i <= 3; i++)
                {
                    string backup = Path + ".bak" + i;
                    if (storage.Exists(backup) && TryDecode(storage.Read(backup), out var recovered))
                    { Notice = "save.recovered"; return recovered; }
                }
                // Never overwrite an unsupported or damaged save with defaults.
                for (int i = 1; i <= 3; i++) mainInvalid |= storage.Exists(Path + ".bak" + i);
                if (mainInvalid) { IsReadOnly = true; Notice = "save.unreadable"; }
            }
            catch (Exception ex) when (IsStorageFailure(ex)) { IsReadOnly = true; Notice = "save.unreadable"; }
            return new SaveData();
        }
        public bool TrySave(SaveData data)
        {
            if (IsReadOnly || !Valid(data)) return false;
            try
            {
                string encoded = Encode(data), temporary = Path + ".tmp";
                storage.WriteDurable(temporary, encoded);
                if (!TryDecode(storage.Read(temporary), out _)) return false;
                // Copy only validated snapshots. Primary remains intact until the final atomic replace.
                if (storage.Exists(Path) && TryDecode(storage.Read(Path), out _))
                {
                    if (storage.Exists(Path + ".bak2") && TryDecode(storage.Read(Path + ".bak2"), out _)) storage.Copy(Path + ".bak2", Path + ".bak3");
                    if (storage.Exists(Path + ".bak1") && TryDecode(storage.Read(Path + ".bak1"), out _)) storage.Copy(Path + ".bak1", Path + ".bak2");
                    storage.Copy(Path, Path + ".bak1");
                }
                storage.Replace(temporary, Path); return true;
            }
            catch (Exception ex) when (IsStorageFailure(ex)) { return false; }
        }
        private static bool IsStorageFailure(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException;
        public static SaveData Clone(SaveData data) => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
        public static string Encode(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            return JsonUtility.ToJson(new Envelope { payload = payload, digest = Digest(payload) });
        }
        public static bool TryDecode(string json, out SaveData data)
        {
            data = null;
            try
            {
                var envelope = JsonUtility.FromJson<Envelope>(json);
                if (envelope == null || envelope.format != 1 || string.IsNullOrEmpty(envelope.payload) || envelope.digest != Digest(envelope.payload)) return false;
                var candidate = JsonUtility.FromJson<SaveData>(envelope.payload);
                // Explicit v1 -> v2 migration; the verified source is preserved until an actual transaction commits.
                if (candidate != null && candidate.version == 1)
                {
                    candidate.version = 2; candidate.musicVolume = 0.6f; candidate.sfxVolume = 0.7f;
                    candidate.displayWidth = 1920; candidate.displayHeight = 1080; candidate.fullscreen = false;
                }
                if (!Valid(candidate)) return false;
                data = candidate; return true;
            }
            catch (ArgumentException) { return false; }
        }
        private static string Digest(string payload)
        {
            using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
        public static bool Valid(SaveData data)
        {
            if (data == null || data.version != 2 || data.currency < 0 || data.totalRuns < 0 || data.victories < 0 || data.victories > data.totalRuns || data.totalKills < 0 ||
                double.IsNaN(data.totalSurvivalSeconds) || double.IsInfinity(data.totalSurvivalSeconds) || data.totalSurvivalSeconds < 0 ||
                data.metaUpgradeLevels == null || data.metaUpgradeLevels.Length != 5 || data.settledRunIds == null ||
                !ValidVolume(data.musicVolume) || !ValidVolume(data.sfxVolume) || !Survivor.Settings.DisplayPreview.ValidSize(data.displayWidth,data.displayHeight) ||
                data.language != "" && data.language != "zh-TW" && data.language != "en") return false;
            foreach (int level in data.metaUpgradeLevels) if (level < 0 || level > 5) return false;
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (string id in data.settledRunIds) if (string.IsNullOrEmpty(id) || !ids.Add(id)) return false;
            return true;
        }
        private static bool ValidVolume(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
    }
}
