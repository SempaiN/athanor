using System;
using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public sealed class MissionDef
    {
        public string Id;
        public string Name;    // TODO(loc): mover a tabla al agregar EN
        public double Reward;  // Esencia al completar
        public Func<GameState, bool> IsMet;
    }

    /// Cadena de objetivos (v1.1): guía el arranque y marca el camino a la Gran Obra,
    /// sin pantallas de tutorial. Se completan en orden.
    public static class MissionCatalog
    {
        static MissionDef M(string id, string name, double reward, Func<GameState, bool> met) =>
            new MissionDef { Id = id, Name = name, Reward = reward, IsMet = met };

        static int TotalGens(GameState s) => s.GeneratorsOwned.Values.Sum();

        static readonly ElementId[] Tier1 =
            { ElementId.Barro, ElementId.Lava, ElementId.Polvo, ElementId.Vapor, ElementId.Niebla, ElementId.Energia };
        static readonly ElementId[] Tier2 =
            { ElementId.Piedra, ElementId.Metal, ElementId.Cristal, ElementId.Vida };
        static readonly ElementId[] TriaPrima =
            { ElementId.Sal, ElementId.Mercurio, ElementId.Azufre };

        public static readonly IReadOnlyList<MissionDef> All = new List<MissionDef>
        {
            M("m01", "Tocá el matraz 25 veces",              50,        s => s.TotalClicks >= 25),
            M("m02", "Transmutá materiales en Esencia",      100,       s => s.LifetimeEssence >= 20),
            M("m03", "Contratá tu primer ayudante",          150,       s => TotalGens(s) >= 1),
            M("m04", "Combiná Tierra y Agua: creá Barro",    300,       s => s.Discovered.Contains(ElementId.Barro)),
            M("m05", "Descubrí los 6 compuestos básicos",    1_000,     s => Tier1.All(e => s.Discovered.Contains(e))),
            M("m06", "Llegá a 10 ayudantes en total",        2_500,     s => TotalGens(s) >= 10),
            M("m07", "Creá tu primer material superior",     5_000,     s => Tier2.Any(e => s.Discovered.Contains(e))),
            M("m08", "Comprá una mejora del laboratorio",    10_000,    s => s.UpgradesOwned.Count >= 1),
            M("m09", "Forjá la Tria Prima completa",         50_000,    s => TriaPrima.All(e => s.Discovered.Contains(e))),
            M("m10", "Transmutá el primer Oro",              200_000,   s => s.Discovered.Contains(ElementId.Oro)),
            M("m11", "Creá la Piedra Filosofal",             1_000_000, s => s.Discovered.Contains(ElementId.PiedraFilosofal)),
            M("m12", "Realizá la Gran Obra (prestigio)",     2_000_000, s => s.PrestigeCount >= 1),
        };

        /// Objetivo activo, o null si se completaron todos.
        public static MissionDef Current(GameState s) =>
            s.MissionIndex >= 0 && s.MissionIndex < All.Count ? All[s.MissionIndex] : null;

        /// Completa los objetivos cumplidos en cadena. Devuelve los completados ahora.
        public static List<MissionDef> CheckProgress(GameState s)
        {
            List<MissionDef> done = null;
            while (true)
            {
                var cur = Current(s);
                if (cur == null || !cur.IsMet(s)) break;
                s.Essence += cur.Reward;
                s.LifetimeEssence += cur.Reward;
                s.MissionIndex++;
                (done = done ?? new List<MissionDef>()).Add(cur);
            }
            return done ?? new List<MissionDef>();
        }
    }
}
