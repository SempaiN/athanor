using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Laboratorio: matraz clickeable, feedback y acciones rápidas.
    public sealed class LabPanel
    {
        readonly MonoBehaviour host; // para coroutines de FX
        GameController game;
        public RectTransform Root { get; private set; }

        RectTransform flask;
        RectTransform fxLayer;
        Text transmuteLabel;
        Button transmuteButton;
        Text upgradeLabel;
        Button upgradeButton;

        Coroutine pulse;
        int floatersAlive;
        const int MaxFloaters = 24;

        static readonly ElementId[] BaseElements =
            { ElementId.Tierra, ElementId.Agua, ElementId.Fuego, ElementId.Aire };

        public LabPanel(MonoBehaviour host) { this.host = host; }

        LabDecor decor;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("LabPanel", parent);
            Ui.Fill(Root);

            BuildFlask();
            BuildButtons();

            decor = new LabDecor();
            decor.Build(Root);

            fxLayer = Ui.Rect("FxLayer", Root);
            Ui.Fill(fxLayer);
        }

        Image ring;
        RectTransform liquidRt;
        float breathePhase;
        float bubbleTimer;

        /// Animaciones idle; lo llama MainScreen.Update solo cuando esta pestaña está activa.
        public void Tick(float dt)
        {
            breathePhase += dt;
            // El matraz "respira" y el aro pulsa su brillo suavemente
            if (flask != null && pulse == null)
                flask.localScale = Vector3.one * (1f + 0.012f * Mathf.Sin(breathePhase * 1.6f));
            if (ring != null)
            {
                var c = ring.color;
                c.a = 0.13f + 0.06f * (0.5f + 0.5f * Mathf.Sin(breathePhase * 1.1f));
                ring.color = c;
            }
            if (liquidRt != null)
                liquidRt.anchoredPosition = new Vector2(0, -55 + 4f * Mathf.Sin(breathePhase * 2.3f));

            bubbleTimer += dt;
            float every = game.State.HighQualityMode ? 0.9f : 1.6f;
            if (bubbleTimer >= every)
            {
                bubbleTimer = 0;
                host.StartCoroutine(RisingBubble());
            }
        }

        IEnumerator RisingBubble()
        {
            var b = Ui.Panel("IdleBubble", flask, new Color(1f, 1f, 1f, 0.0f));
            b.sprite = UiTheme.Circle();
            b.type = Image.Type.Simple;
            b.raycastTarget = false;
            float x = Random.Range(-70f, 70f);
            float size = Random.Range(14f, 30f);
            Ui.Place(b.rectTransform, x, -120, size, size);

            const float duration = 1.6f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (b == null) yield break;
                float k = t / duration;
                b.rectTransform.anchoredPosition = new Vector2(x + 8f * Mathf.Sin(k * 9f), -120 + 200 * k);
                float a = k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f;
                b.color = new Color(1f, 1f, 1f, 0.30f * a);
                yield return null;
            }
            if (b != null) Object.Destroy(b.gameObject);
        }

        void BuildFlask()
        {
            var ringImg = Ui.Panel("Ring", Root, new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.16f));
            ringImg.sprite = UiTheme.Circle();
            ringImg.type = Image.Type.Simple;
            ringImg.raycastTarget = false;
            Ui.Place(ringImg.rectTransform, 0, 90, 620, 620);
            ring = ringImg;

            var flaskImg = Ui.Panel("Flask", Root, UiTheme.Amber);
            flaskImg.sprite = UiTheme.Circle();
            flaskImg.type = Image.Type.Simple;
            Ui.Place(flaskImg.rectTransform, 0, 90, 490, 490);
            flask = flaskImg.rectTransform;

            var inner = Ui.Panel("FlaskInner", flask, UiTheme.Background);
            inner.sprite = UiTheme.Circle();
            inner.type = Image.Type.Simple;
            inner.raycastTarget = false;
            Ui.Place(inner.rectTransform, 0, -20, 375, 375);

            var liquid = Ui.Panel("FlaskLiquid", flask, UiTheme.Violet);
            liquid.sprite = UiTheme.Circle();
            liquid.type = Image.Type.Simple;
            liquid.raycastTarget = false;
            Ui.Place(liquid.rectTransform, 0, -55, 275, 275);
            liquidRt = liquid.rectTransform;

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

        void BuildButtons()
        {
            transmuteButton = Ui.TextButton("Transmute", Root, UiTheme.Gold, out transmuteLabel);
            Ui.Anchor((RectTransform)transmuteButton.transform, new Vector2(0.5f, 0f), new Vector2(-260, 24), new Vector2(490, 150));
            transmuteButton.onClick.AddListener(OnTransmuteClicked);

            upgradeButton = Ui.TextButton("Upgrade", Root, UiTheme.Green, out upgradeLabel);
            Ui.Anchor((RectTransform)upgradeButton.transform, new Vector2(0.5f, 0f), new Vector2(260, 24), new Vector2(490, 150));
            upgradeButton.onClick.AddListener(() => game.BuyClickUpgrade());
        }

        // ---- Acciones ----

        void OnFlaskClicked()
        {
            double yield = game.ClickFlask();

            if (pulse != null) host.StopCoroutine(pulse);
            pulse = host.StartCoroutine(PulseFlask());

            if (floatersAlive < MaxFloaters)
            {
                host.StartCoroutine(FloatingText("+" + NumberFormat.Fmt(yield), UiTheme.TextMain));
                host.StartCoroutine(ParticleBurst());
            }
        }

        void OnTransmuteClicked()
        {
            double gained = game.TransmuteBasics();
            if (gained > 0 && floatersAlive < MaxFloaters)
                host.StartCoroutine(FloatingText("+" + NumberFormat.Fmt(gained) + " " + Loc.T("ui_esencia"), UiTheme.Gold));
        }

        // ---- Refresh ----

        public void Refresh()
        {
            decor.Refresh();
            var s = game.State;
            double sellValue = 0;
            foreach (var def in ElementCatalog.Elements)
                if (def.Tier == 0)
                    sellValue += s.BalanceOf(def.Id) * def.EssenceValue;

            transmuteLabel.text = Loc.T("ui_transmutar_todo") + "\n+" + NumberFormat.Fmt(sellValue) + " " + Loc.T("ui_esencia");
            transmuteButton.interactable = sellValue > 0;

            upgradeLabel.text = Loc.T("ui_poder_click") + " " + Loc.T("ui_nivel") + (s.ClickPowerLevel + 1)
                + "\n" + Loc.T("ui_coste") + ": " + NumberFormat.Fmt(game.ClickUpgradeCost);
            upgradeButton.interactable = game.CanBuyClickUpgrade;
        }

        // ---- FX ----

        IEnumerator PulseFlask()
        {
            const float duration = 0.12f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                flask.localScale = Vector3.one * (1f + 0.08f * k);
                yield return null;
            }
            flask.localScale = Vector3.one;
            pulse = null; // libera el idle (la respiración del matraz)
        }

        IEnumerator FloatingText(string content, Color color)
        {
            floatersAlive++;
            var label = Ui.Label("Floater", fxLayer, content, 54, color,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            float dx = Random.Range(-70f, 70f);
            Ui.Place(label.rectTransform, dx, 370, 500, 70);

            const float duration = 0.45f;
            Color c = label.color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                label.rectTransform.anchoredPosition = new Vector2(dx, 370 + 150 * k);
                c.a = 1f - k * k;
                label.color = c;
                yield return null;
            }
            Object.Destroy(label.gameObject);
            floatersAlive--;
        }

        IEnumerator ParticleBurst()
        {
            int count = game.State.HighQualityMode ? 10 : 5;
            var parts = new List<(RectTransform rt, Image img, Vector2 dir)>();
            for (int i = 0; i < count; i++)
            {
                var def = ElementCatalog.Get(BaseElements[i % BaseElements.Length]);
                var img = Ui.Panel("P", fxLayer, UiTheme.ElementColor(def.ColorHex), rounded: false);
                img.raycastTarget = false;
                float ang = Random.Range(0f, Mathf.PI * 2);
                Ui.Place(img.rectTransform, 0, 90, 24, 24);
                parts.Add((img.rectTransform, img, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))));
            }

            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float dist = 120 + 220 * k;
                foreach (var p in parts)
                {
                    p.rt.anchoredPosition = new Vector2(p.dir.x * dist, 90 + p.dir.y * dist);
                    var c = p.img.color;
                    c.a = 1f - k;
                    p.img.color = c;
                }
                yield return null;
            }
            foreach (var p in parts) Object.Destroy(p.rt.gameObject);
        }
    }
}
