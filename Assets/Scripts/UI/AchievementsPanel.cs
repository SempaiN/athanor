using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Logros: lista completa con estado y bonus.
    public sealed class AchievementsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        sealed class Row
        {
            public AchievementDef Def;
            public Image Bg, Medal, Stripe;
            public Text Name, Desc, Bonus;
        }

        readonly List<Row> rows = new List<Row>();
        const float RowH = 150;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("AchievementsPanel", parent);
            Ui.Fill(Root);

            var content = Ui.ScrollList("List", Root, out _);
            var viewport = (RectTransform)content.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 10);
            viewport.offsetMax = new Vector2(-20, -10);

            int i = 0;
            foreach (var def in AchievementCatalog.All)
            {
                var row = new Row { Def = def };
                row.Bg = Ui.Panel("Ach_" + def.Id, content, UiTheme.Card);
                Ui.Row(row.Bg.rectTransform, i, RowH);

                // Medalla dentro de un chip tonal dorado (fila estilo Material)
                row.Stripe = Ui.Panel("MedalChip", row.Bg.transform, new Color(1, 1, 1, 0.04f));
                row.Stripe.raycastTarget = false;
                Ui.Anchor(row.Stripe.rectTransform, new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(84, 84));

                row.Medal = Ui.Panel("Medal", row.Stripe.transform, UiTheme.TextDim);
                row.Medal.sprite = ProceduralIcons.Medal(out _);
                row.Medal.type = Image.Type.Simple;
                row.Medal.raycastTarget = false;
                Ui.Place(row.Medal.rectTransform, 0, 0, 58, 58);

                row.Name = Ui.Label("Name", row.Bg.transform, def.Name, 34, UiTheme.TextMain,
                                    TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(row.Name.rectTransform, new Vector2(0f, 1f), new Vector2(122, -20), new Vector2(590, 44));

                row.Desc = Ui.Label("Desc", row.Bg.transform, def.Desc, 27, UiTheme.TextDim,
                                    TextAnchor.MiddleLeft);
                Ui.Anchor(row.Desc.rectTransform, new Vector2(0f, 1f), new Vector2(122, -68), new Vector2(630, 40));

                row.Bonus = Ui.Label("Bonus", row.Bg.transform, "+" + Mathf.RoundToInt((float)(def.Bonus * 100)) + "%",
                                     38, UiTheme.Green, TextAnchor.MiddleRight, FontStyle.Bold);
                Ui.Anchor(row.Bonus.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(160, 50));

                rows.Add(row);
                i++;
            }

            content.sizeDelta = new Vector2(0, rows.Count * RowH + 20);
        }

        public void Refresh()
        {
            var s = game.State;
            foreach (var row in rows)
            {
                bool unlocked = s.AchievementsUnlocked.Contains(row.Def.Id);
                ProceduralIcons.Medal(out bool medalBaked);
                row.Medal.color = unlocked
                    ? (medalBaked ? Color.white : UiTheme.Gold)
                    : new Color(1, 1, 1, 0.10f);
                row.Name.color = unlocked ? UiTheme.TextMain : UiTheme.TextDim;
                row.Bonus.color = unlocked ? UiTheme.Green : new Color(1, 1, 1, 0.18f);
                row.Bg.color = unlocked ? UiTheme.Card : new Color(UiTheme.Card.r, UiTheme.Card.g, UiTheme.Card.b, 0.55f);
                row.Stripe.color = unlocked ? UiTheme.Tint(UiTheme.Gold, 0.18f) : new Color(1, 1, 1, 0.04f);
            }
        }
    }
}
