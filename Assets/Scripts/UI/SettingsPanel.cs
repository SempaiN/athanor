using UnityEngine;
using UnityEngine.UI;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Ajustes: calidad gráfica, sonido, info y borrado de progreso.
    public sealed class SettingsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        Text qualityLabel, soundLabel, resetLabel;
        Button resetBtn;
        bool confirmingReset;
        float confirmUntil;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("SettingsPanel", parent);
            Ui.Fill(Root);

            float y = -40;

            // Calidad gráfica
            var qTitle = Ui.Label("QTitle", Root, Loc.T("ui_ajuste_calidad"), 36, UiTheme.TextMain,
                                  TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Anchor(qTitle.rectTransform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(500, 50));
            var qBtn = Ui.TextButton("Quality", Root, UiTheme.Card, out qualityLabel);
            qualityLabel.fontSize = 30;
            qualityLabel.color = UiTheme.Amber;
            Ui.Anchor((RectTransform)qBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 10), new Vector2(430, 110));
            qBtn.onClick.AddListener(() => game.ToggleQuality());

            y -= 160;

            // Sonido
            var sTitle = Ui.Label("STitle", Root, Loc.T("ui_ajuste_sonido"), 36, UiTheme.TextMain,
                                  TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Anchor(sTitle.rectTransform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(500, 50));
            var sBtn = Ui.TextButton("Sound", Root, UiTheme.Card, out soundLabel);
            soundLabel.fontSize = 30;
            soundLabel.color = UiTheme.Amber;
            Ui.Anchor((RectTransform)sBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 10), new Vector2(430, 110));
            sBtn.onClick.AddListener(() => game.ToggleSound());

            y -= 200;

            // Info y enlace al repo
            var info = Ui.Label("Info", Root,
                "Athanor v" + GameVersion.Version + "\n" + Loc.T("ui_ajuste_info"),
                28, UiTheme.TextDim, TextAnchor.UpperLeft);
            Ui.Anchor(info.rectTransform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(700, 90));

            var repoBtn = Ui.TextButton("Repo", Root, UiTheme.Card, out var repoLabel);
            repoLabel.fontSize = 28;
            repoLabel.color = UiTheme.TextMain;
            repoLabel.text = "GitHub";
            Ui.Anchor((RectTransform)repoBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 10), new Vector2(430, 100));
            repoBtn.onClick.AddListener(() => Application.OpenURL("https://github.com/SempaiN/athanor/releases"));

            // Borrar progreso (abajo del todo, doble confirmación)
            resetBtn = Ui.TextButton("Reset", Root, new Color(0.55f, 0.15f, 0.15f), out resetLabel);
            resetLabel.fontSize = 30;
            resetLabel.color = UiTheme.TextMain;
            Ui.Anchor((RectTransform)resetBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(600, 110));
            resetBtn.onClick.AddListener(OnResetClicked);
        }

        void OnResetClicked()
        {
            if (!confirmingReset)
            {
                confirmingReset = true;
                confirmUntil = Time.unscaledTime + 3f;
                Refresh();
                return;
            }
            confirmingReset = false;
            game.ResetSave();
        }

        public void Refresh()
        {
            if (confirmingReset && Time.unscaledTime > confirmUntil) confirmingReset = false;

            var s = game.State;
            qualityLabel.text = s.HighQualityMode
                ? Loc.T("ui_calidad_alta")
                : Loc.T("ui_calidad_rendimiento");
            soundLabel.text = s.SoundOff ? Loc.T("ui_sonido_off") : Loc.T("ui_sonido_on");
            resetLabel.text = confirmingReset ? Loc.T("ui_reset_confirmar") : Loc.T("ui_reset");
        }
    }
}
