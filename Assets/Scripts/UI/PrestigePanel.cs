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

            var orb = Ui.Panel("Orb", Root, UiTheme.Violet);
            orb.sprite = UiTheme.Circle();
            orb.type = Image.Type.Simple;
            orb.raycastTarget = false;
            Ui.Anchor(orb.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(180, 180));

            quintText = Ui.Label("Quint", Root, "0", 72, UiTheme.Violet,
                                 TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(quintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(600, 84));

            var caption = Ui.Label("Caption", Root, Loc.T("ui_quintaesencia"), 34, UiTheme.TextDim);
            Ui.Anchor(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(600, 40));

            bonusText = Ui.Label("Bonus", Root, "", 38, UiTheme.TextMain);
            Ui.Anchor(bonusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(900, 48));

            reqText = Ui.Label("Reqs", Root, "", 32, UiTheme.TextDim, TextAnchor.UpperCenter);
            Ui.Anchor(reqText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -470), new Vector2(920, 130));

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
            if (!confirming)
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
