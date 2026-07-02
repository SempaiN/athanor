using System;
using UnityEngine;
using Athanor.Domain;
using Athanor.Infra;

namespace Athanor.Game
{
    /// Dueño del estado: carga, autosave y API de acciones para la UI.
    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        public GameState State { get; private set; }

        /// Bonus de logros (Etapa 5); por ahora 0.
        public double AchievementBonus => 0;

        ISaveBackend backend;
        float saveTimer;
        const float AutosaveSeconds = 30f;

        public event Action StateChanged;

        void Awake()
        {
            Instance = this;
            backend = new JsonFileSaveBackend();
            State = SaveSystem.Load(backend);
            Application.targetFrameRate = State.HighQualityMode ? 60 : 30;
        }

        void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= AutosaveSeconds)
            {
                saveTimer = 0;
                SaveNow();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        public void SaveNow() => SaveSystem.Save(backend, State);

        // ---- Acciones ----

        /// Toque al matraz. Devuelve cuánto dio de cada elemento base (para el feedback).
        public double ClickFlask()
        {
            double yield = GameRules.ClickYield(State, AchievementBonus);
            GameRules.ApplyClick(State, AchievementBonus);
            StateChanged?.Invoke();
            return yield;
        }

        /// Transmuta (vende) todos los elementos por Esencia. Devuelve lo ganado.
        public double TransmuteAll()
        {
            double gained = 0;
            foreach (var def in ElementCatalog.Elements)
                gained += GameRules.Transmute(State, def.Id, State.BalanceOf(def.Id));
            if (gained > 0) StateChanged?.Invoke();
            return gained;
        }

        // ---- Mejora de click (coste 50 × 4^nivel) ----

        public double ClickUpgradeCost => 50 * Math.Pow(4, State.ClickPowerLevel);

        public bool CanBuyClickUpgrade => State.Essence >= ClickUpgradeCost;

        public bool BuyClickUpgrade()
        {
            if (!CanBuyClickUpgrade) return false;
            State.Essence -= ClickUpgradeCost;
            State.ClickPowerLevel++;
            StateChanged?.Invoke();
            return true;
        }
    }
}
