using System;
using System.Linq;
using UnityEngine;
using Athanor.Domain;
using Athanor.Infra;

namespace Athanor.Game
{
    /// Dueño del estado: carga, autosave, tick idle y API de acciones para la UI.
    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        public GameState State { get; private set; }

        /// Esencia otorgada por progreso offline en este arranque (0 si nada).
        public double OfflineGain { get; private set; }

        /// Bonus de logros, cacheado (se recalcula al desbloquear).
        public double AchievementBonus { get; private set; }

        ISaveBackend backend;
        float saveTimer;
        float uiTimer;
        float achievementTimer;
        double transmuterAcc;
        const float AutosaveSeconds = 30f;
        const float UiRefreshSeconds = 0.25f;
        const float AchievementCheckSeconds = 0.5f;

        public event Action StateChanged;
        /// Se dispara al descubrir un elemento nuevo (para toasts/refresco de paneles).
        public event Action<ElementId> ElementDiscovered;
        public event Action<AchievementDef> AchievementUnlocked;

        void Awake()
        {
            Instance = this;
            backend = new JsonFileSaveBackend();
            State = SaveSystem.Load(backend);
            AchievementBonus = AchievementCatalog.TotalBonus(State);
            Application.targetFrameRate = 60;

            ApplyOfflineProgress();
        }

        void ApplyOfflineProgress()
        {
            if (State.LastSeenUnixUtc <= 0) return;
            double away = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - State.LastSeenUnixUtc;
            if (away < 60) return; // menos de 1 min fuera: nada
            OfflineGain = GameRules.OfflineEssence(EssencePerSecond(), away);
            if (OfflineGain > 0)
            {
                State.Essence += OfflineGain;
                State.LifetimeEssence += OfflineGain;
            }
        }

