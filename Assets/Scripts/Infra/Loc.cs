using System.Collections.Generic;

namespace Athanor.Infra
{
    /// Localización mínima: es como idioma base, estructura lista para agregar en.
    public static class Loc
    {
        static readonly Dictionary<string, string> es = new Dictionary<string, string>
        {
            // Elementos
            { "el_tierra", "Tierra" }, { "el_agua", "Agua" }, { "el_fuego", "Fuego" },
            { "el_aire", "Aire" }, { "el_barro", "Barro" }, { "el_lava", "Lava" },
            { "el_polvo", "Polvo" }, { "el_vapor", "Vapor" }, { "el_niebla", "Niebla" },
            { "el_energia", "Energía" }, { "el_piedra", "Piedra" }, { "el_metal", "Metal" },
            { "el_cristal", "Cristal" }, { "el_vida", "Vida" }, { "el_sal", "Sal" },
            { "el_mercurio", "Mercurio" }, { "el_azufre", "Azufre" }, { "el_oro", "Oro" },
            { "el_eter", "Éter" }, { "el_piedrafilosofal", "Piedra Filosofal" },
            // Generadores
            { "gen_aprendiz", "Aprendiz" }, { "gen_alambique", "Alambique" },
            { "gen_brasero", "Brasero" }, { "gen_fuelle", "Fuelle" },
            { "gen_crisol", "Crisol" }, { "gen_condensador", "Condensador" },
            { "gen_athanor", "Horno Athanor" }, { "gen_transmutador", "Transmutador" },
            // UI
            { "ui_esencia", "Esencia" },
            { "ui_transmutar_todo", "Transmutar todo" },
            { "ui_poder_click", "Poder de click" },
            { "ui_nivel", "Nv." },
            { "ui_coste", "Coste" },
            { "ui_update_titulo", "Actualización disponible" },
            { "ui_update_texto", "Hay una versión nueva del juego:" },
            { "ui_update_descargar", "Descargar" },
            { "ui_update_luego", "Más tarde" },
        };

        public static string T(string key) =>
            es.TryGetValue(key, out var v) ? v : key;
    }
}
