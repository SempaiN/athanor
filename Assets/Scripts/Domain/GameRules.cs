using System;

namespace Athanor.Domain
{
    /// Fórmulas y acciones del juego (GDD §4–§6, §9). Determinista y testeable.
    public static class GameRules
    {
        public const double GeneratorCostGrowth = 1.15;
        public const double OfflineEfficiency = 0.5;
        public const double OfflineCapHours = 8;
        public const double PrestigeEssenceFloor = 1_000_000;

        // ---- Click ----

        /// Unidades de cada elemento base que otorga un click.
        public static double ClickYield(GameState s, double achievementBonus) =>
            Math.Pow(2, s.ClickPowerLevel) * s.GlobalMultiplier(achievementBonus)
            * UpgradeCatalog.ClickMult(s) * BuffCatalog.ClickMult(s);

        public static void ApplyClick(GameState s, double achievementBonus)
        {
            double yield = ClickYield(s, achievementBonus);
            s.Add(ElementId.Tierra, yield);
            s.Add(ElementId.Agua, yield);
            s.Add(ElementId.Fuego, yield);
            s.Add(ElementId.Aire, yield);
            s.TotalClicks++;
        }

        // ---- Combinación ----

        public static bool CanCombine(GameState s, Recipe r) =>
            s.BalanceOf(r.InputA) >= r.UnitsPerInput &&
            s.BalanceOf(r.InputB) >= r.UnitsPerInput;

        /// Ejecuta la receta una vez. Devuelve true si el output es un descubrimiento nuevo.
        public static bool Combine(GameState s, Recipe r)
        {
            if (!CanCombine(s, r)) return false;
            bool isNew = !s.Discovered.Contains(r.Output);
            s.Add(r.InputA, -r.UnitsPerInput);
            s.Add(r.InputB, -r.UnitsPerInput);
            s.Add(r.Output, 1);
            return isNew;
        }

        /// Una receta se muestra (aunque sea con "?") si ambos insumos ya fueron descubiertos.
        public static bool IsRecipeVisible(GameState s, Recipe r) =>
            s.Discovered.Contains(r.InputA) && s.Discovered.Contains(r.InputB);

        // ---- Transmutar (vender por Esencia) ----

        public static double Transmute(GameState s, ElementId id, double units)
        {
            units = Math.Min(units, s.BalanceOf(id));
            if (units <= 0) return 0;
            double gained = units * ElementCatalog.Get(id).EssenceValue;
            s.Add(id, -units);
            s.Essence += gained;
            s.LifetimeEssence += gained;
            return gained;
        }

        /// Venta automática según las mejoras de automatización (1 vez por segundo).
        /// El Alambique perpetuo deja una reserva de 50 por elemento para combinar.
        public static double AutoSell(GameState s)
        {
            double gained = 0;
            if (UpgradeCatalog.Has(s, UpgradeEffect.AutoSellSurplus))
                foreach (var def in ElementCatalog.Elements)
                {
                    if (def.Id == ElementId.PiedraFilosofal) continue;
                    double excess = s.BalanceOf(def.Id) - 50;
                    if (excess > 0) gained += Transmute(s, def.Id, excess);
                }
            if (UpgradeCatalog.Has(s, UpgradeEffect.AutoSellBasics))
                foreach (var def in ElementCatalog.Elements)
                    if (def.Tier == 0)
                        gained += Transmute(s, def.Id, s.BalanceOf(def.Id));
            return gained;
        }

        // ---- Generadores ----

        public static double GeneratorCost(double baseCost, int owned) =>
            baseCost * Math.Pow(GeneratorCostGrowth, owned);

        /// Coste de comprar `count` de una vez (suma geométrica de la curva).
        public static double BulkCost(double baseCost, int owned, int count)
        {
            if (count <= 0) return 0;
            double g = GeneratorCostGrowth;
            return baseCost * Math.Pow(g, owned) * (Math.Pow(g, count) - 1) / (g - 1);
        }

        /// Máxima cantidad comprable con la esencia disponible.
        public static int MaxAffordable(double baseCost, int owned, double essence)
        {
            if (essence < GeneratorCost(baseCost, owned)) return 0;
            double g = GeneratorCostGrowth;
            double first = baseCost * Math.Pow(g, owned);
            int n = (int)Math.Floor(Math.Log(essence * (g - 1) / first + 1, g));
            n = Math.Min(n, 100_000);
            while (n > 0 && BulkCost(baseCost, owned, n) > essence) n--;   // corrección numérica
            while (n < 100_000 && BulkCost(baseCost, owned, n + 1) <= essence) n++;
            return n;
        }

        // ---- Prestigio: La Gran Obra ----

        public static bool CanPrestige(GameState s) =>
            s.Discovered.Contains(ElementId.PiedraFilosofal) &&
            s.LifetimeEssence >= PrestigeEssenceFloor;

        /// Quintaesencia total que corresponde a la esencia histórica actual.
        public static double PrestigeTotalFor(double lifetimeEssence) =>
            Math.Floor(Math.Sqrt(lifetimeEssence / 1_000_000.0));

        public static double PrestigeGain(GameState s) =>
            Math.Max(0, PrestigeTotalFor(s.LifetimeEssence) - s.Quintessence);

        /// Reinicia el progreso conservando Quintaesencia, logros y descubrimientos.
        public static void DoPrestige(GameState s)
        {
            if (!CanPrestige(s)) return;
            s.Quintessence += PrestigeGain(s);
            s.PrestigeCount++;
            s.Essence = 0;
            s.Balances.Clear();
            s.GeneratorsOwned.Clear();
            s.UpgradesOwned.Clear();
            s.ClickPowerLevel = 0;
        }

        // ---- Progreso offline ----

        /// Esencia ganada offline dada la producción por segundo al momento de cerrar.
        public static double OfflineEssence(double essencePerSecond, double secondsAway)
        {
            double capped = Math.Min(secondsAway, OfflineCapHours * 3600);
            if (capped <= 0) return 0;
            return essencePerSecond * capped * OfflineEfficiency;
        }

        /// Variante que respeta las mejoras compradas (eficiencia y tope).
        public static double OfflineEssence(GameState s, double essencePerSecond, double secondsAway)
        {
            double capped = Math.Min(secondsAway, UpgradeCatalog.OfflineCapHours(s) * 3600);
            if (capped <= 0) return 0;
            return essencePerSecond * capped * UpgradeCatalog.OfflineEfficiency(s);
        }
    }
}