        void Update()
        {
            double dt = Time.deltaTime;
            bool producing = TickGenerators(dt);

            uiTimer += Time.deltaTime;
            if (uiTimer >= UiRefreshSeconds)
            {
                uiTimer = 0;
                if (producing) StateChanged?.Invoke();
            }

            achievementTimer += Time.deltaTime;
            if (achievementTimer >= AchievementCheckSeconds)
            {
                achievementTimer = 0;
                CheckAchievements();
            }

            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= AutosaveSeconds)
            {
                saveTimer = 0;
                SaveNow();
            }
        }

        void CheckAchievements()
        {
            var news = AchievementCatalog.CheckUnlocks(State);
            if (news.Count == 0) return;
            AchievementBonus = AchievementCatalog.TotalBonus(State);
            foreach (var a in news)
            {
                AudioManager.Instance?.Achievement();
                AchievementUnlocked?.Invoke(a);
            }
            StateChanged?.Invoke();
        }

        bool TickGenerators(double dt)
        {
            bool any = State.GeneratorsOwned.Values.Any(n => n > 0);
            if (!any) return false;

            GeneratorCatalog.Tick(State, dt, AchievementBonus);
            TickTransmuter(dt);
            return true;
        }

        void TickTransmuter(double dt)
        {
            int owned = State.GeneratorsOwned.TryGetValue("transmutador", out var n) ? n : 0;
            if (owned == 0) return;

            transmuterAcc += dt * owned;
            int runs = (int)transmuterAcc;
            if (runs <= 0) return;
            transmuterAcc -= runs;

            for (int i = 0; i < runs; i++)
            {
                var recipe = BestKnownRecipe();
                if (recipe == null) break;
                GameRules.Combine(State, recipe);
            }
        }

        /// La receta descubierta de mayor tier que se puede ejecutar ahora.
        Recipe BestKnownRecipe() =>
            ElementCatalog.Recipes
                .Where(r => State.Discovered.Contains(r.Output) && GameRules.CanCombine(State, r))
                .OrderByDescending(r => ElementCatalog.Get(r.Output).Tier)
                .FirstOrDefault();

        /// Popup de progreso al VOLVER de segundo plano (la app no siempre se reinicia).
        public event Action<double> OfflineGranted;

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveNow();
                return;
            }
            // Volviendo de background: otorgar lo generado mientras tanto
            if (State.LastSeenUnixUtc <= 0) return;
            double away = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - State.LastSeenUnixUtc;
            if (away < 60) return;
            double gain = GameRules.OfflineEssence(EssencePerSecond(), away);
            State.LastSeenUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (gain <= 0) return;
            State.Essence += gain;
            State.LifetimeEssence += gain;
            StateChanged?.Invoke();
            OfflineGranted?.Invoke(gain);
        }

        void OnApplicationQuit() => SaveNow();

        public void SaveNow() => SaveSystem.Save(backend, State);

        // ---- Click ----

        /// Toque al matraz. Devuelve cuánto dio de cada elemento base (para el feedback).
        public double ClickFlask()
        {
            double yield = GameRules.ClickYield(State, AchievementBonus);
            GameRules.ApplyClick(State, AchievementBonus);
            AudioManager.Instance?.Click();
            StateChanged?.Invoke();
            return yield;
        }

        // ---- Transmutar ----

        /// Vende todos los elementos BÁSICOS (tier 0). Los superiores se venden
        /// individualmente en el panel de elementos (hacen falta para combinar).
        public double TransmuteBasics()
        {
            double gained = 0;
            foreach (var def in ElementCatalog.Elements.Where(e => e.Tier == 0))
                gained += GameRules.Transmute(State, def.Id, State.BalanceOf(def.Id));
            if (gained > 0) StateChanged?.Invoke();
            return gained;
        }

        public double TransmuteElement(ElementId id, double units)
        {
            double gained = GameRules.Transmute(State, id, units);
            if (gained > 0) StateChanged?.Invoke();
            return gained;
        }

        // ---- Combinar ----

        public bool Combine(Recipe recipe)
        {
            bool wasNew = GameRules.Combine(State, recipe) && true;
            if (wasNew)
            {
                AudioManager.Instance?.Discover();
                ElementDiscovered?.Invoke(recipe.Output);
            }
            StateChanged?.Invoke();
            return wasNew;
        }

        // ---- Generadores ----

        public int GeneratorOwned(string id) =>
            State.GeneratorsOwned.TryGetValue(id, out var n) ? n : 0;

        public double GeneratorCost(GeneratorDef g) =>
            GameRules.GeneratorCost(g.BaseCost, GeneratorOwned(g.Id));

        public bool BuyGenerator(GeneratorDef g) => BuyGenerator(g, 1);

        public bool BuyGenerator(GeneratorDef g, int count)
        {
            if (count <= 0) return false;
            double cost = GameRules.BulkCost(g.BaseCost, GeneratorOwned(g.Id), count);
            if (State.Essence < cost) return false;
            State.Essence -= cost;
            State.GeneratorsOwned[g.Id] = GeneratorOwned(g.Id) + count;
            AudioManager.Instance?.Buy();
            StateChanged?.Invoke();
            return true;
        }

        /// Esencia/segundo estimada si se vendiera todo lo producido (para HUD y offline).
        public double EssencePerSecond()
        {
            double mult = State.GlobalMultiplier(AchievementBonus);
            double sum = 0;
            foreach (var g in GeneratorCatalog.Generators)
            {
                int owned = GeneratorOwned(g.Id);
                if (owned == 0 || g.Produces.Length == 0) continue;
                double unitsPerElement = g.BaseProd * owned * mult / g.Produces.Length;
                foreach (var el in g.Produces)
                    sum += unitsPerElement * ElementCatalog.Get(el).EssenceValue;
            }
            return sum;
        }

        // ---- Prestigio: La Gran Obra ----

        public event Action Prestiged;

        public bool CanPrestige => GameRules.CanPrestige(State) && GameRules.PrestigeGain(State) >= 1;

        public double PrestigeGain => GameRules.PrestigeGain(State);

        public bool DoPrestige()
        {
            if (!CanPrestige) return false;
            GameRules.DoPrestige(State);
            transmuterAcc = 0;
            SaveNow();
            AudioManager.Instance?.Prestige();
            Prestiged?.Invoke();
            StateChanged?.Invoke();
            return true;
        }

        // ---- Ajustes ----

        public void ToggleSound()
        {
            State.SoundOff = !State.SoundOff;
            AudioManager.Instance?.ApplySoundSetting();
            StateChanged?.Invoke();
        }

        public void ToggleQuality()
        {
            State.HighQualityMode = !State.HighQualityMode;
            StateChanged?.Invoke();
        }

        /// Borra el progreso por completo (con confirmación en la UI).
        public void ResetSave()
        {
            State = new GameState();
            AchievementBonus = 0;
            transmuterAcc = 0;
            OfflineGain = 0;
            SaveNow();
            StateChanged?.Invoke();
        }

        // ---- Mejora de click (coste 50 × 4^nivel) ----

        public double ClickUpgradeCost => 50 * Math.Pow(4, State.ClickPowerLevel);

        public bool CanBuyClickUpgrade => State.Essence >= ClickUpgradeCost;

        public bool BuyClickUpgrade()
        {
            if (!CanBuyClickUpgrade) return false;
            State.Essence -= ClickUpgradeCost;
            State.ClickPowerLevel++;
            AudioManager.Instance?.Buy();
            StateChanged?.Invoke();
            return true;
        }
    }
}
