using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Ayudantes: lista de generadores comprables.
    public sealed class GeneratorsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        sealed class Card
        {
            public GeneratorDef Def;
            public GameObject Go;
            public Text Name, Owned, Info;
            public Button Buy;
            public Text BuyLabel;
            public bool Revealed;
        }

        readonly List<Card> cards = new List<Card>();
        const float RowH = 170;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("GeneratorsPanel", parent);
            Ui.Fill(Root);

            var content = Ui.ScrollList("List", Root, out _);
            ((RectTransform)content.parent).anchorMin = Vector2.zero;
            ((RectTransform)content.parent).anchorMax = Vector2.one;
            ((RectTransform)content.parent).offsetMin = new Vector2(20, 10);
            ((RectTransform)content.parent).offsetMax = new Vector2(-20, -10);

            int i = 0;
            foreach (var g in GeneratorCatalog.Generators)
            {
                var card = new Card { Def = g };
                var bg = Ui.Panel("Card_" + g.Id, content, UiTheme.Card);
                Ui.Row(bg.rectTransform, i, RowH);
                card.Go = bg.gameObject;

                card.Name = Ui.Label("Name", bg.transform, Loc.T(g.NameKey), 42, UiTheme.TextMain,
                                     TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(card.Name.rectTransform, new Vector2(0f, 1f), new Vector2(30, -14), new Vector2(520, 52));

                card.Owned = Ui.Label("Owned", bg.transform, "", 38, UiTheme.Amber,
                                      TextAnchor.MiddleRight, FontStyle.Bold);
                Ui.Anchor(card.Owned.rectTransform, new Vector2(1f, 1f), new Vector2(-300, -14), new Vector2(150, 52));

                card.Info = Ui.Label("Info", bg.transform, "", 30, UiTheme.TextDim, TextAnchor.UpperLeft);
                Ui.Anchor(card.Info.rectTransform, new Vector2(0f, 1f), new Vector2(30, -70), new Vector2(600, 70));

                card.Buy = Ui.TextButton("Buy", bg.transform, UiTheme.Green, out card.BuyLabel);
                card.BuyLabel.fontSize = 32;
                Ui.Anchor((RectTransform)card.Buy.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(250, 120));
                card.Buy.gameObject.AddComponent<RepeatButton>(); // mantener = comprar en cadena
                var def = g;
                card.Buy.onClick.AddListener(() => game.BuyGenerator(def));

                cards.Add(card);
                i++;
            }

            var content2 = (RectTransform)content;
            content2.sizeDelta = new Vector2(0, cards.Count * RowH + 20);
        }

        static string ProduceText(GeneratorDef g, double mult)
        {
            if (g.AutoCombine) return Loc.T("ui_combina_solo");
            double perElement = g.BaseProd * mult / g.Produces.Length;
            var names = string.Join(" + ", g.Produces.Select(e => Loc.T(ElementCatalog.Get(e).NameKey)));
            return Loc.T("ui_produce") + " " + NumberFormat.Fmt(perElement) + "/s " + names + " (por unidad)";
        }

        public void Refresh()
        {
            var s = game.State;
            double mult = s.GlobalMultiplier(game.AchievementBonus);

            foreach (var c in cards)
            {
                int owned = game.GeneratorOwned(c.Def.Id);
                double cost = game.GeneratorCost(c.Def);

                // Se revela cuando ya tenés alguno o tu esencia histórica se acerca al coste
                bool revealed = owned > 0 || s.LifetimeEssence >= c.Def.BaseCost * 0.4;
                c.Revealed = revealed;
                c.Name.text = revealed ? Loc.T(c.Def.NameKey) : Loc.T("ui_desconocido");
                if (!revealed)
                {
                    c.Owned.text = "";
                    c.Info.text = "";
                    c.BuyLabel.text = Loc.T("ui_desconocido");
                    c.Buy.interactable = false;
                    continue;
                }

                c.Owned.text = "x" + owned;
                c.Info.text = ProduceText(c.Def, mult);
                c.BuyLabel.text = Loc.T("ui_comprar") + "\n" + NumberFormat.Fmt(cost);
                c.Buy.interactable = s.Essence >= cost;
            }
        }
    }
}
