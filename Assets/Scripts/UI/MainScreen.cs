using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pantalla principal: HUD, matraz clickeable y feedback (pulso, partículas, +N).
    /// Nota: solo caracteres presentes en LegacyRuntime.ttf (nada de ⚗/✦, no existen en Android).
    public sealed class MainScreen : MonoBehaviour
    {
        GameController game;
        RectTransform root;      // área segura (evita notch/gestos)
        RectTransform fxLayer;   // capa de partículas/floaters

        Text essenceText;
        readonly Dictionary<ElementId, Text> baseCounters = new Dictionary<ElementId, Text>();
        RectTransform flask;
        Text transmuteLabel;
        Button transmuteButton;
        Text upgradeLabel;
        Button upgradeButton;

        Coroutine pulse;
        int floatersAlive;
        const int MaxFloaters = 24;

        static readonly ElementId[] BaseElements =
            { ElementId.Tierra, ElementId.Agua, ElementId.Fuego, ElementId.Aire };

        public void Build(RectTransform safeRoot)
        {
            root = safeRoot;
            game = GameController.Instance;
            game.StateChanged += Refresh;

            BuildTopBar();
            BuildFlask();
            BuildBottomBar();

            fxLayer = Ui.Rect("FxLayer", root);
            Ui.Fill(fxLayer);

            Refresh();
        }

        void OnDestroy()
        {
            if (game != null) game.StateChanged -= Refresh;
        }

        // ---- Construcción ----

        void BuildTopBar()
        {
            var bar = Ui.Panel("TopBar", root, UiTheme.Panel);
            Ui.Anchor(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(1020, 280));

            // Icono de Esencia: círculo dorado a la izquierda del número
            var essenceIcon = Ui.Panel("EssenceIcon", bar.transform, UiTheme.Gold);
            essenceIcon.sprite = UiTheme.Circle();
            essenceIcon.type = Image.Type.Simple;
            Ui.Anchor(essenceIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(-210, -55), new Vector2(52, 52));

            essenceText = Ui.Label("Essence", bar.transform, "0", 78, UiTheme.Gold,
                                   TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(essenceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(30, -55), new Vector2(400, 90));

            var subtitle = Ui.Label("EssenceCaption", bar.transform, Loc.T("ui_esencia"), 34,
                                    UiTheme.TextDim);
            Ui.Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(1000, 40));

            // 4 columnas: punto de color arriba, cantidad debajo (centrado, sin desbordes)
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

        void BuildFlask()
        {
            // Aro exterior tenue (círculo de transmutación placeholder)
            var ring = Ui.Panel("Ring", root, new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.16f));
            ring.sprite = UiTheme.Circle();
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            Ui.Place(ring.rectTransform, 0, 40, 640, 640);

            // Matraz placeholder compuesto por formas planas (sin glifos de fuente)
            var flaskImg = Ui.Panel("Flask", root, UiTheme.Amber);
            flaskImg.sprite = UiTheme.Circle();
            flaskImg.type = Image.Type.Simple;
            Ui.Place(flaskImg.rectTransform, 0, 40, 500, 500);
            flask = flaskImg.rectTransform;

            var inner = Ui.Panel("FlaskInner", flask, UiTheme.Background);
            inner.sprite = UiTheme.Circle();
            inner.type = Image.Type.Simple;
            inner.raycastTarget = false;
            Ui.Place(inner.rectTransform, 0, -20, 380, 380);

            var liquid = Ui.Panel("FlaskLiquid", flask, UiTheme.Violet);
            liquid.sprite = UiTheme.Circle();
            liquid.type = Image.Type.Simple;
            liquid.raycastTarget = false;
            Ui.Place(liquid.rectTransform, 0, -55, 280, 280);

            var bubble1 = Ui.Panel("Bubble1", flask, new Color(1f, 1f, 1f, 0.35f));
            bubble1.sprite = UiTheme.Circle();
            bubble1.type = Image.Type.Simple;
            bubble1.raycastTarget = false;
            Ui.Place(bubble1.rectTransform, -50, 10, 46, 46);

            var bubble2 = Ui.Panel("Bubble2", flask, new Color(1f, 1f, 1f, 0.25f));
            bubble2.sprite = UiTheme.Circle();
            bubble2.type = Image.Type.Simple;
            bubble2.raycastTarget = false;
            Ui.Place(bubble2.rectTransform, 35, 60, 28, 28);

            var btn = flaskImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = flaskImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnFlaskClicked);
        }

        void BuildBottomBar()
        {
            transmuteButton = Ui.TextButton("Transmute", root, UiTheme.Gold, out transmuteLabel);
            Ui.Anchor((RectTransform)transmuteButton.transform, new Vector2(0.5f, 0f), new Vector2(-260, 56), new Vector2(490, 150));
            transmuteButton.onClick.AddListener(OnTransmuteClicked);

            upgradeButton = Ui.TextButton("Upgrade", root, UiTheme.Green, out upgradeLabel);
            Ui.Anchor((RectTransform)upgradeButton.transform, new Vector2(0.5f, 0f), new Vector2(260, 56), new Vector2(490, 150));
            upgradeButton.onClick.AddListener(() => game.BuyClickUpgrade());

            var version = Ui.Label("Version", root, "v" + GameVersion.Version, 26, UiTheme.TextDim);
            Ui.Anchor(version.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(400, 30));
        }

        // ---- Acciones ----

        void OnFlaskClicked()
        {
            double yield = game.ClickFlask();

            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(PulseFlask());

            if (floatersAlive < MaxFloaters)
            {
                // Blanco neutro: son materiales, no Esencia
                StartCoroutine(FloatingText("+" + NumberFormat.Fmt(yield), UiTheme.TextMain));
                StartCoroutine(ParticleBurst());
            }
        }

        void OnTransmuteClicked()
        {
            double gained = game.TransmuteAll();
            if (gained > 0 && floatersAlive < MaxFloaters)
                StartCoroutine(FloatingText("+" + NumberFormat.Fmt(gained) + " " + Loc.T("ui_esencia"), UiTheme.Gold));
        }

        // ---- Refresh ----

        void Refresh()
        {
            var s = game.State;
            essenceText.text = NumberFormat.Fmt(s.Essence);

            foreach (var kv in baseCounters)
                kv.Value.text = NumberFormat.Fmt(s.BalanceOf(kv.Key));

            double sellValue = 0;
            foreach (var def in ElementCatalog.Elements)
                sellValue += s.BalanceOf(def.Id) * def.EssenceValue;
            transmuteLabel.text = Loc.T("ui_transmutar_todo") + "\n+" + NumberFormat.Fmt(sellValue) + " " + Loc.T("ui_esencia");
            transmuteButton.interactable = sellValue > 0;

            upgradeLabel.text = Loc.T("ui_poder_click") + " " + Loc.T("ui_nivel") + (s.ClickPowerLevel + 1)
                + "\n" + Loc.T("ui_coste") + ": " + NumberFormat.Fmt(game.ClickUpgradeCost);
            upgradeButton.interactable = game.CanBuyClickUpgrade;
        }

        // ---- Feedback visual ----

        IEnumerator PulseFlask()
        {
            const float duration = 0.12f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI); // 0→1→0
                flask.localScale = Vector3.one * (1f + 0.08f * k);
                yield return null;
            }
            flask.localScale = Vector3.one;
        }

        IEnumerator FloatingText(string content, Color color)
        {
            floatersAlive++;
            var label = Ui.Label("Floater", fxLayer, content, 54, color,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            float dx = Random.Range(-70f, 70f);
            Ui.Place(label.rectTransform, dx, 320, 500, 70);

            const float duration = 0.45f;
            Color c = label.color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                label.rectTransform.anchoredPosition = new Vector2(dx, 320 + 150 * k);
                c.a = 1f - k * k;
                label.color = c;
                yield return null;
            }
            Destroy(label.gameObject);
            floatersAlive--;
        }

        IEnumerator ParticleBurst()
        {
            // Partículas UI planas con los colores de los 4 elementos base.
            int count = game.State.HighQualityMode ? 10 : 5;
            var parts = new List<(RectTransform rt, Image img, Vector2 dir)>();
            for (int i = 0; i < count; i++)
            {
                var def = ElementCatalog.Get(BaseElements[i % BaseElements.Length]);
                var img = Ui.Panel("P", fxLayer, UiTheme.ElementColor(def.ColorHex), rounded: false);
                img.raycastTarget = false;
                float ang = Random.Range(0f, Mathf.PI * 2);
                Ui.Place(img.rectTransform, 0, 40, 24, 24);
                parts.Add((img.rectTransform, img, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))));
            }

            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float dist = 120 + 220 * k;
                foreach (var p in parts)
                {
                    p.rt.anchoredPosition = new Vector2(p.dir.x * dist, 40 + p.dir.y * dist);
                    var c = p.img.color;
                    c.a = 1f - k;
                    p.img.color = c;
                }
                yield return null;
            }
            foreach (var p in parts) Destroy(p.rt.gameObject);
        }
    }
}
