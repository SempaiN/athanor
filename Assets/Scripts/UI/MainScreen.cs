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
    public sealed class MainScreen : MonoBehaviour
    {
        GameController game;
        Canvas canvas;

        Text essenceText;
        readonly Dictionary<ElementId, Text> baseCounters = new Dictionary<ElementId, Text>();
        RectTransform flask;
        Image flaskImage;
        Text transmuteLabel;
        Button transmuteButton;
        Text upgradeLabel;
        Button upgradeButton;

        Coroutine pulse;
        int floatersAlive;
        const int MaxFloaters = 24;

        static readonly ElementId[] BaseElements =
            { ElementId.Tierra, ElementId.Agua, ElementId.Fuego, ElementId.Aire };

        public void Build(Canvas rootCanvas)
        {
            canvas = rootCanvas;
            game = GameController.Instance;
            game.StateChanged += Refresh;

            BuildTopBar();
            BuildFlask();
            BuildBottomBar();
            Refresh();
        }

        void OnDestroy()
        {
            if (game != null) game.StateChanged -= Refresh;
        }

        // ---- Construcción ----

        void BuildTopBar()
        {
            var bar = Ui.Panel("TopBar", canvas.transform, UiTheme.Panel);
            Ui.Anchor(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(1020, 250));

            essenceText = Ui.Label("Essence", bar.transform, "", 76, UiTheme.Gold,
                                   TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(essenceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(1000, 90));

            var subtitle = Ui.Label("EssenceCaption", bar.transform, Loc.T("ui_esencia"), 34,
                                    UiTheme.TextDim);
            Ui.Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -104), new Vector2(1000, 40));

            // Contadores de los 4 elementos base
            float slot = 1020f / BaseElements.Length;
            for (int i = 0; i < BaseElements.Length; i++)
            {
                var el = BaseElements[i];
                var def = ElementCatalog.Get(el);
                float cx = -510 + slot * (i + 0.5f);

                var dot = Ui.Panel("Dot_" + el, bar.transform, UiTheme.ElementColor(def.ColorHex));
                dot.sprite = UiTheme.Circle();
                dot.type = Image.Type.Simple;
                Ui.Anchor(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(cx - 62, 52), new Vector2(34, 34));

                var counter = Ui.Label("Count_" + el, bar.transform, "0", 38, UiTheme.TextMain,
                                       TextAnchor.MiddleLeft);
                Ui.Anchor(counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(cx - 34, 52), new Vector2(150, 44));
                baseCounters[el] = counter;
            }
        }

        void BuildFlask()
        {
            // Aro exterior (círculo de transmutación placeholder)
            var ring = Ui.Panel("Ring", canvas.transform, new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.18f));
            ring.sprite = UiTheme.Circle();
            ring.type = Image.Type.Simple;
            Ui.Place(ring.rectTransform, 0, 60, 640, 640);

            var flaskImg = Ui.Panel("Flask", canvas.transform, UiTheme.Amber);
            flaskImg.sprite = UiTheme.Circle();
            flaskImg.type = Image.Type.Simple;
            Ui.Place(flaskImg.rectTransform, 0, 60, 520, 520);
            flask = flaskImg.rectTransform;
            flaskImage = flaskImg;

            var icon = Ui.Label("FlaskGlyph", flask, "⚗", 200, UiTheme.Background);
            Ui.Fill(icon.rectTransform);

            var btn = flaskImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = flaskImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnFlaskClicked);
        }

        void BuildBottomBar()
        {
            transmuteButton = Ui.TextButton("Transmute", canvas.transform, UiTheme.Gold, out transmuteLabel);
            Ui.Anchor(((RectTransform)transmuteButton.transform), new Vector2(0.5f, 0f), new Vector2(-260, 60), new Vector2(490, 150));
            transmuteButton.onClick.AddListener(OnTransmuteClicked);

            upgradeButton = Ui.TextButton("Upgrade", canvas.transform, UiTheme.Green, out upgradeLabel);
            Ui.Anchor(((RectTransform)upgradeButton.transform), new Vector2(0.5f, 0f), new Vector2(260, 60), new Vector2(490, 150));
            upgradeButton.onClick.AddListener(() => game.BuyClickUpgrade());

            var version = Ui.Label("Version", canvas.transform, "v" + GameVersion.Version, 26, UiTheme.TextDim);
            Ui.Anchor(version.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(400, 30));
        }

        // ---- Acciones ----

        void OnFlaskClicked()
        {
            double yield = game.ClickFlask();

            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(PulseFlask());

            if (floatersAlive < MaxFloaters)
            {
                StartCoroutine(FloatingText("+" + NumberFormat.Fmt(yield)));
                StartCoroutine(ParticleBurst());
            }
        }

        void OnTransmuteClicked()
        {
            double gained = game.TransmuteAll();
            if (gained > 0 && floatersAlive < MaxFloaters)
                StartCoroutine(FloatingText("+" + NumberFormat.Fmt(gained) + " ✦"));
        }

        // ---- Refresh ----

        void Refresh()
        {
            var s = game.State;
            essenceText.text = "✦ " + NumberFormat.Fmt(s.Essence);

            foreach (var kv in baseCounters)
                kv.Value.text = NumberFormat.Fmt(s.BalanceOf(kv.Key));

            double sellValue = 0;
            foreach (var def in ElementCatalog.Elements)
                sellValue += s.BalanceOf(def.Id) * def.EssenceValue;
            transmuteLabel.text = Loc.T("ui_transmutar_todo") + "\n+" + NumberFormat.Fmt(sellValue) + " ✦";
            transmuteButton.interactable = sellValue > 0;

            upgradeLabel.text = Loc.T("ui_poder_click") + " " + Loc.T("ui_nivel") + (s.ClickPowerLevel + 1)
                + "\n" + Loc.T("ui_coste") + ": " + NumberFormat.Fmt(game.ClickUpgradeCost) + " ✦";
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

        IEnumerator FloatingText(string content)
        {
            floatersAlive++;
            var label = Ui.Label("Floater", canvas.transform, content, 56, UiTheme.Gold,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            float dx = Random.Range(-70f, 70f);
            Ui.Place(label.rectTransform, dx, 330, 400, 70);

            const float duration = 0.45f;
            Color c = label.color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                label.rectTransform.anchoredPosition = new Vector2(dx, 330 + 150 * k);
                c.a = 1f - k * k;
                label.color = c;
                yield return null;
            }
            Destroy(label.gameObject);
            floatersAlive--;
        }

        IEnumerator ParticleBurst()
        {
            // Partículas UI planas: cuadraditos con los colores de los 4 elementos base.
            int count = game.State.HighQualityMode ? 10 : 5;
            var parts = new List<(RectTransform rt, Image img, Vector2 dir)>();
            for (int i = 0; i < count; i++)
            {
                var def = ElementCatalog.Get(BaseElements[i % BaseElements.Length]);
                var img = Ui.Panel("P", canvas.transform, UiTheme.ElementColor(def.ColorHex), rounded: false);
                img.raycastTarget = false;
                float ang = Random.Range(0f, Mathf.PI * 2);
                Ui.Place(img.rectTransform, 0, 60, 26, 26);
                parts.Add((img.rectTransform, img, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))));
            }

            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float dist = 120 + 220 * k;
                foreach (var p in parts)
                {
                    p.rt.anchoredPosition = new Vector2(p.dir.x * dist, 60 + p.dir.y * dist);
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
