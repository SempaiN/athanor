using System;
using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public sealed class BuffDef
    {
        public string Id;
        public string Name;        // TODO(loc): tabla al agregar EN
        public double Duration;    // segundos; 0 = efecto instantáneo
        public double ProdMult = 1;
        public double ClickMult = 1;
        public int Weight;         // peso de sorteo
    }

    /// Buffs del Matraz Dorado (inspiración: Golden Cookie de Cookie Clicker).
    public static class BuffCatalog
    {
        public static readonly IReadOnlyList<BuffDef> All = new List<BuffDef>
        {
            new BuffDef { Id = "frenesi", Name = "¡Frenesí!",             Duration = 60, ProdMult = 7, Weight = 45 },
            new BuffDef { Id = "fiebre",  Name = "Fiebre de transmutación", Duration = 15, ClickMult = 7, Weight = 35 },
            new BuffDef { Id = "fortuna", Name = "Fortuna alquímica",     Duration = 0,  Weight = 20 },
        };

        static readonly Dictionary<string, BuffDef> byId = All.ToDictionary(b => b.Id);

        public static BuffDef Get(string id) =>
            string.IsNullOrEmpty(id) ? null : (byId.TryGetValue(id, out var b) ? b : null);

        /// Sorteo ponderado con un valor uniforme [0,1).
        public static BuffDef Roll(double roll01)
        {
            int total = All.Sum(b => b.Weight);
            double target = roll01 * total;
            double acc = 0;
            foreach (var b in All)
            {
                acc += b.Weight;
                if (target < acc) return b;
            }
            return All[All.Count - 1];
        }

        public static BuffDef Active(GameState s)
        {
            if (s.BuffSecondsLeft <= 0) return null;
            return Get(s.ActiveBuffId);
        }

        public static double ProdMult(GameState s) => Active(s)?.ProdMult ?? 1;
        public static double ClickMult(GameState s) => Active(s)?.ClickMult ?? 1;

        /// Aplica un buff. Para "fortuna" (instantáneo) devuelve la esencia otorgada:
        /// 10% de la esencia actual + 30 s de producción.
        public static double Apply(GameState s, BuffDef def, double essencePerSecond)
        {
            if (def.Duration <= 0)
            {
                double gained = s.Essence * 0.10 + essencePerSecond * 30;
                s.Essence += gained;
                s.LifetimeEssence += gained;
                return gained;
            }
            s.ActiveBuffId = def.Id;
            s.BuffSecondsLeft = def.Duration;
            return 0;
        }

        /// Descuenta tiempo del buff. Devuelve true si acaba de expirar.
        public static bool Tick(GameState s, double dt)
        {
            if (s.BuffSecondsLeft <= 0) return false;
            s.BuffSecondsLeft -= dt;
            if (s.BuffSecondsLeft > 0) return false;
            s.BuffSecondsLeft = 0;
            s.ActiveBuffId = "";
            return true;
        }
    }
}
