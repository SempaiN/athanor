using UnityEngine;
using UnityEngine.UI;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Ajustes: calidad, sonido, volúmenes, vibración, info y borrado.
    public sealed class SettingsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        Text qualityLabel, soundLabel, vibrateLabel, resetLabel;
        Slider musicSlider, sfxSlider;
        Button resetBtn;
        bool confirmingReset;
        float confirmUntil;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("SettingsPanel", parent);
            Ui.Fill(Root);

            float y = -34;

            // Calidad gráfica
            AddRowLabel("QTitle", Loc.T("ui_ajuste_calidad"), y);
            var qBtn = Ui.TextButton("Quality", Root, UiTheme.Card, out qualityLabel);
            qualityLabel.fontSize = 28;
            qualityLabel.color = UiTheme.Amber;
            Ui.Anchor((RectTransform)qBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 14), new Vector2(400, 96));
            qBtn.onClick.AddListener(() => game.ToggleQuality());
            y -= 128;

            // Sonido on/off
            AddRowLabel("STitle", Loc.T("ui_ajuste_sonido"), y);
            var sBtn = Ui.TextButton("Sound", Root, UiTheme.Card, out soundLabel);
            soundLabel.fontSize = 28;
            soundLabel.color = UiTheme.Amber;
            Ui.Anchor((RectTransform)sBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 14), new Vector2(400, 96));
            sBtn.onClick.AddListener(() => game.ToggleSound());
            y -= 118;

            // Volumen música
            AddRowLabel("MTitle", Loc.T("ui_vol_musica"), y, 30);
            Ui.HSlider("MusicSlider", Root, out musicSlider);
            var mrt = (RectTransform)musicSlider.transform;
            Ui.Anchor(mrt, new Vector2(1f, 1f), new Vector2(-40, y - 2), new Vector2(500, 44));
            musicSlider.onValueChanged.AddListener(v => game.SetMusicVolume(v));
            y -= 96;

            // Volumen efectos
            AddRowLabel("FTitle", Loc.T("ui_vol_sfx"), y, 30);
            Ui.HSlider("SfxSlider", Root, out sfxSlider);
            var frt = (RectTransform)sfxSlider.transform;
            Ui.Anchor(frt, new Vector2(1f, 1f), new Vector2(-40, y - 2), new Vector2(500, 44));
            sfxSlider.onValueChanged.AddListener(v => game.SetSfxVolume(v));
            y -= 110;

            // Vibración
            AddRowLabel("VTitle", Loc.T("ui_ajuste_vibracion"), y);
            var vBtn = Ui.TextButton("Vibrate", Root, UiTheme.Card, out vibrateLabel);
            vibrateLabel.fontSize = 28;
            vibrateLabel.color = UiTheme.Amber;
            Ui.Anchor((RectTransform)vBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 14), new Vector2(400, 96));
            vBtn.onClick.AddListener(() => game.ToggleVibrate());
            y -= 140;

            // Info y enlace al repo
            var info = Ui.Label("Info", Root,
                "Athanor v" + GameVersion.Version + "\n" + Loc.T("ui_ajuste_info"),
                26, UiTheme.TextDim, TextAnchor.UpperLeft);
            Ui.Anchor(info.rectTransform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(640, 90));

            var repoBtn = Ui.TextButton("Repo", Root, UiTheme.Card, out var repoLabel);
            repoLabel.fontSize = 28;
            repoLabel.color = UiTheme.TextMain;
            repoLabel.text = "GitHub";
            Ui.Anchor((RectTransform)repoBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 14), new Vector2(400, 90));
            repoBtn.onClick.AddListener(() => Application.OpenURL("https://github.com/SempaiN/athanor/releases"));

            // Borrar progreso (doble confirmación)
            resetBtn = Ui.TextButton("Reset", Root, new Color(0.55f, 0.15f, 0.15f), out resetLabel);
            resetLabel.fontSize = 30;
            resetLabel.color = UiTheme.TextMain;
            Ui.Anchor((RectTransform)resetBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(600, 104));
            resetBtn.onClick.AddListener(OnResetClicked);
        }

        void AddRowLabel(string name, string text, float y, int size = 34)
        {
            var t = Ui.Label(name, Root, text, size, UiTheme.TextMain,
                             TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(460, 50));
        }

        void OnResetClicked()
        {
            // La confirmación solo vale dentro de la ventana de 3 s
            if (!confirmingReset || Time.unscaledTime > confirmUntil)
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
            vibrateLabel.text = s.VibrateOn ? Loc.T("ui_sonido_on") : Loc.T("ui_sonido_off");
            musicSlider.SetValueWithoutNotify(s.MusicVolume);
            sfxSlider.SetValueWithoutNotify(s.SfxVolume);
            resetLabel.text = confirmingReset ? Loc.T("ui_reset_confirmar") : Loc.T("ui_reset");
        }
    }
}
