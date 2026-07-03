using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Game;

namespace Athanor.EditorTools
{
    /// Prueba de interfaz REAL: entra a playmode, deja que Bootstrap construya el juego,
    /// captura cualquier error de runtime, verifica cada control y pulsa los principales.
    /// (Nació del bug v1.4-v1.9: una excepción en device dejaba la UI sin menús y ningún
    /// test lo veía porque la suite era solo de dominio.)
    ///   Unity.exe -batchmode -projectPath . -executeMethod Athanor.EditorTools.UiSmokeTest.Run
    public static class UiSmokeTest
    {
        static int failures;
        static int frames;
        static bool watchingLog;

        public static void Run()
        {
            // Sin recarga de dominio al entrar a playmode: si no, las suscripciones
            // estáticas de este script se pierden y la prueba se cuelga para siempre.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            // partida limpia y determinista
            string save = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(save)) File.Delete(save);

            Application.logMessageReceived += OnLog;
            watchingLog = true;

            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (!watchingLog) return;
            if (type == LogType.Exception || type == LogType.Error)
            {
                failures++;
                Debug.Log("[UiSmoke] ERROR EN RUNTIME: " + msg);
            }
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                frames = 0;
                EditorApplication.update += Tick;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log(failures == 0 ? "[UiSmoke] TODO OK" : $"[UiSmoke] {failures} fallas");
                EditorApplication.Exit(failures == 0 ? 0 : 1);
            }
        }

        static void Tick()
        {
            frames++;
            if (frames < 150) return; // ~2.5 s: construcción + varios Update()
            EditorApplication.update -= Tick;
            watchingLog = false; // los LogError propios de las FALLAs no deben contarse doble

            try { Assert(); }
            catch (Exception e)
            {
                failures++;
                Debug.LogError("[UiSmoke] Excepción en las verificaciones: " + e);
            }
            EditorApplication.ExitPlaymode();
        }

        static void Check(bool cond, string what)
        {
            if (cond) return;
            failures++;
            Debug.LogError("[UiSmoke] FALLA: " + what);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        static void Assert()
        {
            var canvasGo = GameObject.Find("Canvas");
            Check(canvasGo != null, "existe el Canvas");
            if (canvasGo == null) return;
            var rootT = canvasGo.transform;

            var game = GameController.Instance;
            Check(game != null, "existe GameController");

            // --- Estructura básica ---
            Check(FindDeep(rootT, "TopBar") != null, "HUD superior");
            Check(FindDeep(rootT, "Mission") != null, "banner de objetivo");
            Check(FindDeep(rootT, "Nav") != null, "dock de navegación");
            foreach (var el in new[] { "Tierra", "Agua", "Fuego", "Aire" })
                Check(FindDeep(rootT, "Chip_" + el) != null, "chip HUD de " + el);

            string[] tabKeys = { "lab", "ayudantes", "elementos", "logros", "obra", "ajustes" };
            foreach (var k in tabKeys)
            {
                var tab = FindDeep(rootT, "Tab_" + k);
                Check(tab != null && tab.GetComponent<Button>() != null, "pestaña " + k);
            }

            // --- Click al matraz ---
            var glass = FindDeep(rootT, "Glass");
            Check(glass != null && glass.GetComponent<Button>() != null, "matraz clickeable");
            long clicksBefore = game.State.TotalClicks;
            if (glass != null)
                for (int i = 0; i < 5; i++)
                    glass.GetComponent<Button>().onClick.Invoke();
            Check(game.State.TotalClicks == clicksBefore + 5, "5 clicks registrados");
            Check(game.State.BalanceOf(Athanor.Domain.ElementId.Tierra) >= 5, "el click produce Tierra");

            // --- Transmutar ---
            var transmute = FindDeep(rootT, "Transmute");
            Check(transmute != null && transmute.GetComponent<Button>() != null, "botón Transmutar");
            transmute?.GetComponent<Button>()?.onClick.Invoke();
            Check(game.State.Essence > 0, "transmutar da Esencia");

            // --- Cambio de pestañas: cada panel se activa con su botón ---
            string[] panels = { "LabPanel", "GeneratorsPanel", "ElementsPanel",
                                "AchievementsPanel", "PrestigePanel", "SettingsPanel" };
            for (int i = 0; i < tabKeys.Length; i++)
            {
                FindDeep(rootT, "Tab_" + tabKeys[i])?.GetComponent<Button>()?.onClick.Invoke();
                var panel = FindDeep(rootT, panels[i]);
                Check(panel != null && panel.gameObject.activeSelf,
                      "la pestaña " + tabKeys[i] + " muestra " + panels[i]);
                Check(panel == null || panel.childCount > 0, panels[i] + " tiene contenido");
            }

            // --- Controles clave de cada panel ---
            Check(FindDeep(rootT, "MusicSlider")?.GetComponent<Slider>() != null, "slider de música");
            Check(FindDeep(rootT, "SfxSlider")?.GetComponent<Slider>() != null, "slider de efectos");
            Check(FindDeep(rootT, "Prestige")?.GetComponent<Button>() != null, "botón de la Gran Obra");
            Check(FindDeep(rootT, "Reset")?.GetComponent<Button>() != null, "botón de borrado");
            Check(FindDeep(rootT, "Card_aprendiz") != null, "tarjeta del Aprendiz");
            Check(FindDeep(rootT, "Recipe_Barro") != null, "receta del Barro");
            Check(FindDeep(rootT, "BuffPill") != null, "píldora de buff (oculta)");

            // Volver al laboratorio
            FindDeep(rootT, "Tab_lab")?.GetComponent<Button>()?.onClick.Invoke();
            var lab = FindDeep(rootT, "LabPanel");
            Check(lab != null && lab.gameObject.activeSelf, "vuelta al laboratorio");
        }
    }
}
