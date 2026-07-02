using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Athanor.Domain;

namespace Athanor.Infra
{
    /// Backend de guardado intercambiable (hoy archivo local, mañana nube).
    public interface ISaveBackend
    {
        string Load();          // JSON o null
        void Save(string json);
    }

    public sealed class JsonFileSaveBackend : ISaveBackend
    {
        readonly string path = Path.Combine(Application.persistentDataPath, "save.json");

        public string Load()
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch (Exception e) { Debug.LogError("Load failed: " + e.Message); return null; }
        }

        public void Save(string json)
        {
            try
            {
                // Escritura atómica: temp + replace, para no corromper el save si se mata la app.
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e) { Debug.LogError("Save failed: " + e.Message); }
        }
    }

    /// DTO plano para JsonUtility (no serializa Dictionary/HashSet).
    [Serializable]
    public sealed class SaveDto
    {
        public int saveVersion = 1;
        public double essence;
        public double lifetimeEssence;
        public double quintessence;
        public long totalClicks;
        public int prestigeCount;
        public int clickPowerLevel;
        public long lastSeenUnixUtc;
        public bool highQualityMode;
        public bool soundOff;

        public List<string> elementIds = new List<string>();
        public List<double> elementAmounts = new List<double>();
        public List<string> discovered = new List<string>();
        public List<string> generatorIds = new List<string>();
        public List<int> generatorCounts = new List<int>();
        public List<string> achievements = new List<string>();

        public static SaveDto From(GameState s)
        {
            var d = new SaveDto
            {
                saveVersion = s.SaveVersion,
                essence = s.Essence,
                lifetimeEssence = s.LifetimeEssence,
                quintessence = s.Quintessence,
                totalClicks = s.TotalClicks,
                prestigeCount = s.PrestigeCount,
                clickPowerLevel = s.ClickPowerLevel,
                lastSeenUnixUtc = s.LastSeenUnixUtc,
                highQualityMode = s.HighQualityMode,
                soundOff = s.SoundOff,
            };
            foreach (var kv in s.Balances) { d.elementIds.Add(kv.Key.ToString()); d.elementAmounts.Add(kv.Value); }
            foreach (var e in s.Discovered) d.discovered.Add(e.ToString());
            foreach (var kv in s.GeneratorsOwned) { d.generatorIds.Add(kv.Key); d.generatorCounts.Add(kv.Value); }
            foreach (var a in s.AchievementsUnlocked) d.achievements.Add(a);
            return d;
        }

        public GameState ToState()
        {
            var s = new GameState
            {
                SaveVersion = saveVersion,
                Essence = essence,
                LifetimeEssence = lifetimeEssence,
                Quintessence = quintessence,
                TotalClicks = totalClicks,
                PrestigeCount = prestigeCount,
                ClickPowerLevel = clickPowerLevel,
                LastSeenUnixUtc = lastSeenUnixUtc,
                HighQualityMode = highQualityMode,
                SoundOff = soundOff,
            };
            for (int i = 0; i < elementIds.Count && i < elementAmounts.Count; i++)
                if (Enum.TryParse(elementIds[i], out ElementId id))
                    s.Balances[id] = elementAmounts[i];
            foreach (var name in discovered)
                if (Enum.TryParse(name, out ElementId id))
                    s.Discovered.Add(id);
            for (int i = 0; i < generatorIds.Count && i < generatorCounts.Count; i++)
                s.GeneratorsOwned[generatorIds[i]] = generatorCounts[i];
            foreach (var a in achievements) s.AchievementsUnlocked.Add(a);
            return s;
        }
    }

    public static class SaveSystem
    {
        public static GameState Load(ISaveBackend backend)
        {
            string json = backend.Load();
            if (string.IsNullOrEmpty(json)) return new GameState();
            try { return JsonUtility.FromJson<SaveDto>(json).ToState(); }
            catch (Exception e)
            {
                Debug.LogError("Save corrupto, empezando de cero: " + e.Message);
                return new GameState();
            }
        }

        public static void Save(ISaveBackend backend, GameState s)
        {
            s.LastSeenUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            backend.Save(JsonUtility.ToJson(SaveDto.From(s)));
        }
    }
}
