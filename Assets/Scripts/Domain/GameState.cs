using System;
using System.Collections.Generic;

namespace Athanor.Domain
{
    /// Estado completo del juego. C# puro: sin UnityEngine, serializable a JSON.
    [Serializable]
    public sealed class GameState
    {
        public int SaveVersion = 1;

        public double Essence;
        public double LifetimeEssence;      // histórica, nunca baja (prestigio)
        public double Quintessence;
        public long TotalClicks;
        public int PrestigeCount;

        public Dictionary<ElementId, double> Balances = new Dictionary<ElementId, double>();
        public HashSet<ElementId> Discovered = new HashSet<ElementId>
        {
            ElementId.Tierra, ElementId.Agua, ElementId.Fuego, ElementId.Aire
        };

        public Dictionary<string, int> GeneratorsOwned = new Dictionary<string, int>();
        public HashSet<string> AchievementsUnlocked = new HashSet<string>();

        public int ClickPowerLevel;         // mejoras de click compradas
        public long LastSeenUnixUtc;        // para progreso offline
        public bool HighQualityMode;        // false = Alto Rendimiento (default)

        public double BalanceOf(ElementId id) =>
            Balances.TryGetValue(id, out var v) ? v : 0;

        public void Add(ElementId id, double amount)
        {
            Balances[id] = BalanceOf(id) + amount;
            if (amount > 0 && !Discovered.Contains(id))
                Discovered.Add(id);
        }

        /// Multiplicador global de producción: prestigio + logros.
        public double GlobalMultiplier(double achievementBonus) =>
            (1.0 + 0.10 * Quintessence) * (1.0 + achievementBonus);
    }
}
