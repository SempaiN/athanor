using System;
using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public sealed class AchievementDef
    {
        public string Id;
        public string Name;        // TODO(loc): mover a tabla de localización al agregar EN
        public string Desc;
        public double Bonus;       // multiplicador permanente de producción (0.01 = +1%)
        public Func<GameState, bool> IsMet;
    }

    /// Logros (GDD §8). El bonus total se suma al multiplicador global.
    public static class AchievementCatalog
    {
        static AchievementDef A(string id, string name, string desc, double bonus, Func<GameState, bool> met) =>
            new AchievementDef { Id = id, Name = name, Desc = desc, Bonus = bonus, IsMet = met };

        static int TotalGens(GameState s) => s.GeneratorsOwned.Values.Sum();

        public static readonly IReadOnlyList<AchievementDef> All = new List<AchievementDef>
        {
            // Clicks (+1%)
            A("clicks_100",  "Dedo curioso",      "100 toques al matraz",       0.01, s => s.TotalClicks >= 100),
            A("clicks_1k",   "Dedo constante",    "1.000 toques",               0.01, s => s.TotalClicks >= 1_000),
            A("clicks_10k",  "Dedo de hierro",    "10.000 toques",              0.01, s => s.TotalClicks >= 10_000),
            A("clicks_100k", "Dedo legendario",   "100.000 toques",             0.01, s => s.TotalClicks >= 100_000),
            A("clicks_1m",   "El Dedo Filosofal", "1.000.000 de toques",        0.01, s => s.TotalClicks >= 1_000_000),
            // Esencia histórica (+2%)
            A("ess_1k",  "Primeras gotas",   "1.000 de Esencia histórica", 0.02, s => s.LifetimeEssence >= 1e3),
            A("ess_1m",  "Río dorado",       "1 M de Esencia histórica",   0.02, s => s.LifetimeEssence >= 1e6),
            A("ess_1b",  "Océano dorado",    "1 B de Esencia histórica",   0.02, s => s.LifetimeEssence >= 1e9),
            A("ess_1t",  "Diluvio dorado",   "1 T de Esencia histórica",   0.02, s => s.LifetimeEssence >= 1e12),
            A("ess_1qa", "Esencia infinita", "1 Qa de Esencia histórica",  0.02, s => s.LifetimeEssence >= 1e15),
            // Elementos descubiertos (+2%)
            A("disc_6",  "Aprendiz alquimista", "6 elementos descubiertos",  0.02, s => s.Discovered.Count >= 6),
            A("disc_10", "Adepto",              "10 elementos descubiertos", 0.02, s => s.Discovered.Count >= 10),
            A("disc_15", "Maestro",             "15 elementos descubiertos", 0.02, s => s.Discovered.Count >= 15),
            A("disc_20", "Sabio hermético",     "los 20 elementos",          0.02, s => s.Discovered.Count >= 20),
            // Generadores (+1%)
            A("gen_1",   "Primer ayudante", "1 ayudante contratado",  0.01, s => TotalGens(s) >= 1),
            A("gen_10",  "Pequeño taller",  "10 ayudantes",           0.01, s => TotalGens(s) >= 10),
            A("gen_50",  "Gran taller",     "50 ayudantes",           0.01, s => TotalGens(s) >= 50),
            A("gen_100", "Manufactura",     "100 ayudantes",          0.01, s => TotalGens(s) >= 100),
            A("gen_250", "Gremio propio",   "250 ayudantes",          0.01, s => TotalGens(s) >= 250),
            // Prestigios (+5%)
            A("pres_1",  "La Gran Obra",   "primer prestigio", 0.05, s => s.PrestigeCount >= 1),
            A("pres_3",  "Ciclo eterno",   "3 prestigios",     0.05, s => s.PrestigeCount >= 3),
            A("pres_10", "Uróboros",       "10 prestigios",    0.05, s => s.PrestigeCount >= 10),
            // Especiales
            A("sp_oro",    "Toque de Midas",     "descubrir el Oro",              0.02, s => s.Discovered.Contains(ElementId.Oro)),
            A("sp_piedra", "Magnum Opus",        "crear la Piedra Filosofal",     0.05, s => s.Discovered.Contains(ElementId.PiedraFilosofal)),
            A("sp_quint",  "Alma quintaesencia", "10 de Quintaesencia",           0.05, s => s.Quintessence >= 10),
            A("sp_click5", "Manos doradas",      "Poder de click nivel 6",        0.02, s => s.ClickPowerLevel >= 5),
            // Matraz Dorado
            A("gold_1",  "Reflejo dorado",  "atrapar 1 Matraz Dorado",    0.01, s => s.GoldenTaps >= 1),
            A("gold_10", "Cazador dorado",  "atrapar 10 Matraces Dorados", 0.02, s => s.GoldenTaps >= 10),
            A("gold_50", "Bendición áurea", "atrapar 50 Matraces Dorados", 0.05, s => s.GoldenTaps >= 50),
        };

        /// Bonus total de los logros ya desbloqueados.
        public static double TotalBonus(GameState s) =>
            All.Where(a => s.AchievementsUnlocked.Contains(a.Id)).Sum(a => a.Bonus);

        /// Desbloquea los logros cumplidos. Devuelve los nuevos (para toasts).
        public static List<AchievementDef> CheckUnlocks(GameState s)
        {
            List<AchievementDef> unlocked = null;
            foreach (var a in All)
            {
                if (s.AchievementsUnlocked.Contains(a.Id)) continue;
                if (!a.IsMet(s)) continue;
                s.AchievementsUnlocked.Add(a.Id);
                (unlocked = unlocked ?? new List<AchievementDef>()).Add(a);
            }
            return unlocked ?? new List<AchievementDef>();
        }
    }
}
