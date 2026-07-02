using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Orquestador de la UI: HUD superior, pestañas inferiores y paneles.
    public sealed class MainScreen : MonoBehaviour
    {
        GameController game;
        RectTransform root;
        RectTransform contentArea;

        Text essenceText;
        Text essenceCaption;
        readonly Dictionary<ElementId, Text> baseCounters = new Dictionary<ElementId, Text>();

        LabPanel lab;
        GeneratorsPanel generators;
        ElementsPanel elements;
        AchievementsPanel achievements;
        PrestigePanel prestige;
        SettingsPanel settings;

        sealed class TabEntry
        {
            public string Key;
            public RectTransform Panel;
            public Image Bg;
            public Image Icon;
            public Color IconColor;
            public Text Label;
        }

        readonly List<TabEntry> tabs = new List<TabEntry>();

        const float TopBarH = 280;
        const float NavH = 150;

        static readonly ElementId[] BaseElements =
            { ElementId.Tierra, ElementId.Agua, ElementId.Fuego, ElementId.Aire };

        public void Build(RectTransform safeRoot)
        {
            root = safeRoot;
            game = GameController.Instance;
            game.StateChanged += Refresh;

            BuildTopBar();

            // Zona de contenido entre HUD y pestañas
            contentArea = Ui.Rect("Content", root);
            contentArea.anchorMin = new Vector2(0, 0);
            contentArea.anchorMax = new Vector2(1, 1);
            contentArea.offsetMin = new Vector2(0, NavH);
            contentArea.offsetMax = new Vector2(0, -TopBarH - 20);

            lab = new LabPanel(this);
            lab.Build(contentArea);

            generators = new GeneratorsPanel();
            generators.Build(contentArea);

            elements = new ElementsPanel();
            elements.Build(contentArea);

            achievements = new AchievementsPanel();
            achievements.Build(contentArea);

            prestige = new PrestigePanel(this);
            prestige.Build(contentArea);

            settings = new SettingsPanel();
            settings.Build(contentArea);

            BuildNav();
            ShowTab("lab");
            game.ElementDiscovered += OnElementDiscovered;
            game.AchievementUnlocked += OnAchievementUnlocked;
            game.Prestiged += OnPrestiged;
            game.OfflineGranted += ShowOfflinePopup;
            game.MissionCompleted += OnMissionCompleted;
            Refresh();

            if (game.OfflineGain > 0) ShowOfflinePopup(game.OfflineGain);
        }

        void Update()
        {
            if (lab != null && lab.Root.gameObject.activeSelf)
                lab.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.StateChanged -= Refresh;
                game.ElementDiscovered -= OnElementDiscovered;
                game.AchievementUnlocked -= OnAchievementUnlocked;
                game.Prestiged -= OnPrestiged;
                game.OfflineGranted -= ShowOfflinePopup;
                game.MissionCompleted -= OnMissionCompleted;
            }
        }

        void OnMissionCompleted(MissionDef def)
        {
            StartCoroutine(DiscoveryToast(
                Loc.T("ui_objetivo_ok") + " +" + NumberFormat.Fmt(def.Reward) + " " + Loc.T("ui_esencia"),
                UiTheme.Green));
        }

        void OnAchievementUnlocked(AchievementDef def)
        {
            StartCoroutine(DiscoveryToast(Loc.T("ui_logro") + " " + def.Name + " (+" +
                Mathf.RoundToInt((float)(def.Bonus * 100)) + "%)", UiTheme.Gold));
        }

        void OnPrestiged()
        {
            ShowTab("lab");
            StartCoroutine(DiscoveryToast(Loc.T("ui_prestigio_boton") + " +10%/" +
                Loc.T("ui_quintaesencia"), UiTheme.Violet));
        }

        void ShowOfflinePopup(double gained)
        {
            var veil = Ui.Panel("OfflineVeil", root, new Color(0, 0, 0, 0.65f), rounded: false);
            Ui.Fill(veil.rectTransform);

            var card = Ui.Panel("OfflineCard", veil.transform, UiTheme.Card);
            Ui.Place(card.rectTransform, 0, 0, 860, 460);
            StartCoroutine(UiFx.PopIn(card.rectTransform));

            var title = Ui.Label("Title", card.transform, Loc.T("ui_offline_titulo"), 46,
                                 UiTheme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(800, 60));

            var body = Ui.Label("Body", card.transform,
                Loc.T("ui_offline_texto") + "\n+" + NumberFormat.Fmt(gained) + " " + Loc.T("ui_esencia"),
                40, UiTheme.Gold);
            Ui.Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(780, 140));

            var ok = Ui.TextButton("Ok", card.transform, UiTheme.Gold, out var okLabel);
            okLabel.text = Loc.T("ui_ok");
            Ui.Anchor((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(480, 110));
            ok.onClick.AddListener(() => Destroy(veil.gameObject));
        }

        void OnElementDiscovered(ElementId id)
        {
            var def = ElementCatalog.Get(id);
            StartCoroutine(DiscoveryToast(Loc.T("ui_nuevo_elemento") + " " + Loc.T(def.NameKey),
                                          UiTheme.ElementColor(def.ColorHex)));
        }

        System.Collections.IEnumerator PopEssence()
        {
            var rt = essenceText.rectTransform;
            const float duration = 0.18f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                rt.localScale = Vector3.one * (1f + 0.12f * k);
                yield return null;
            }
            rt.localScale = Vector3.one;
            essencePop = null;
        }

        int toastsAlive;

        System.Collections.IEnumerator DiscoveryToast(string message, Color accent)
        {
            float slotOffset = (toastsAlive % 3) * 112f; // hasta 3 apilados sin taparse
            toastsAlive++;
            var card = Ui.Panel("Toast", root, UiTheme.Card);
            float baseY = -TopBarH - 44 - slotOffset;
            Ui.Anchor(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, baseY), new Vector2(760, 100));

            var stripe = Ui.Panel("Stripe", card.transform, accent, rounded: false);
            stripe.raycastTarget = false;
            Ui.Anchor(stripe.rectTransform, new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(12, 100));

            var dot = Ui.Panel("Dot", card.transform, accent);
            dot.sprite = UiTheme.Circle();
            dot.type = Image.Type.Simple;
            Ui.Anchor(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(44, 44));

            var label = Ui.Label("Msg", card.transform, message, 34, UiTheme.TextMain,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Fill(label.rectTransform);

            // Desliza hacia abajo al entrar, se desvanece al salir
            var cg = card.gameObject.AddComponent<CanvasGroup>();
            const float inDur = 0.18f;
            for (float t = 0; t < inDur; t += Time.deltaTime)
            {
                float k = t / inDur;
                card.rectTransform.anchoredPosition = new Vector2(0, baseY + 40 * (1 - k));
                cg.alpha = k;
                yield return null;
            }
            cg.alpha = 1;
            card.rectTransform.anchoredPosition = new Vector2(0, baseY);

            yield return new WaitForSeconds(2.0f);

            const float outDur = 0.25f;
            for (float t = 0; t < outDur; t += Time.deltaTime)
            {
                cg.alpha = 1 - t / outDur;
                yield return null;
            }
            toastsAlive--;
            Destroy(card.gameObject);
        }

        // ---- HUD superior ----

        void BuildTopBar()
        {
            var bar = Ui.Panel("TopBar", root, UiTheme.Panel);
            Ui.Anchor(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(1020, TopBarH));

            // Halo dorado suave detrás del contador principal
            var glow = Ui.Panel("EssenceGlow", bar.transform,
                new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.10f));
            glow.raycastTarget = false;
            Ui.Anchor(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(560, 118));

            var essenceIcon = Ui.Panel("EssenceIcon", bar.transform, UiTheme.Gold);
            essenceIcon.sprite = UiTheme.Circle();
            essenceIcon.type = Image.Type.Simple;
            Ui.Anchor(essenceIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(-210, -55), new Vector2(52, 52));

            essenceText = Ui.Label("Essence", bar.transform, "0", 78, UiTheme.Gold,
                                   TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(essenceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(30, -55), new Vector2(400, 90));

            essenceCaption = Ui.Label("EssenceCaption", bar.transform, Loc.T("ui_esencia"), 34,
                                      UiTheme.TextDim);
            Ui.Anchor(essenceCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(1000, 40));

            float slot = 1020f / BaseElements.Length;
            for (int i = 0; i < BaseElements.Length; i++)
            {
                var el = BaseElements[i];
                var def = ElementCatalog.Get(el);
                float cx = -510 + slot * (i + 0.5f);

                var dot = Ui.Panel("Dot_" + el, bar.transform, UiTheme.ElementColor(def.ColorHex));
                dot.sprite = UiTheme.Circle();
                dot.type = Image.Type.Simple;
                Ui.Anchor(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(cx, 96), new Vector2(36, 36));

                var counter = Ui.Label("Count_" + el, bar.transform, "0", 38, UiTheme.TextMain,
                                       TextAnchor.MiddleCenter);
                Ui.Anchor(counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(cx, 44), new Vector2(slot - 10, 44));
                baseCounters[el] = counter;
            }
        }

        // ---- Pestañas ----

        void BuildNav()
        {
            var nav = Ui.Panel("Nav", root, UiTheme.Panel);
            Ui.Anchor(nav.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(1060, NavH - 10));

            RegisterTab(nav.transform, "lab", lab.Root);
            RegisterTab(nav.transform, "ayudantes", generators.Root);
            RegisterTab(nav.transform, "elementos", elements.Root);
            RegisterTab(nav.transform, "logros", achievements.Root);
            RegisterTab(nav.transform, "obra", prestige.Root);
            RegisterTab(nav.transform, "ajustes", settings.Root);
            LayoutTabs(nav.rectTransform);
        }

        void RegisterTab(Transform navBar, string key, RectTransform panel)
        {
            var bg = Ui.Panel("Tab_" + key, navBar, UiTheme.Card);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            bg.gameObject.AddComponent<ButtonJuice>();
            string k = key;
            btn.onClick.AddListener(() => ShowTab(k));

            // Iconito de forma+color propio por pestaña (flat, sin fuentes)
            (Color color, bool diamond) icon = key switch
            {
                "lab" => (UiTheme.Amber, false),
                "ayudantes" => (UiTheme.Green, false),
                "elementos" => (UiTheme.Hex("#3D9BB3"), true),
                "logros" => (UiTheme.Gold, false),
                "obra" => (UiTheme.Violet, true),
                _ => (UiTheme.TextDim, false),
            };
            var iconImg = Ui.Panel("Icon", bg.transform, icon.color);
            iconImg.sprite = key == "lab" || key == "logros" ? UiTheme.Circle() : UiTheme.RoundedRect();
            iconImg.type = Image.Type.Simple;
            iconImg.raycastTarget = false;
            Ui.Anchor(iconImg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(30, 30));
            if (icon.diamond)
                iconImg.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            var label = Ui.Label("Label", bg.transform, Loc.T("ui_tab_" + key), 22,
                                 UiTheme.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(170, 32));

            tabs.Add(new TabEntry
            {
                Key = key, Panel = panel, Bg = bg,
                Icon = iconImg, IconColor = icon.color, Label = label,
            });
        }

        void LayoutTabs(RectTransform nav)
        {
            float w = 1040f / tabs.Count;
            for (int i = 0; i < tabs.Count; i++)
            {
                var rt = tabs[i].Bg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(10 + i * w, 0);
                rt.sizeDelta = new Vector2(w - 10, NavH - 34);
            }
        }

        void ShowTab(string key)
        {
            foreach (var t in tabs)
            {
                bool active = t.Key == key;
                bool wasActive = t.Panel.gameObject.activeSelf;
                t.Panel.gameObject.SetActive(active);
                t.Bg.color = active ? UiTheme.Amber : UiTheme.Card;
                t.Label.color = active ? UiTheme.Background : UiTheme.TextMain;
                t.Icon.color = active ? UiTheme.Background : t.IconColor;

                if (active && !wasActive)
                {
                    var cg = t.Panel.GetComponent<CanvasGroup>();
                    if (cg == null) cg = t.Panel.gameObject.AddComponent<CanvasGroup>();
                    StartCoroutine(UiFx.FadeIn(cg));
                }
            }
            Refresh();
        }

        // ---- Refresh ----

        double lastEssenceShown;
        Coroutine essencePop;

        void Refresh()
        {
            var s = game.State;
            essenceText.text = NumberFormat.Fmt(s.Essence);

            // "Pop" del contador cuando entra un buen chorro de esencia
            if (s.Essence > lastEssenceShown &&
                s.Essence - lastEssenceShown > System.Math.Max(5, lastEssenceShown * 0.02))
            {
                if (essencePop != null) StopCoroutine(essencePop);
                essencePop = StartCoroutine(PopEssence());
            }
            lastEssenceShown = s.Essence;

            double eps = game.EssencePerSecond();
            essenceCaption.text = eps > 0
                ? Loc.T("ui_esencia") + "  (+" + NumberFormat.Fmt(eps) + "/s en materiales)"
                : Loc.T("ui_esencia");

            foreach (var kv in baseCounters)
                kv.Value.text = NumberFormat.Fmt(s.BalanceOf(kv.Key));

            if (lab.Root.gameObject.activeSelf) lab.Refresh();
            if (generators.Root.gameObject.activeSelf) generators.Refresh();
            if (elements.Root.gameObject.activeSelf) elements.Refresh();
            if (achievements.Root.gameObject.activeSelf) achievements.Refresh();
            if (prestige.Root.gameObject.activeSelf) prestige.Refresh();
            if (settings.Root.gameObject.activeSelf) settings.Refresh();
        }
    }
}
