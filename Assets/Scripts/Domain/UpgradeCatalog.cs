using System;
using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public enum UpgradeEffect { ClickMult, ProdMult, OfflineEfficiency, OfflineCapHours }

    public sealed class UpgradeDef
    {
        public string Id;
        public string Name;   // TODO(loc): mover a tabla al agregar EN
        public string Desc;
        public double Cost;   // Esencia, compra única
        public UpgradeEffect Effect;
        public double Value;  // multiplicador o nuevo valor según efecto
    }

    /// Mejoras globales de compra única (v0.8.0).
    public static class UpgradeCatalog
    {
        public static readonly IReadOnlyList<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef { Id = "up_guantes",     Name = "Guantes de cobre",      Desc = "Poder de click x2",            Cost = 2_000,       Effect = UpgradeEffect.ClickMult,         Value = 2 },
            new UpgradeDef { Id = "up_simbolos",    Name = "Símbolos grabados",     Desc = "Producción total +25%",        Cost = 25_000,      Effect = UpgradeEffect.ProdMult,          Value = 1.25 },
            new UpgradeDef { Id = "up_mercurio",    Name = "Manos de mercurio",     Desc = "Poder de click x3",            Cost = 150_000,     Effect = UpgradeEffect.ClickMult,         Value = 3 },
            new UpgradeDef { Id = "up_reloj",       Name = "Reloj de arena eterno", Desc = "Eficiencia offline 75%",       Cost = 500_000,     Effect = UpgradeEffect.OfflineEfficiency, Value = 0.75 },
            new UpgradeDef { Id = "up_hornos",      Name = "Hornos gemelos",        Desc = "Producción total +50%",        Cost = 1_000_000,   Effect = UpgradeEffect.ProdMult,          Value = 1.5 },
            new UpgradeDef { Id = "up_calendario",  Name = "Calendario lunar",      Desc = "Tope offline: 24 horas",       Cost = 5_000_000,   Effect = UpgradeEffect.OfflineCapHours,   Value = 24 },
            new UpgradeDef { Id = "up_catalizador", Name = "Catalizador áureo",     Desc = "Producción total x2",          Cost = 20_000_000,  Effect = UpgradeEffect.ProdMult,          Value = 2 },
            new UpgradeDef { Id = "up_midas",       Name = "Dedo de Midas",         Desc = "Poder de click x5",            Cost = 100_000_000, Effect = UpgradeEffect.ClickMult,         Value = 5 },
        };

        static readonly Dictionary<string, UpgradeDef> byId = All.ToDictionary(u => u.Id);
        public static UpgradeDef Get(string id) => byId[id];

        static IEnumerable<UpgradeDef> Owned(GameState s) =>
            All.Where(u => s.UpgradesOwned.Contains(u.Id));

        public static double ClickMult(GameState s) =>
            Owned(s).Where(u => u.Effect == UpgradeEffect.ClickMult)
                    .Aggregate(1.0, (acc, u) => acc * u.Value);

        public static double ProdMult(GameState s) =>
            Owned(s).Where(u => u.Effect == UpgradeEffect.ProdMult)
                    .Aggregate(1.0, (acc, u) => acc * u.Value);

        public static double OfflineEfficiency(GameState s)
        {
            double best = GameRules.OfflineEfficiency;
            foreach (var u in Owned(s).Where(u => u.Effect == UpgradeEffect.OfflineEfficiency))
                best = Math.Max(best, u.Value);
            return best;
        }

        public static double OfflineCapHours(GameState s)
        {
            double best = GameRules.OfflineCapHours;
            foreach (var u in Owned(s).Where(u => u.Effect == UpgradeEffect.OfflineCapHours))
                best = Math.Max(best, u.Value);
            return best;
        }
    }
}
