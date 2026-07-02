using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña La Gran Obra: prestigio con Quintaesencia.
    public sealed class PrestigePanel
    {
        readonly MonoBehaviour host;
        GameController game;
        public RectTransform Root { get; private set; }

        Text quintText, bonusText, gainText, reqText;
        Button prestigeBtn;
        Text prestigeLabel;
        bool confirming;
        float confirmUntil;

        public PrestigePanel(MonoBehaviour host) { this.host = host; }

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("PrestigePanel", parent);
            Ui.Fill(Root);

            // Cabecera ilustrada (Canva) con recorte por máscara; sin ella, layout clásico
            float yOff = 0;
            var bannerSprite = Resources.Load<Sprite>("Art/Core/gran_obra");
            if (bannerSprite != null)
            {
                var frame = Ui.Panel("BannerFrame", Root, UiTheme.Card);
                Ui.Anchor(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(980, 300));
                frame.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

                var art = Ui.Panel("Art", frame.transform, Color.white, rounded: false);
                art.sprite = bannerSprite;
                art.type = Image.Type.Simple;
                art.raycastTarget = false;
                Ui.Place(art.rectTransform, 0, 30, 980, 551); // 16:9 recortado a franja

                yOff = 310;
            }

            var orb = Ui.Panel("Orb", Root, UiTheme.Violet);
            orb.sprite = UiTheme.Circle();
            orb.type = Image.Type.Simple;
            orb.raycastTarget = false;
            Ui.Anchor(orb.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50 - yOff), new Vector2(150, 150));

            quintText = Ui.Label("Quint", Root, "0", 68, UiTheme.Violet,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(quintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -216 - yOff), new Vector2(600, 80));

            var caption = Ui.Label("Caption", Root, Loc.T("ui_quintaesencia"), 32, UiTheme.TextDim);
            Ui.Anchor(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -290 - yOff), new Vector2(600, 38));

            bonusText = Ui.Label("Bonus", Root, "", 34, UiTheme.TextMain);
            Ui.Anchor(bonusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -348 - yOff), new Vector2(900, 44));

            reqText = Ui.Label("Reqs", Root, "", 30, UiTheme.TextDim, TextAnchor.UpperCenter);
            Ui.Anchor(reqText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -404 - yOff), new Vector2(920, 124));

            gainText = Ui.Label("Gain", Root, "", 40, UiTheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(gainText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(900, 60));

            prestigeBtn = Ui.TextButton("Prestige", Root, UiTheme.Violet, out prestigeLabel);
            prestigeLabel.fontSize = 38;
            prestigeLabel.color = UiTheme.TextMain;
            Ui.Anchor((RectTransform)prestigeBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(700, 150));
            prestigeBtn.onClick.AddListener(OnPrestigeClicked);
        }

        void OnPrestigeClicked()
        {
            // La confirmación solo vale dentro de la ventana de 3 s
            if (!confirming || Time.unscaledTime > confirmUntil)
            {
                confirming = true;
                confirmUntil = Time.unscaledTime + 3f;
                Refresh();
                return;
            }
            confirming = false;
            game.DoPrestige();
        }

        public void Refresh()
        {
            if (confirming && Time.unscaledTime > confirmUntil) confirming = false;

            var s = game.State;
            quintText.text = NumberFormat.Fmt(s.Quintessence);
            bonusText.text = Loc.T("ui_bonus_actual") + " +" +
                Mathf.RoundToInt((float)(s.Quintessence * 10)) + "%  ·  " +
                Loc.T("ui_prestigios") + ": " + s.PrestigeCount;

            bool hasStone = s.Discovered.Contains(ElementId.PiedraFilosofal);
            string stoneMark = hasStone ? "[OK]" : "[X]";
            string essMark = s.LifetimeEssence >= GameRules.PrestigeEssenceFloor ? "[OK]" : "[X]";
            reqText.text = Loc.T("ui_prestigio_desc") + "\n" +
                stoneMark + " " + Loc.T("el_piedrafilosofal") + "   " +
                essMark + " " + NumberFormat.Fmt(GameRules.PrestigeEssenceFloor) + " " + Loc.T("ui_esencia") +
                " (" + NumberFormat.Fmt(s.LifetimeEssence) + ")";

            double gain = game.PrestigeGain;
            gainText.text = "+" + NumberFormat.Fmt(gain) + " " + Loc.T("ui_quintaesencia");

            prestigeBtn.interactable = game.CanPrestige;
            prestigeLabel.text = confirming
                ? Loc.T("ui_prestigio_confirmar")
                : Loc.T("ui_prestigio_boton");
        }
    }
}
