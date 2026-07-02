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
        GameObject missionCard;
        Text missionText, missionReward;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("LabPanel", parent);
            Ui.Fill(Root);

            BuildFlask();
            BuildButtons();
            BuildMissionBanner();

            decor = new LabDecor();
            decor.Build(Root);

            fxLayer = Ui.Rect("FxLayer", Root);
            Ui.Fill(fxLayer);
        }

        void BuildMissionBanner()
        {
            var card = Ui.Panel("Mission", Root, UiTheme.Card);
            Ui.Anchor(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(1010, 92));
            missionCard = card.gameObject;

            var icon = Ui.Panel("Icon", card.transform, UiTheme.Amber);
            icon.raycastTarget = false;
            Ui.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(26, 0), new Vector2(34, 34));
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            missionText = Ui.Label("Text", card.transform, "", 30, UiTheme.TextMain,
                                   TextAnchor.MiddleLeft);
            Ui.Anchor(missionText.rectTransform, new Vector2(0f, 0.5f), new Vector2(84, 0), new Vector2(690, 84));

            missionReward = Ui.Label("Reward", card.transform, "", 30, UiTheme.Gold,
                                     TextAnchor.MiddleRight, FontStyle.Bold);
            Ui.Anchor(missionReward.rectTransform, new Vector2(1f, 0.5f), new Vector2(-24, 0), new Vector2(210, 84));
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
                liquidRt.anchoredPosition = new Vector2(0, 3.5f * Mathf.Sin(breathePhase * 2.3f));

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
            // Burbuja dentro del cuerpo del matraz (se estrecha hacia arriba)
            var b = Ui.Panel("IdleBubble", flask, new Color(1f, 1f, 1f, 0.0f));
            b.sprite = UiTheme.Circle();
            b.type = Image.Type.Simple;
            b.raycastTarget = false;
            float x = Random.Range(-55f, 55f);
            float size = Random.Range(12f, 26f);
            Ui.Place(b.rectTransform, x, -130, size, size);

            const float duration = 1.6f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (b == null) yield break;
                float k = t / duration;
                float narrowing = 1f - 0.5f * k; // el cono se cierra al subir
                b.rectTransform.anchoredPosition =
                    new Vector2((x + 8f * Mathf.Sin(k * 9f)) * narrowing, -130 + 130 * k);
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
            Ui.Place(ringImg.rectTransform, 0, 90, 640, 640);
            ring = ringImg;

            // Matraz Erlenmeyer procedural: líquido debajo, vidrio encima, brillo diagonal
            var flaskRoot = Ui.Rect("Flask", Root);
            Ui.Place(flaskRoot, 0, 90, 520, 520);
            flask = flaskRoot;

            var liquid = Ui.Panel("Liquid", flask, UiTheme.Violet, rounded: false);
            liquid.sprite = UiTheme.FlaskLiquid();
            liquid.type = Image.Type.Simple;
            liquid.raycastTarget = false;
            Ui.Fill(liquid.rectTransform);
            liquidRt = liquid.rectTransform;

            // Línea de superficie del líquido (se mueve con el vaivén)
            var surface = Ui.Panel("Surface", liquidRt, new Color(1f, 1f, 1f, 0.22f));
            surface.sprite = UiTheme.Circle();
            surface.type = Image.Type.Simple;
            surface.raycastTarget = false;
            Ui.Place(surface.rectTransform, 0, -14, 168, 22);

            var glass = Ui.Panel("Glass", flask, new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.35f), rounded: false);
            glass.sprite = UiTheme.FlaskGlass();
            glass.type = Image.Type.Simple;
            Ui.Fill(glass.rectTransform);

            var shine = Ui.Panel("Shine", flask, new Color(1f, 1f, 1f, 0.10f));
            shine.raycastTarget = false;
            Ui.Place(shine.rectTransform, -62, -20, 44, 200);
            shine.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);

            var btn = glass.gameObject.AddComponent<Button>();
            btn.targetGraphic = glass;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnFlaskClicked);
            glass.alphaHitTestMinimumThreshold = 0.05f; // solo cuenta el toque dentro de la silueta
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

            var mission = MissionCatalog.Current(s);
            missionCard.SetActive(mission != null);
            if (mission != null)
            {
                missionText.text = Loc.T("ui_objetivo") + ": " + mission.Name;
                missionReward.text = "+" + NumberFormat.Fmt(mission.Reward);
            }

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

        // Pool de partículas: reutiliza las imágenes en vez de crear/destruir por click
        readonly Stack<Image> particlePool = new Stack<Image>();

        Image GetParticle()
        {
            if (particlePool.Count > 0)
            {
                var p = particlePool.Pop();
                p.gameObject.SetActive(true);
                return p;
            }
            var img = Ui.Panel("P", fxLayer, Color.white, rounded: false);
            img.raycastTarget = false;
            return img;
        }

        void ReleaseParticle(Image p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            particlePool.Push(p);
        }

        IEnumerator ParticleBurst()
        {
            int count = game.State.HighQualityMode ? 10 : 5;
            var parts = new List<(RectTransform rt, Image img, Vector2 dir)>();
            for (int i = 0; i < count; i++)
            {
                var el = BaseElements[i % BaseElements.Length];
                var def = ElementCatalog.Get(el);
                var img = GetParticle();
                img.sprite = ProceduralIcons.For(el); // mini icono del elemento
                img.color = UiTheme.ElementColor(def.ColorHex);
                float ang = Random.Range(0f, Mathf.PI * 2);
                Ui.Place(img.rectTransform, 0, 90, 34, 34);
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
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
            foreach (var p in parts) ReleaseParticle(p.img);
        }
    }
}
