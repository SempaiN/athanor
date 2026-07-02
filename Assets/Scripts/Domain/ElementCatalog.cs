using System;
using System.Collections.Generic;
using System.Linq;

namespace Athanor.Domain
{
    public enum ElementId
    {
        Tierra, Agua, Fuego, Aire,
        Barro, Lava, Polvo, Vapor, Niebla, Energia,
        Piedra, Metal, Cristal, Vida,
        Sal, Mercurio, Azufre,
        Oro, Eter,
        PiedraFilosofal
    }

    public sealed class ElementDef
    {
        public ElementId Id;
        public string NameKey;      // clave de localización, ej. "el_tierra"
        public int Tier;
        public double EssenceValue; // valor al transmutar 1 unidad en Esencia
        public string ColorHex;     // color flat del placeholder e identidad visual
    }

    public sealed class Recipe
    {
        public ElementId Output;
        public ElementId InputA;
        public ElementId InputB;
        public int UnitsPerInput = 10; // consume 10 de cada insumo → 1 de output
    }

    /// Catálogo estático: 20 elementos y su árbol de recetas (GDD §3).
    public static class ElementCatalog
    {
        public static readonly IReadOnlyList<ElementDef> Elements = new List<ElementDef>
        {
            // Tier 0
            Def(ElementId.Tierra, 0, 1, "#8C5A3C"),
            Def(ElementId.Agua,   0, 1, "#3D9BB3"),
            Def(ElementId.Fuego,  0, 1, "#E4572E"),
            Def(ElementId.Aire,   0, 1, "#9FB8C8"),
            // Tier 1 (valor 25)
            Def(ElementId.Barro,   1, 25, "#6E4F35"),
            Def(ElementId.Lava,    1, 25, "#D9642E"),
            Def(ElementId.Polvo,   1, 25, "#B59F7E"),
            Def(ElementId.Vapor,   1, 25, "#B8D8DB"),
            Def(ElementId.Niebla,  1, 25, "#7FA8B8"),
            Def(ElementId.Energia, 1, 25, "#F2C14E"),
            // Tier 2 (valor 625)
            Def(ElementId.Piedra,  2, 625, "#7D7A75"),
            Def(ElementId.Metal,   2, 625, "#A8AABC"),
            Def(ElementId.Cristal, 2, 625, "#BFE3E0"),
            Def(ElementId.Vida,    2, 625, "#7FB069"),
            // Tier 3 — Tria Prima (valor 15625)
            Def(ElementId.Sal,      3, 15_625, "#E8E4DA"),
            Def(ElementId.Mercurio, 3, 15_625, "#C0C5CE"),
            Def(ElementId.Azufre,   3, 15_625, "#E3B505"),
            // Tier 4 (valor ≈ 390k)
            Def(ElementId.Oro,  4, 390_625, "#E8C547"),
            Def(ElementId.Eter, 4, 390_625, "#9B72CF"),
            // Tier 5
            Def(ElementId.PiedraFilosofal, 5, 9_765_625, "#D64550"),
        };

        public static readonly IReadOnlyList<Recipe> Recipes = new List<Recipe>
        {
            R(ElementId.Barro,   ElementId.Tierra, ElementId.Agua),
            R(ElementId.Lava,    ElementId.Tierra, ElementId.Fuego),
            R(ElementId.Polvo,   ElementId.Tierra, ElementId.Aire),
            R(ElementId.Vapor,   ElementId.Agua,   ElementId.Fuego),
            R(ElementId.Niebla,  ElementId.Agua,   ElementId.Aire),
            R(ElementId.Energia, ElementId.Fuego,  ElementId.Aire),

            R(ElementId.Piedra,  ElementId.Barro, ElementId.Lava),
            R(ElementId.Metal,   ElementId.Lava,  ElementId.Polvo),
            R(ElementId.Cristal, ElementId.Vapor, ElementId.Niebla),
            R(ElementId.Vida,    ElementId.Barro, ElementId.Energia),

            R(ElementId.Sal,      ElementId.Piedra, ElementId.Cristal),
            R(ElementId.Mercurio, ElementId.Metal,  ElementId.Niebla),
            R(ElementId.Azufre,   ElementId.Energia, ElementId.Vida),

            R(ElementId.Oro,  ElementId.Azufre,   ElementId.Mercurio),
            R(ElementId.Eter, ElementId.Mercurio, ElementId.Sal),

            R(ElementId.PiedraFilosofal, ElementId.Oro, ElementId.Eter),
        };

        static readonly Dictionary<ElementId, ElementDef> byId =
            Elements.ToDictionary(e => e.Id);

        public static ElementDef Get(ElementId id) => byId[id];

        public static Recipe RecipeFor(ElementId output) =>
            Recipes.FirstOrDefault(r => r.Output == output);

        static ElementDef Def(ElementId id, int tier, double value, string color) =>
            new ElementDef
            {
                Id = id,
                NameKey = "el_" + id.ToString().ToLowerInvariant(),
                Tier = tier,
                EssenceValue = value,
                ColorHex = color,
            };

        static Recipe R(ElementId output, ElementId a, ElementId b) =>
            new Recipe { Output = output, InputA = a, InputB = b };
    }
}
