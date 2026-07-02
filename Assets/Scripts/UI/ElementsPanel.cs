using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Elementos: árbol de combinaciones con descubrimiento progresivo
    /// y venta individual de elementos superiores.
    public sealed class ElementsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        sealed class Row
        {
            public Recipe Recipe;
            public Image OutDot;
            public Text Title, Detail, OwnedText;
            public Button CombineBtn, SellBtn;
            public Text CombineLabel, SellLabel;
        }

        readonly List<Row> rows = new List<Row>();
        const float RowH = 200;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("ElementsPanel", parent);
            Ui.Fill(Root);

            var content = Ui.ScrollList("List", Root, out _);
            var viewport = (RectTransform)content.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 10);
            viewport.offsetMax = new Vector2(-20, -10);

            var ordered = ElementCatalog.Recipes
                .OrderBy(r => ElementCatalog.Get(r.Output).Tier)
                .ToList();

            int i = 0;
            foreach (var recipe in ordered)
            {
                var row = new Row { Recipe = recipe };
                var bg = Ui.Panel("Recipe_" + recipe.Output, content, UiTheme.Card);
                Ui.Row(bg.rectTransform, i, RowH);

                row.OutDot = Ui.Panel("Dot", bg.transform, UiTheme.TextDim);
                row.OutDot.sprite = UiTheme.Circle();
                row.OutDot.type = Image.Type.Simple;
                Ui.Anchor(row.OutDot.rectTransform, new Vector2(0f, 1f), new Vector2(28, -22), new Vector2(44, 44));

                row.Title = Ui.Label("Title", bg.transform, "", 40, UiTheme.TextMain,
                                     TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(row.Title.rectTransform, new Vector2(0f, 1f), new Vector2(90, -18), new Vector2(480, 52));

                row.OwnedText = Ui.Label("Owned", bg.transform, "", 34, UiTheme.Amber,
                                         TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(row.OwnedText.rectTransform, new Vector2(0f, 1f), new Vector2(90, -68), new Vector2(480, 44));

                row.Detail = Ui.Label("Detail", bg.transform, "", 28, UiTheme.TextDim, TextAnchor.UpperLeft);
                Ui.Anchor(row.Detail.rectTransform, new Vector2(0f, 1f), new Vector2(90, -116), new Vector2(560, 66));

                row.CombineBtn = Ui.TextButton("Combine", bg.transform, UiTheme.Green, out row.CombineLabel);
                row.CombineLabel.fontSize = 30;
                Ui.Anchor((RectTransform)row.CombineBtn.transform, new Vector2(1f, 1f), new Vector2(-18, -16), new Vector2(300, 84));
                row.CombineBtn.gameObject.AddComponent<RepeatButton>(); // mantener = combinar en cadena
                var rec = recipe;
                row.CombineBtn.onClick.AddListener(() => game.Combine(rec));

                row.SellBtn = Ui.TextButton("Sell", bg.transform, UiTheme.Gold, out row.SellLabel);
                row.SellLabel.fontSize = 28;
                Ui.Anchor((RectTransform)row.SellBtn.transform, new Vector2(1f, 1f), new Vector2(-18, -108), new Vector2(300, 74));
                row.SellBtn.onClick.AddListener(() =>
                    game.TransmuteElement(rec.Output, game.State.BalanceOf(rec.Output)));

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
                var r = row.Recipe;
                var outDef = ElementCatalog.Get(r.Output);
                bool visible = GameRules.IsRecipeVisible(s, r);
                bool discovered = s.Discovered.Contains(r.Output);

                if (!visible)
                {
                    row.OutDot.color = new Color(1, 1, 1, 0.08f);
                    row.Title.text = Loc.T("ui_desconocido");
                    row.OwnedText.text = "";
                    row.Detail.text = "";
                    row.CombineBtn.gameObject.SetActive(false);
                    row.SellBtn.gameObject.SetActive(false);
                    continue;
                }

                string inA = Loc.T(ElementCatalog.Get(r.InputA).NameKey);
                string inB = Loc.T(ElementCatalog.Get(r.InputB).NameKey);
                double haveA = s.BalanceOf(r.InputA);
                double haveB = s.BalanceOf(r.InputB);

                row.CombineBtn.gameObject.SetActive(true);
                row.CombineLabel.text = Loc.T("ui_combinar");
                row.CombineBtn.interactable = GameRules.CanCombine(s, r);

                row.Detail.text =
                    r.UnitsPerInput + " " + inA + " (" + NumberFormat.Fmt(haveA) + ")  +  " +
                    r.UnitsPerInput + " " + inB + " (" + NumberFormat.Fmt(haveB) + ")";

                if (!discovered)
                {
                    row.OutDot.color = new Color(1, 1, 1, 0.15f);
                    row.Title.text = "?";
                    row.OwnedText.text = Loc.T("ui_receta_oculta");
                    row.OwnedText.color = UiTheme.TextDim;
                    row.SellBtn.gameObject.SetActive(false);
                    continue;
                }

                double owned = s.BalanceOf(r.Output);
                row.OutDot.color = UiTheme.ElementColor(outDef.ColorHex);
                row.Title.text = Loc.T(outDef.NameKey);
                row.OwnedText.color = UiTheme.Amber;
                row.OwnedText.text = "x" + NumberFormat.Fmt(owned) +
                    "   (" + NumberFormat.Fmt(outDef.EssenceValue) + " " + Loc.T("ui_esencia") + " c/u)";

                row.SellBtn.gameObject.SetActive(owned > 0);
                row.SellLabel.text = Loc.T("ui_vender") + " +" +
                    NumberFormat.Fmt(owned * outDef.EssenceValue);
            }
        }
    }
}
