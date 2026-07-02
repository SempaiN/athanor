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
                UpgradeRules();
                BuffRules();
                PrestigeRules();
                BalanceSimulation();
                OfflineRules();
                AchievementRules();
                MissionRules();
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

            // Compra en lote: suma geométrica == suma manual
            double manual = 0;
            for (int k = 0; k < 7; k++) manual += GameRules.GeneratorCost(100, 3 + k);
            Check(Math.Abs(GameRules.BulkCost(100, 3, 7) - manual) < 1e-6, "bulk = suma manual");
            Check(GameRules.BulkCost(100, 0, 0) == 0, "bulk 0 = 0");

            // Máx comprable: consistente con el coste
            double budget = GameRules.BulkCost(100, 5, 12) + 0.01;
            int max = GameRules.MaxAffordable(100, 5, budget);
            Check(max == 12, $"max affordable = 12 (dio {max})");
            Check(GameRules.MaxAffordable(100, 0, 99) == 0, "max = 0 si no alcanza");
            Check(GameRules.MaxAffordable(100, 0, 100) == 1, "max = 1 justo");

            var s = new GameState();
            s.GeneratorsOwned["aprendiz"] = 2; // 0.5/s c/u → 1/s tierra
            GeneratorCatalog.Tick(s, 10, 0);
            Check(Math.Abs(s.BalanceOf(ElementId.Tierra) - 10) < 1e-6, "producción 10s aprendiz x2");

            s = new GameState();
            s.GeneratorsOwned["crisol"] = 1; // 6/s repartidos entre barro y lava
            GeneratorCatalog.Tick(s, 1, 0);
            Check(Math.Abs(s.BalanceOf(ElementId.Barro) - 3) < 1e-6 &&
                  Math.Abs(s.BalanceOf(ElementId.Lava) - 3) < 1e-6, "crisol alterna materiales");

            // Hitos: x2 por umbral alcanzado
            Check(Math.Abs(GeneratorCatalog.MilestoneMult(9) - 1) < 1e-9, "sin hito antes de 10");
            Check(Math.Abs(GeneratorCatalog.MilestoneMult(10) - 2) < 1e-9, "x2 con 10");
            Check(Math.Abs(GeneratorCatalog.MilestoneMult(100) - 16) < 1e-9, "x16 con 100");
            Check(GeneratorCatalog.NextMilestone(10) == 25, "próximo hito 25");
            Check(GeneratorCatalog.NextMilestone(500) == 0, "sin hitos restantes");

            s = new GameState();
            s.GeneratorsOwned["aprendiz"] = 10; // 0.5*10*x2 = 10/s
            GeneratorCatalog.Tick(s, 1, 0);
            Check(Math.Abs(s.BalanceOf(ElementId.Tierra) - 10) < 1e-6, "hito aplicado en producción");
        }

        static void UpgradeRules()
        {
            Check(UpgradeCatalog.All.Count == 8, "8 mejoras");
            Check(UpgradeCatalog.All.Select(u => u.Id).Distinct().Count() == 8, "ids de mejora únicos");

            var s = new GameState();
            Check(Math.Abs(UpgradeCatalog.ClickMult(s) - 1) < 1e-9, "click mult base 1");
            Check(Math.Abs(UpgradeCatalog.ProdMult(s) - 1) < 1e-9, "prod mult base 1");

            s.UpgradesOwned.Add("up_guantes");   // click x2
            s.UpgradesOwned.Add("up_mercurio");  // click x3
            Check(Math.Abs(UpgradeCatalog.ClickMult(s) - 6) < 1e-9, "clicks multiplicativos");
            Check(Math.Abs(GameRules.ClickYield(s, 0) - 6) < 1e-9, "yield con mejoras");

            s.UpgradesOwned.Add("up_simbolos");  // prod +25%
            Check(Math.Abs(s.GlobalMultiplier(0) - 1.25) < 1e-9, "prod mult en multiplicador global");

            // Offline con mejoras
            Check(Math.Abs(GameRules.OfflineEssence(s, 10, 3600) - 10 * 3600 * 0.5) < 1e-6,
                  "offline 50% sin mejora");
            s.UpgradesOwned.Add("up_reloj");
            Check(Math.Abs(GameRules.OfflineEssence(s, 10, 3600) - 10 * 3600 * 0.75) < 1e-6,
                  "offline 75% con reloj");
            s.UpgradesOwned.Add("up_calendario");
            Check(Math.Abs(GameRules.OfflineEssence(s, 10, 100 * 3600) - 10 * 24 * 3600 * 0.75) < 1e-6,
                  "tope 24h con calendario");

            // El prestigio limpia las mejoras
            s.LifetimeEssence = 4e6;
            s.Discovered.Add(ElementId.PiedraFilosofal);
            GameRules.DoPrestige(s);
            Check(s.UpgradesOwned.Count == 0, "prestigio limpia mejoras");
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

        static void MissionRules()
        {
            Check(MissionCatalog.All.Count == 12, "12 objetivos");
            Check(MissionCatalog.All.Select(m => m.Id).Distinct().Count() == 12, "ids de objetivo únicos");
            Check(MissionCatalog.All.All(m => m.Reward > 0), "recompensas positivas");

            var s = new GameState();
            Check(MissionCatalog.Current(s).Id == "m01", "arranca en m01");
            Check(MissionCatalog.CheckProgress(s).Count == 0, "nada cumplido de inicio");

            // Cumplir m01 encadena m02 (la recompensa de m01 supera el umbral de esencia de m02)
            s.TotalClicks = 25;
            var done = MissionCatalog.CheckProgress(s);
            Check(done.Count == 2 && done[0].Id == "m01" && done[1].Id == "m02",
                  $"cadena m01+m02 (dio {done.Count})");
            Check(s.MissionIndex == 2, "índice avanza a 2");
            Check(Math.Abs(s.Essence - 150) < 1e-9, "recompensas acumuladas 50+100");

            Check(MissionCatalog.Current(s).Id == "m03", "sigue m03");
            s.GeneratorsOwned["aprendiz"] = 1;
            Check(MissionCatalog.CheckProgress(s).Count == 1, "m03 al contratar");

            // El último objetivo cierra la cadena
            s.MissionIndex = MissionCatalog.All.Count;
            Check(MissionCatalog.Current(s) == null, "cadena terminada = null");
        }

        static void BuffRules()
        {
            Check(BuffCatalog.All.Count == 3, "3 buffs");
            Check(Math.Abs(BuffCatalog.Roll(0.0).Weight - 45) < 1e-9, "roll 0 → frenesí");
            Check(BuffCatalog.Roll(0.999).Duration == 0, "roll alto → fortuna");

            var s = new GameState();
            Check(BuffCatalog.ProdMult(s) == 1 && BuffCatalog.ClickMult(s) == 1, "sin buff = x1");

            var frenesi = BuffCatalog.Get("frenesi");
            BuffCatalog.Apply(s, frenesi, 0);
            Check(Math.Abs(s.GlobalMultiplier(0) - 7) < 1e-9, "frenesí x7 en producción");
            Check(!BuffCatalog.Tick(s, 30), "no expira a mitad");
            Check(BuffCatalog.Tick(s, 31), "expira al agotar");
            Check(s.ActiveBuffId == "" && BuffCatalog.ProdMult(s) == 1, "limpio tras expirar");

            s.Essence = 1000;
            s.LifetimeEssence = 1000;
            var fortuna = BuffCatalog.Get("fortuna");
            double gained = BuffCatalog.Apply(s, fortuna, 10); // 10% de 1000 + 30*10 = 400
            Check(Math.Abs(gained - 400) < 1e-6 && Math.Abs(s.Essence - 1400) < 1e-6, "fortuna instantánea");

            var fiebre = BuffCatalog.Get("fiebre");
            BuffCatalog.Apply(s, fiebre, 0);
            Check(Math.Abs(GameRules.ClickYield(s, 0) - 7) < 1e-9, "fiebre x7 en click");
        }

        /// Bot greedy que juega 2 h simuladas: valida la economía de punta a punta.
        static void BalanceSimulation()
        {
            var s = new GameState();
            double firstPrestigeAt = -1;
            const double dt = 1.0;
            const int hours = 2;

            for (int t = 0; t < hours * 3600; t++)
            {
                // 2 clicks por segundo
                GameRules.ApplyClick(s, 0);
                GameRules.ApplyClick(s, 0);

                GeneratorCatalog.Tick(s, dt, 0);

                if (t % 5 == 0)
                {
                    // 1) Combinar solo con excedente (deja reserva para seguir vendiendo)
                    foreach (var r in ElementCatalog.Recipes.OrderByDescending(r => ElementCatalog.Get(r.Output).Tier))
                    {
                        int safety = 0;
                        while (s.BalanceOf(r.InputA) > r.UnitsPerInput * 3 &&
                               s.BalanceOf(r.InputB) > r.UnitsPerInput * 3 &&
                               GameRules.CanCombine(s, r) && safety++ < 20)
                            GameRules.Combine(s, r);
                    }

                    // 2) Vender el excedente de todo (reserva 60 de lo que sirve de insumo)
                    foreach (var def in ElementCatalog.Elements)
                    {
                        bool isInput = ElementCatalog.Recipes.Any(r => r.InputA == def.Id || r.InputB == def.Id);
                        double reserve = isInput ? 60 : 0;
                        double excess = s.BalanceOf(def.Id) - reserve;
                        if (excess > 0 && def.Id != ElementId.PiedraFilosofal)
                            GameRules.Transmute(s, def.Id, excess);
                    }

                    // 3) Mejorar el click cuando sobra (x2 por nivel: motor del early game)
                    double upCost = 50 * Math.Pow(4, s.ClickPowerLevel);
                    if (s.Essence > upCost * 2)
                    {
                        s.Essence -= upCost;
                        s.ClickPowerLevel++;
                    }

                    // 4) Comprar generadores mientras alcance (el más barato primero)
                    for (int buys = 0; buys < 25; buys++)
                    {
                        GeneratorDef best = null;
                        double bestCost = double.MaxValue;
                        foreach (var g in GeneratorCatalog.Generators)
                        {
                            double cost = GameRules.GeneratorCost(g.BaseCost,
                                s.GeneratorsOwned.TryGetValue(g.Id, out var n) ? n : 0);
                            if (cost <= s.Essence && cost < bestCost) { best = g; bestCost = cost; }
                        }
                        if (best == null) break;
                        s.Essence -= bestCost;
                        s.GeneratorsOwned[best.Id] = (s.GeneratorsOwned.TryGetValue(best.Id, out var m) ? m : 0) + 1;
                    }
                }

                if (firstPrestigeAt < 0 && GameRules.CanPrestige(s))
                    firstPrestigeAt = t / 60.0;
            }

            Debug.Log($"[SelfTest] Sim 2h: esencia hist. {s.LifetimeEssence:E2}, " +
                      $"descubiertos {s.Discovered.Count}/20, " +
                      (firstPrestigeAt > 0 ? $"prestigio a los {firstPrestigeAt:F0} min" : "prestigio NO alcanzado"));

            Check(s.LifetimeEssence >= 1e6, "sim: 1M de esencia histórica en 2h");
            Check(s.Discovered.Count >= 14, $"sim: descubrir la mayoría del árbol (dio {s.Discovered.Count})");
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
                MissionIndex = 4,
                LastSeenUnixUtc = 1712345678,
                PlaySeconds = 3661.5,
                HighQualityMode = true,
                SoundOff = true,
                MusicVolume = 0.4f,
                SfxVolume = 0.7f,
                VibrateOn = true,
            };
            s.Add(ElementId.Oro, 12.5);
            s.Add(ElementId.Vapor, 3);
            s.GeneratorsOwned["brasero"] = 4;
            s.AchievementsUnlocked.Add("ess_1k");
            s.UpgradesOwned.Add("up_guantes");

            string json = JsonUtility.ToJson(SaveDto.From(s));
            var back = JsonUtility.FromJson<SaveDto>(json).ToState();

            Check(Math.Abs(back.Essence - s.Essence) < 1e-9, "save: essence");
            Check(Math.Abs(back.BalanceOf(ElementId.Oro) - 12.5) < 1e-9, "save: balances");
            Check(back.Discovered.Contains(ElementId.Oro) && back.Discovered.Contains(ElementId.Tierra),
                  "save: descubiertos");
            Check(back.GeneratorsOwned["brasero"] == 4, "save: generadores");
            Check(back.AchievementsUnlocked.Contains("ess_1k"), "save: logros");
            Check(back.UpgradesOwned.Contains("up_guantes"), "save: mejoras");
            Check(back.HighQualityMode && back.SoundOff && back.VibrateOn, "save: flags");
            Check(Math.Abs(back.MusicVolume - 0.4f) < 1e-5 && Math.Abs(back.SfxVolume - 0.7f) < 1e-5,
                  "save: volúmenes");
            // Un save viejo sin campos de volumen debe cargar con volumen 1
            var oldSave = JsonUtility.FromJson<SaveDto>("{\"saveVersion\":1}").ToState();
            Check(Math.Abs(oldSave.MusicVolume - 1f) < 1e-5 && Math.Abs(oldSave.SfxVolume - 1f) < 1e-5,
                  "save viejo: volumen 1 por defecto");
            Check(back.TotalClicks == 777 && back.PrestigeCount == 2 && back.ClickPowerLevel == 5,
                  "save: contadores");
            Check(Math.Abs(back.PlaySeconds - s.PlaySeconds) < 1e-9, "save: tiempo jugado");
            Check(back.MissionIndex == 4, "save: objetivo activo");
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
