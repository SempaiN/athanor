using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public sealed class GeneratorDef
    {
        public string Id;           // clave estable para el guardado
        public string NameKey;      // clave de localización
        public double BaseCost;     // en Esencia
        public double BaseProd;     // unidades/segundo por unidad comprada
        public ElementId[] Produces; // si produce varios, alterna en partes iguales
        public bool AutoCombine;    // Transmutador: ejecuta la mejor receta conocida
    }

    /// Ayudantes (GDD §5). Balance inicial, ajustable con testing.
    public static class GeneratorCatalog
    {
        public static readonly IReadOnlyList<GeneratorDef> Generators = new List<GeneratorDef>
        {
            new GeneratorDef { Id = "aprendiz",     NameKey = "gen_aprendiz",     BaseCost = 15,        BaseProd = 0.5, Produces = new[]{ ElementId.Tierra } },
            new GeneratorDef { Id = "alambique",    NameKey = "gen_alambique",    BaseCost = 100,       BaseProd = 2,   Produces = new[]{ ElementId.Agua } },
            new GeneratorDef { Id = "brasero",      NameKey = "gen_brasero",      BaseCost = 600,       BaseProd = 8,   Produces = new[]{ ElementId.Fuego } },
            new GeneratorDef { Id = "fuelle",       NameKey = "gen_fuelle",       BaseCost = 3_500,     BaseProd = 20,  Produces = new[]{ ElementId.Aire } },
            new GeneratorDef { Id = "crisol",       NameKey = "gen_crisol",       BaseCost = 20_000,    BaseProd = 6,   Produces = new[]{ ElementId.Barro, ElementId.Lava } },
            new GeneratorDef { Id = "condensador",  NameKey = "gen_condensador",  BaseCost = 120_000,   BaseProd = 15,  Produces = new[]{ ElementId.Vapor, ElementId.Niebla } },
            new GeneratorDef { Id = "athanor",      NameKey = "gen_athanor",      BaseCost = 700_000,   BaseProd = 35,  Produces = new[]{ ElementId.Energia } },
            new GeneratorDef { Id = "transmutador", NameKey = "gen_transmutador", BaseCost = 4_000_000, BaseProd = 1,   Produces = new ElementId[0], AutoCombine = true },
        };

        static readonly Dictionary<string, GeneratorDef> byId =
            Generators.ToDictionary(g => g.Id);

        public static GeneratorDef Get(string id) => byId[id];

        /// Aplica la producción de todos los generadores durante deltaSeconds.
        public static void Tick(GameState s, double deltaSeconds, double achievementBonus)
        {
            double mult = s.GlobalMultiplier(achievementBonus);
            foreach (var gen in Generators)
            {
                int owned = s.GeneratorsOwned.TryGetValue(gen.Id, out var n) ? n : 0;
                if (owned == 0) continue;

                if (gen.AutoCombine)
                {
                    // El Transmutador ejecuta la receta descubierta de mayor tier, owned veces/s.
                    // Acumulación fraccional se maneja en la capa Unity (ver GameLoop).
                    continue;
                }

                double units = gen.BaseProd * owned * mult * deltaSeconds / gen.Produces.Length;
                foreach (var el in gen.Produces)
                    s.Add(el, units);
            }
        }
    }
}
