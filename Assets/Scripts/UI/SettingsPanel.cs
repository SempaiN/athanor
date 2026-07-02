using System;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Ajustes: calidad, sonido, volúmenes, vibración, info y borrado.
    public sealed class SettingsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        Text qualityLabel, soundLabel, vibrateLabel, resetLabel, statsText;
        Text copyLabel, pasteLabel;
        Slider musicSlider, sfxSlider;
        Button resetBtn;
        bool confirmingReset;
        float confirmUntil;
        string copyFeedback, pasteFeedback;
        float feedbackUntil;
        bool confirmingPaste;
        float pasteConfirmUntil;

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
            y -= 124;

            // Copia de seguridad del guardado (portapapeles)
            AddRowLabel("BkTitle", Loc.T("ui_backup_titulo"), y);
            var copyBtn = Ui.TextButton("CopySave", Root, UiTheme.Card, out copyLabel);
            copyLabel.fontSize = 26;
            copyLabel.color = UiTheme.TextMain;
            Ui.Anchor((RectTransform)copyBtn.transform, new Vector2(1f, 1f), new Vector2(-40, y + 14), new Vector2(400, 90));
            copyBtn.onClick.AddListener(OnCopySave);
            var pasteBtn = Ui.TextButton("PasteSave", Root, UiTheme.Card, out pasteLabel);
            pasteLabel.fontSize = 26;
            pasteLabel.color = UiTheme.TextMain;
            Ui.Anchor((RectTransform)pasteBtn.transform, new Vector2(1f, 1f), new Vector2(-456, y + 14), new Vector2(310, 90));
            pasteBtn.onClick.AddListener(OnPasteSave);
            y -= 128;

            // Estadísticas de la partida
            AddRowLabel("StatsTitle", Loc.T("ui_stats_titulo"), y);
            statsText = Ui.Label("Stats", Root, "", 27, UiTheme.TextDim, TextAnchor.UpperLeft);
            Ui.Anchor(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(50, y - 54), new Vector2(940, 230));
            y -= 300;

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

        void OnCopySave()
        {
            GUIUtility.systemCopyBuffer = game.ExportSave();
            copyFeedback = Loc.T("ui_copiado");
            feedbackUntil = Time.unscaledTime + 2.5f;
            Refresh();
        }

        void OnPasteSave()
        {
            // Sobrescribe el progreso: doble confirmación de 3 s
            if (!confirmingPaste || Time.unscaledTime > pasteConfirmUntil)
            {
                confirmingPaste = true;
                pasteConfirmUntil = Time.unscaledTime + 3f;
                Refresh();
                return;
            }
            confirmingPaste = false;
            bool ok = game.ImportSave(GUIUtility.systemCopyBuffer);
            pasteFeedback = ok ? Loc.T("ui_cargado") : Loc.T("ui_invalido");
            feedbackUntil = Time.unscaledTime + 2.5f;
            Refresh();
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

            if (Time.unscaledTime > feedbackUntil) { copyFeedback = null; pasteFeedback = null; }
            if (confirmingPaste && Time.unscaledTime > pasteConfirmUntil) confirmingPaste = false;
            copyLabel.text = copyFeedback ?? Loc.T("ui_copiar_save");
            pasteLabel.text = confirmingPaste
                ? Loc.T("ui_pegar_confirmar")
                : (pasteFeedback ?? Loc.T("ui_pegar_save"));

            var s = game.State;
            qualityLabel.text = s.HighQualityMode
                ? Loc.T("ui_calidad_alta")
                : Loc.T("ui_calidad_rendimiento");
            soundLabel.text = s.SoundOff ? Loc.T("ui_sonido_off") : Loc.T("ui_sonido_on");
            vibrateLabel.text = s.VibrateOn ? Loc.T("ui_sonido_on") : Loc.T("ui_sonido_off");
            musicSlider.SetValueWithoutNotify(s.MusicVolume);
            sfxSlider.SetValueWithoutNotify(s.SfxVolume);
            resetLabel.text = confirmingReset ? Loc.T("ui_reset_confirmar") : Loc.T("ui_reset");

            var t = TimeSpan.FromSeconds(s.PlaySeconds);
            string tiempo = t.TotalHours >= 1
                ? $"{(int)t.TotalHours} h {t.Minutes} min"
                : $"{t.Minutes} min {t.Seconds} s";
            statsText.text =
                Loc.T("ui_stat_tiempo") + ": " + tiempo + "\n" +
                Loc.T("ui_stat_clicks") + ": " + NumberFormat.Fmt(s.TotalClicks) + "\n" +
                Loc.T("ui_stat_esencia") + ": " + NumberFormat.Fmt(s.LifetimeEssence) + "\n" +
                Loc.T("ui_stat_elementos") + ": " + s.Discovered.Count + "/" + ElementCatalog.Elements.Count + "\n" +
                Loc.T("ui_stat_logros") + ": " + s.AchievementsUnlocked.Count + "/" + AchievementCatalog.All.Count + "\n" +
                Loc.T("ui_stat_dorados") + ": " + s.GoldenTaps + "\n" +
                Loc.T("ui_prestigios") + ": " + s.PrestigeCount + "  ·  " +
                Loc.T("ui_quintaesencia") + ": " + NumberFormat.Fmt(s.Quintessence);
        }
    }
}
