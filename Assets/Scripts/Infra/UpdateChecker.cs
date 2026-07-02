using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Athanor.UI;

namespace Athanor.Infra
{
    /// Al abrir la app consulta GitHub Releases y, si hay versión nueva, muestra un aviso
    /// con botón de descarga. Pensado para la fase de distribución por GitHub; se elimina
    /// del bootstrap si el juego se publica en Play Store.
    public sealed class UpdateChecker : MonoBehaviour
    {
        const string ApiUrl = "https://api.github.com/repos/SempaiN/athanor/releases/latest";
        const string DownloadUrl = "https://github.com/SempaiN/athanor/releases/latest/download/athanor.apk";

        RectTransform root;

        [Serializable]
        class ReleaseDto { public string tag_name; }

        public void Init(RectTransform uiRoot)
        {
            root = uiRoot;
            StartCoroutine(Check());
        }

        IEnumerator Check()
        {
            yield return new WaitForSeconds(1.5f); // no compite con el arranque

            using (var req = UnityWebRequest.Get(ApiUrl))
            {
                req.timeout = 8;
                req.SetRequestHeader("User-Agent", "athanor-app");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success) yield break; // sin red: silencio

                ReleaseDto dto = null;
                try { dto = JsonUtility.FromJson<ReleaseDto>(req.downloadHandler.text); }
                catch { yield break; }

                if (dto != null && IsNewer(dto.tag_name, GameVersion.Version))
                    ShowPrompt(dto.tag_name);
            }
        }

        /// "v0.2.0" vs "0.1.2" → true si el remoto es mayor.
        static bool IsNewer(string remoteTag, string current)
        {
            if (string.IsNullOrEmpty(remoteTag)) return false;
            var r = Parse(remoteTag.TrimStart('v', 'V'));
            var c = Parse(current);
            for (int i = 0; i < 3; i++)
            {
                if (r[i] > c[i]) return true;
                if (r[i] < c[i]) return false;
            }
            return false;
        }

        static int[] Parse(string v)
        {
            var outp = new int[3];
            var parts = v.Split('.');
            for (int i = 0; i < 3 && i < parts.Length; i++)
                int.TryParse(parts[i], out outp[i]);
            return outp;
        }

        void ShowPrompt(string newVersion)
        {
            // Velo oscuro que bloquea el juego hasta decidir
            var veil = Ui.Panel("UpdateVeil", root, new Color(0, 0, 0, 0.65f), rounded: false);
            Ui.Fill(veil.rectTransform);

            var card = Ui.Panel("UpdateCard", veil.transform, UiTheme.Card);
            Ui.Place(card.rectTransform, 0, 0, 860, 520);
            StartCoroutine(UiFx.PopIn(card.rectTransform));

            var title = Ui.Label("Title", card.transform, Loc.T("ui_update_titulo"), 52,
                                 UiTheme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(800, 70));

            var body = Ui.Label("Body", card.transform,
                                Loc.T("ui_update_texto") + "\n" + newVersion, 40,
                                UiTheme.TextMain);
            Ui.Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(780, 160));

            var download = Ui.TextButton("Download", card.transform, UiTheme.Gold, out var dlLabel);
            dlLabel.text = Loc.T("ui_update_descargar");
            Ui.Anchor((RectTransform)download.transform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(560, 120));
            download.onClick.AddListener(() => Application.OpenURL(DownloadUrl));

            var later = Ui.TextButton("Later", card.transform, UiTheme.Panel, out var laterLabel);
            laterLabel.text = Loc.T("ui_update_luego");
            laterLabel.color = UiTheme.TextDim;
            Ui.Anchor((RectTransform)later.transform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(560, 100));
            later.onClick.AddListener(() => Destroy(veil.gameObject));
        }
    }
}
