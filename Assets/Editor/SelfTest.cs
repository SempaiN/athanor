using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Athanor.Domain;
using Athanor.Infra;

namespace Athanor.EditorTools
{
    /// Tests de dominio ejecutables por CLI:
    ///   Unity.exe -batchmode -executeMethod Athanor.EditorTools.SelfTest.Run
    public static class SelfTest
    {
        static int failures;

        static void Check(bool cond, string what)
        {
            if (cond) return;
            failures++;
            Debug.LogError("[SelfTest] FALLA: " + what);
        }

        public static void Run()
        {
            try
            {
                failures = 0;
                CatalogIntegrity();
                LocalizationCoverage();
                ClickRules();
                CombineRules();
                TransmuteRules();
                GeneratorRules();
                PrestigeRules();
                OfflineRules();
                AchievementRules();
                SaveRoundtrip();
                EconomySanity();

                Debug.Log(failures == 0
                    ? "[SelfTest] TODO OK"
                    : $"[SelfTest] {failures} fallas");
                EditorApplication.Exit(failures == 0 ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[SelfTest] Excepción: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void CatalogIntegrity()
        {
            Check(ElementCatalog.Elements.Count == 20, "deben ser 20 elementos");
            Check(ElementCatalog.Recipes.Count == 16, "deben ser 16 recetas");
            Check(ElementCatalog.Elements.Select(e => e.Id).Distinct().Count() == 20, "ids únicos");
            Check(ElementCatalog.Recipes.Select(r => r.Output).Distinct().Count() == 16, "outputs únicos");

            var ids = new HashSet<ElementId>(ElementCatalog.Elements.Select(e => e.Id));
            foreach (var r in ElementCatalog.Recipes)
            {
                Check(ids.Contains(r.InputA) && ids.Contains(r.InputB) && ids.Contains(r.Output),
                      "receta con ids válidos: " + r.Output);
                Check(r.UnitsPerInput > 0, "unidades > 0: " + r.Output);
                int tierOut = ElementCatalog.Get(r.Output).Tier;
                Check(ElementCatalog.Get(r.InputA).Tier < tierOut && ElementCatalog.Get(r.InputB).Tier < tierOut,
                      "insumos de tier menor: " + r.Output);
            }

            // Cada elemento de tier > 0 tiene receta; los tier 0 no
            foreach (var e in ElementCatalog.Elements)
            {
                bool hasRecipe = ElementCatalog.RecipeFor(e.Id) != null;
                Check(e.Tier == 0 ? !hasRecipe : hasRecipe, "receta según tier: " + e.Id);
                Check(ColorUtility.TryParseHtmlString(e.ColorHex, out _), "color parseable: " + e.Id);
                Check(e.EssenceValue > 0, "valor > 0: " + e.Id);
            }

            Check(GeneratorCatalog.Generators.Count == 8, "8 generadores");
            Check(GeneratorCatalog.Generators.Select(g => g.Id).Distinct().Count() == 8, "ids de generador únicos");
            var costs = GeneratorCatalog.Generators.Select(g => g.BaseCost).ToList();
            Check(costs.SequenceEqual(costs.OrderBy(c => c).ToList()), "costes de generador crecientes");
        }

        static void LocalizationCoverage()
        {
            foreach (var e in ElementCatalog.Elements)
                Check(Loc.T(e.NameKey) != e.NameKey, "loc de elemento: " + e.NameKey);
            foreach (var g in GeneratorCatalog.Generators)
                Check(Loc.T(g.NameKey) != g.NameKey, "loc de generador: " + g.NameKey);
            string[] uiKeys = {
                "ui_esencia","ui_transmutar_todo","ui_poder_click","ui_nivel","ui_coste",
                "ui_update_titulo","ui_update_texto","ui_update_descargar","ui_update_luego",
                "ui_tab_lab","ui_tab_ayudantes","ui_tab_elementos","ui_tab_logros","ui_tab_obra","ui_tab_ajustes",
                "ui_comprar","ui_produce","ui_combina_solo","ui_desconocido",
                "ui_combinar","ui_vender","ui_nuevo_elemento","ui_receta_oculta",
                "ui_quintaesencia","ui_bonus_actual","ui_prestigios","ui_prestigio_desc",
                "ui_prestigio_boton","ui_prestigio_confirmar","ui_logro",
                "ui_ajuste_calidad","ui_calidad_rendimiento","ui_calidad_alta",
                "ui_ajuste_sonido","ui_sonido_on","ui_sonido_off","ui_ajuste_info",
                "ui_reset","ui_reset_confirmar","ui_offline_titulo","ui_offline_texto","ui_ok",
            };
            foreach (var k in uiKeys)
                Check(Loc.T(k) != k, "loc UI: " + k);
        }

        static void ClickRules()
        {
            var s = new GameState();
            Check(Math.Abs(GameRules.ClickYield(s, 0) - 1) < 1e-9, "click base = 1");
            s.ClickPowerLevel = 3;
            Check(Math.Abs(GameRules.ClickYield(s, 0) - 8) < 1e-9, "click x2 por nivel");
            s.Quintessence = 5; // +50%
            Check(Math.Abs(GameRules.ClickYield(s, 0) - 12) < 1e-9, "click con quintaesencia");

            s = new GameState();
            GameRules.ApplyClick(s, 0);
            Check(s.TotalClicks == 1, "cuenta clicks");
            Check(Math.Abs(s.BalanceOf(ElementId.Tierra) - 1) < 1e-9 &&
                  Math.Abs(s.BalanceOf(ElementId.Aire) - 1) < 1e-9, "click da los 4 básicos");
        }

        static void CombineRules()
        {
            var s = new GameState();
            var recipe = ElementCatalog.RecipeFor(ElementId.Barro);
            Check(!GameRules.CanCombine(s, recipe), "no combina sin materiales");

            s.Add(ElementId.Tierra, 10);
            s.Add(ElementId.Agua, 9.5);
            Check(!GameRules.CanCombine(s, recipe), "no combina con 9.5");

            s.Add(ElementId.Agua, 0.5);
            Check(GameRules.CanCombine(s, recipe), "combina justo con 10");
            bool isNew = GameRules.Combine(s, recipe);
            Check(isNew, "primer combine descubre");
            Check(Math.Abs(s.BalanceOf(ElementId.Tierra)) < 1e-9, "consume tierra");
            Check(Math.Abs(s.BalanceOf(ElementId.Barro) - 1) < 1e-9, "produce 1 barro");
            Check(s.Discovered.Contains(ElementId.Barro), "queda descubierto");

            s.Add(ElementId.Tierra, 10);
            s.Add(ElementId.Agua, 10);
            Check(!GameRules.Combine(s, recipe), "segundo combine no es nuevo");
        }

        static void TransmuteRules()
        {
            var s = new GameState();
            s.Add(ElementId.Oro, 3);
            double gained = GameRules.Transmute(s, ElementId.Oro, 2);
            double oroVal = ElementCatalog.Get(ElementId.Oro).EssenceValue;
            Check(Math.Abs(gained - 2 * oroVal) < 1e-6, "esencia por transmutación");
            Check(Math.Abs(s.BalanceOf(ElementId.Oro) - 1) < 1e-9, "resta unidades");
            Check(Math.Abs(s.Essence - gained) < 1e-6 && Math.Abs(s.LifetimeEssence - gained) < 1e-6,
                  "esencia e histórica");
            Check(GameRules.Transmute(s, ElementId.Oro, 100) <= oroVal + 1e-6, "no vende más de lo que hay");
        }

        static void GeneratorRules()
        {
            Check(Math.Abs(GameRules.GeneratorCost(100, 0) - 100) < 1e-9, "coste base");
            Check(Math.Abs(GameRules.GeneratorCost(100, 2) - 100 * 1.15 * 1.15) < 1e-6, "coste x1.15^n");

            var s = new GameState();
            s.GeneratorsOwned["aprendiz"] = 2; // 0.5/s c/u → 1/s tierra
            GeneratorCatalog.Tick(s, 10, 0);
            Check(Math.Abs(s.BalanceOf(ElementId.Tierra) - 10) < 1e-6, "producción 10s aprendiz x2");

            s = new GameState();
            s.GeneratorsOwned["crisol"] = 1; // 6/s repartidos entre barro y lava
            GeneratorCatalog.Tick(s, 1, 0);
            Check(Math.Abs(s.BalanceOf(ElementId.Barro) - 3) < 1e-6 &&
                  Math.Abs(s.BalanceOf(ElementId.Lava) - 3) < 1e-6, "crisol alterna materiales");
        }

        static void PrestigeRules()
        {
            Check(Math.Abs(GameRules.PrestigeTotalFor(1e6) - 1) < 1e-9, "1 quint con 1M");
            Check(Math.Abs(GameRules.PrestigeTotalFor(9e6) - 3) < 1e-9, "3 quint con 9M");

            var s = new GameState();
            s.LifetimeEssence = 4e6;
            Check(!GameRules.CanPrestige(s), "sin piedra no hay prestigio");
            s.Discovered.Add(ElementId.PiedraFilosofal);
            Check(GameRules.CanPrestige(s), "con piedra y esencia sí");
            Check(Math.Abs(GameRules.PrestigeGain(s) - 2) < 1e-9, "ganancia = 2");

            s.Essence = 500;
            s.Add(ElementId.Oro, 5);
            s.GeneratorsOwned["aprendiz"] = 7;
            s.ClickPowerLevel = 4;
            s.AchievementsUnlocked.Add("clicks_100");
            GameRules.DoPrestige(s);
            Check(Math.Abs(s.Quintessence - 2) < 1e-9, "quint acumulada");
            Check(s.PrestigeCount == 1, "cuenta prestigios");
            Check(s.Essence == 0 && s.Balances.Count == 0 && s.GeneratorsOwned.Count == 0
                  && s.ClickPowerLevel == 0, "resetea progreso");
            Check(s.Discovered.Contains(ElementId.PiedraFilosofal) &&
                  s.AchievementsUnlocked.Contains("clicks_100"), "conserva descubrimientos y logros");
            Check(Math.Abs(GameRules.PrestigeGain(s)) < 1e-9, "sin ganancia inmediata tras prestigiar");
        }

        static void OfflineRules()
        {
            Check(Math.Abs(GameRules.OfflineEssence(10, 3600) - 10 * 3600 * 0.5) < 1e-6, "offline 50%");
            Check(Math.Abs(GameRules.OfflineEssence(10, 100 * 3600) - 10 * 8 * 3600 * 0.5) < 1e-6, "tope 8h");
            Check(GameRules.OfflineEssence(10, -5) == 0, "tiempo negativo = 0");
        }

        static void AchievementRules()
        {
            Check(AchievementCatalog.All.Count >= 24, "al menos 24 logros");
            Check(AchievementCatalog.All.Select(a => a.Id).Distinct().Count() == AchievementCatalog.All.Count,
                  "ids de logro únicos");

            var s = new GameState();
            foreach (var a in AchievementCatalog.All)
                a.IsMet(s); // no debe lanzar con estado fresco

            s.TotalClicks = 150;
            var news = AchievementCatalog.CheckUnlocks(s);
            Check(news.Any(a => a.Id == "clicks_100"), "desbloquea clicks_100");
            Check(AchievementCatalog.CheckUnlocks(s).Count == 0, "no re-desbloquea");
            Check(Math.Abs(AchievementCatalog.TotalBonus(s) - 0.01) < 1e-9, "bonus acumulado");
        }

        static void SaveRoundtrip()
        {
            var s = new GameState
            {
                Essence = 123.45,
                LifetimeEssence = 999.9,
                Quintessence = 3,
                TotalClicks = 777,
                PrestigeCount = 2,
                ClickPowerLevel = 5,
                LastSeenUnixUtc = 1712345678,
                HighQualityMode = true,
                SoundOff = true,
            };
            s.Add(ElementId.Oro, 12.5);
            s.Add(ElementId.Vapor, 3);
            s.GeneratorsOwned["brasero"] = 4;
            s.AchievementsUnlocked.Add("ess_1k");

            string json = JsonUtility.ToJson(SaveDto.From(s));
            var back = JsonUtility.FromJson<SaveDto>(json).ToState();

            Check(Math.Abs(back.Essence - s.Essence) < 1e-9, "save: essence");
            Check(Math.Abs(back.BalanceOf(ElementId.Oro) - 12.5) < 1e-9, "save: balances");
            Check(back.Discovered.Contains(ElementId.Oro) && back.Discovered.Contains(ElementId.Tierra),
                  "save: descubiertos");
            Check(back.GeneratorsOwned["brasero"] == 4, "save: generadores");
            Check(back.AchievementsUnlocked.Contains("ess_1k"), "save: logros");
            Check(back.HighQualityMode && back.SoundOff, "save: flags");
            Check(back.TotalClicks == 777 && back.PrestigeCount == 2 && back.ClickPowerLevel == 5,
                  "save: contadores");
            Check(back.LastSeenUnixUtc == 1712345678, "save: timestamp");
        }

        // La economía debe premiar combinar y la Piedra Filosofal debe ser alcanzable.
        static void EconomySanity()
        {
            foreach (var r in ElementCatalog.Recipes)
            {
                double inputCost = r.UnitsPerInput *
                    (ElementCatalog.Get(r.InputA).EssenceValue + ElementCatalog.Get(r.InputB).EssenceValue);
                double outputValue = ElementCatalog.Get(r.Output).EssenceValue;
                Check(outputValue > inputCost * 1.2,
                      $"combinar {r.Output} debe dar ganancia (in {inputCost} vs out {outputValue})");
            }

            double baseUnits = BaseUnitsFor(ElementId.PiedraFilosofal);
            Check(baseUnits < 200_000,
                  $"Piedra Filosofal alcanzable: {baseUnits} unidades base (límite 200K)");
            Debug.Log($"[SelfTest] 1 Piedra Filosofal = {baseUnits} unidades base");
        }

        static double BaseUnitsFor(ElementId id)
        {
            var recipe = ElementCatalog.RecipeFor(id);
            if (recipe == null) return 1;
            return recipe.UnitsPerInput * (BaseUnitsFor(recipe.InputA) + BaseUnitsFor(recipe.InputB));
        }
    }
}
