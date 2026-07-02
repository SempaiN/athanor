using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;
using Athanor.Infra;

namespace Athanor.UI
{
    /// Pestaña Ayudantes: generadores comprables con modo x1 / x10 / Máx.
    public sealed class GeneratorsPanel
    {
        GameController game;
        public RectTransform Root { get; private set; }

        sealed class Card
        {
            public GeneratorDef Def;
            public Text Name, Owned, Info;
            public Button Buy;
            public Text BuyLabel;
        }

        readonly List<Card> cards = new List<Card>();
        readonly List<(int mode, Image bg, Text label)> modeButtons = new List<(int, Image, Text)>();
        Text hint;
        int buyMode = 1; // 1, 10, 0 = Máx

        const float RowH = 170;
        const float HeaderH = 90;

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            Root = Ui.Rect("GeneratorsPanel", parent);
            Ui.Fill(Root);

            BuildHeader();

            var content = Ui.ScrollList("List", Root, out _);
            var viewport = (RectTransform)content.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 10);
            viewport.offsetMax = new Vector2(-20, -HeaderH - 6);

            int i = 0;
            foreach (var g in GeneratorCatalog.Generators)
            {
                var card = new Card { Def = g };
                var bg = Ui.Panel("Card_" + g.Id, content, UiTheme.Card);
                Ui.Row(bg.rectTransform, i, RowH);

                var stripe = Ui.Panel("Stripe", bg.transform, UiTheme.Green, rounded: false);
                stripe.raycastTarget = false;
                Ui.Anchor(stripe.rectTransform, new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(10, RowH - 10));

                card.Name = Ui.Label("Name", bg.transform, "", 42, UiTheme.TextMain,
                                     TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(card.Name.rectTransform, new Vector2(0f, 1f), new Vector2(34, -14), new Vector2(520, 52));

                card.Owned = Ui.Label("Owned", bg.transform, "", 38, UiTheme.Amber,
                                      TextAnchor.MiddleRight, FontStyle.Bold);
                Ui.Anchor(card.Owned.rectTransform, new Vector2(1f, 1f), new Vector2(-300, -14), new Vector2(150, 52));

                card.Info = Ui.Label("Info", bg.transform, "", 30, UiTheme.TextDim, TextAnchor.UpperLeft);
                Ui.Anchor(card.Info.rectTransform, new Vector2(0f, 1f), new Vector2(34, -70), new Vector2(600, 70));

                card.Buy = Ui.TextButton("Buy", bg.transform, UiTheme.Green, out card.BuyLabel);
                card.BuyLabel.fontSize = 30;
                Ui.Anchor((RectTransform)card.Buy.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(250, 120));
                card.Buy.gameObject.AddComponent<RepeatButton>(); // mantener = comprar en cadena
                var def = g;
                card.Buy.onClick.AddListener(() => OnBuy(def));

                cards.Add(card);
                i++;
            }

            content.sizeDelta = new Vector2(0, cards.Count * RowH + 20);
        }

        void BuildHeader()
        {
            var header = Ui.Panel("Header", Root, UiTheme.Panel);
            Ui.Anchor(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(1020, HeaderH - 12));

            hint = Ui.Label("Hint", header.transform, Loc.T("ui_comprar_modo"), 30,
                            UiTheme.TextDim, TextAnchor.MiddleLeft);
            Ui.Anchor(hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(28, 0), new Vector2(420, 60));

            (int mode, string label)[] modes = { (1, "x1"), (10, "x10"), (0, Loc.T("ui_max")) };
            for (int m = 0; m < modes.Length; m++)
            {
                var btn = Ui.TextButton("Mode_" + modes[m].mode, header.transform, UiTheme.Card, out var lbl);
                lbl.fontSize = 28;
                lbl.text = modes[m].label;
                lbl.color = UiTheme.TextMain;
                Ui.Anchor((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(-24 - (2 - m) * 150, 0), new Vector2(138, 62));
                int mode = modes[m].mode;
                btn.onClick.AddListener(() => { buyMode = mode; Refresh(); });
                modeButtons.Add((mode, (Image)btn.targetGraphic, lbl));
            }
        }

        void OnBuy(GeneratorDef def)
        {
            int count = CountFor(def);
            if (count > 0) game.BuyGenerator(def, count);
        }

        int CountFor(GeneratorDef def)
        {
            if (buyMode != 0) return buyMode;
            return GameRules.MaxAffordable(def.BaseCost, game.GeneratorOwned(def.Id), game.State.Essence);
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

            foreach (var mb in modeButtons)
            {
                bool active = mb.mode == buyMode;
                mb.bg.color = active ? UiTheme.Amber : UiTheme.Card;
                mb.label.color = active ? UiTheme.Background : UiTheme.TextMain;
            }

            bool anyOwned = s.GeneratorsOwned.Values.Any(n => n > 0);
            hint.text = anyOwned
                ? Loc.T("ui_comprar_modo")
                : Loc.T("ui_hint_sin_ayudantes");

            foreach (var c in cards)
            {
                int owned = game.GeneratorOwned(c.Def.Id);

                bool revealed = owned > 0 || s.LifetimeEssence >= c.Def.BaseCost * 0.4;
                c.Name.text = revealed ? Loc.T(c.Def.NameKey) : Loc.T("ui_desconocido");
                if (!revealed)
                {
                    c.Owned.text = "";
                    c.Info.text = "";
                    c.BuyLabel.text = Loc.T("ui_desconocido");
                    c.Buy.interactable = false;
                    continue;
                }

                int count = CountFor(c.Def);
                int shownCount = count > 0 ? count : 1;
                double cost = GameRules.BulkCost(c.Def.BaseCost, owned, shownCount);

                c.Owned.text = "x" + owned;
                c.Info.text = ProduceText(c.Def, mult);
                c.BuyLabel.text = Loc.T("ui_comprar") + " x" + shownCount + "\n" + NumberFormat.Fmt(cost);
                c.Buy.interactable = count > 0 && s.Essence >= cost;
            }
        }
    }
}
