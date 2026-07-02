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
        public double playSeconds;
        public int clickPowerLevel;
        public int missionIndex;
        public long lastSeenUnixUtc;
        public bool highQualityMode;
        public bool soundOff;
        // Volúmenes guardados invertidos: un save viejo sin el campo (0) => volumen 1.
        public float musicVolMinus;
        public float sfxVolMinus;
        public bool vibrateOn;
        public string activeBuffId = "";
        public double buffSecondsLeft;
        public long goldenTaps;

        public List<string> elementIds = new List<string>();
        public List<double> elementAmounts = new List<double>();
        public List<string> discovered = new List<string>();
        public List<string> generatorIds = new List<string>();
        public List<int> generatorCounts = new List<int>();
        public List<string> achievements = new List<string>();
        public List<string> upgrades = new List<string>();

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
                playSeconds = s.PlaySeconds,
                clickPowerLevel = s.ClickPowerLevel,
                missionIndex = s.MissionIndex,
                lastSeenUnixUtc = s.LastSeenUnixUtc,
                highQualityMode = s.HighQualityMode,
                soundOff = s.SoundOff,
                musicVolMinus = 1f - s.MusicVolume,
                sfxVolMinus = 1f - s.SfxVolume,
                vibrateOn = s.VibrateOn,
                activeBuffId = s.ActiveBuffId,
                buffSecondsLeft = s.BuffSecondsLeft,
                goldenTaps = s.GoldenTaps,
            };
            foreach (var kv in s.Balances) { d.elementIds.Add(kv.Key.ToString()); d.elementAmounts.Add(kv.Value); }
            foreach (var e in s.Discovered) d.discovered.Add(e.ToString());
            foreach (var kv in s.GeneratorsOwned) { d.generatorIds.Add(kv.Key); d.generatorCounts.Add(kv.Value); }
            foreach (var a in s.AchievementsUnlocked) d.achievements.Add(a);
            foreach (var u in s.UpgradesOwned) d.upgrades.Add(u);
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
                PlaySeconds = playSeconds,
                ClickPowerLevel = clickPowerLevel,
                MissionIndex = missionIndex,
                LastSeenUnixUtc = lastSeenUnixUtc,
                HighQualityMode = highQualityMode,
                SoundOff = soundOff,
                MusicVolume = Mathf.Clamp01(1f - musicVolMinus),
                SfxVolume = Mathf.Clamp01(1f - sfxVolMinus),
                VibrateOn = vibrateOn,
                ActiveBuffId = activeBuffId ?? "",
                BuffSecondsLeft = buffSecondsLeft,
                GoldenTaps = goldenTaps,
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
            foreach (var u in upgrades) s.UpgradesOwned.Add(u);
            return s;
        }
    }

    public static class SaveSystem
    {
        const string ExportPrefix = "ATH1.";

        /// Exporta el estado como texto portable (para copia de seguridad por portapapeles).
        public static string Export(GameState s) =>
            ExportPrefix + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(SaveDto.From(s))));

        /// Importa un texto exportado. Devuelve null si es inválido.
        public static GameState TryImport(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return null;
                text = text.Trim();
                if (!text.StartsWith(ExportPrefix)) return null;
                string json = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(text.Substring(ExportPrefix.Length)));
                var dto = JsonUtility.FromJson<SaveDto>(json);
                return dto?.ToState();
            }
            catch { return null; }
        }

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
