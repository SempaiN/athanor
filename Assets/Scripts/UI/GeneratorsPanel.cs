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

        sealed class UpgradeCard
        {
            public UpgradeDef Def;
            public Image Bg, Stripe;
            public Text Name, Desc;
            public Button Buy;
            public Text BuyLabel;
        }

        readonly List<Card> cards = new List<Card>();
        readonly List<UpgradeCard> upgradeCards = new List<UpgradeCard>();
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

                // Icon-chip tonal (fila estilo Material): contenedor suave + icono a color
                var chipColor = g.Produces.Length > 0
                    ? UiTheme.ElementColor(ElementCatalog.Get(g.Produces[0]).ColorHex)
                    : UiTheme.Violet;
                var chip = Ui.Panel("IconChip", bg.transform, UiTheme.Tint(chipColor, 0.15f));
                chip.raycastTarget = false;
                Ui.Anchor(chip.rectTransform, new Vector2(0f, 0.5f), new Vector2(20, 8), new Vector2(84, 84));

                var icon = Ui.Panel("Icon", chip.transform, g.Produces.Length > 0
                    ? ProceduralIcons.TintFor(g.Produces[0], chipColor)
                    : UiTheme.Violet);
                icon.sprite = g.Produces.Length > 0 ? ProceduralIcons.For(g.Produces[0]) : ProceduralIcons.Diamond();
                icon.type = Image.Type.Simple;
                icon.raycastTarget = false;
                Ui.Place(icon.rectTransform, 0, 0, 56, 56);

                card.Name = Ui.Label("Name", bg.transform, "", 40, UiTheme.TextMain,
                                     TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(card.Name.rectTransform, new Vector2(0f, 1f), new Vector2(122, -16), new Vector2(440, 50));

                card.Owned = Ui.Label("Owned", bg.transform, "", 38, UiTheme.Amber,
                                      TextAnchor.MiddleRight, FontStyle.Bold);
                Ui.Anchor(card.Owned.rectTransform, new Vector2(1f, 1f), new Vector2(-300, -14), new Vector2(150, 52));

                card.Info = Ui.Label("Info", bg.transform, "", 28, UiTheme.TextDim, TextAnchor.UpperLeft);
                Ui.Anchor(card.Info.rectTransform, new Vector2(0f, 1f), new Vector2(122, -70), new Vector2(540, 70));

                card.Buy = Ui.TextButton("Buy", bg.transform, UiTheme.Green, out card.BuyLabel);
                card.BuyLabel.fontSize = 30;
                Ui.Anchor((RectTransform)card.Buy.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(250, 120));
                card.Buy.gameObject.AddComponent<RepeatButton>(); // mantener = comprar en cadena
                var def = g;
                card.Buy.onClick.AddListener(() => OnBuy(def));

                cards.Add(card);
                i++;
            }

            // Sección de mejoras globales (compra única) al final de la lista
            float sectionY = cards.Count * RowH + 14;
            const float TitleH = 76;

            var sectionTitle = Ui.Label("UpgradesTitle", content, Loc.T("ui_mejoras_titulo"), 36,
                                        UiTheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            var stRt = sectionTitle.rectTransform;
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.pivot = new Vector2(0.5f, 1);
            stRt.anchoredPosition = new Vector2(0, -sectionY);
            stRt.sizeDelta = new Vector2(0, TitleH);

            float upgradesBase = sectionY + TitleH;
            for (int u = 0; u < UpgradeCatalog.All.Count; u++)
            {
                var def = UpgradeCatalog.All[u];
                var uc = new UpgradeCard { Def = def };
                uc.Bg = Ui.Panel("Up_" + def.Id, content, UiTheme.Card);
                var upRt = uc.Bg.rectTransform;
                upRt.anchorMin = new Vector2(0, 1);
                upRt.anchorMax = new Vector2(1, 1);
                upRt.pivot = new Vector2(0.5f, 1);
                upRt.anchoredPosition = new Vector2(0, -(upgradesBase + u * RowH) - 10);
                upRt.offsetMin = new Vector2(10, upRt.offsetMin.y);
                upRt.offsetMax = new Vector2(-10, upRt.offsetMax.y);
                upRt.sizeDelta = new Vector2(upRt.sizeDelta.x, RowH - 10);

                uc.Stripe = Ui.Panel("Stripe", uc.Bg.transform, UiTheme.Gold, rounded: false);
                uc.Stripe.raycastTarget = false;
                Ui.Anchor(uc.Stripe.rectTransform, new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(10, RowH - 10));

                uc.Name = Ui.Label("Name", uc.Bg.transform, def.Name, 40, UiTheme.TextMain,
                                   TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Anchor(uc.Name.rectTransform, new Vector2(0f, 1f), new Vector2(34, -16), new Vector2(600, 50));

                uc.Desc = Ui.Label("Desc", uc.Bg.transform, def.Desc, 30, UiTheme.TextDim, TextAnchor.UpperLeft);
                Ui.Anchor(uc.Desc.rectTransform, new Vector2(0f, 1f), new Vector2(34, -72), new Vector2(600, 66));

                uc.Buy = Ui.TextButton("Buy", uc.Bg.transform, UiTheme.Gold, out uc.BuyLabel);
                uc.BuyLabel.fontSize = 30;
                Ui.Anchor((RectTransform)uc.Buy.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(250, 120));
                var d = def;
                uc.Buy.onClick.AddListener(() => game.BuyUpgrade(d));

                upgradeCards.Add(uc);
            }

            content.sizeDelta = new Vector2(0, upgradesBase + upgradeCards.Count * RowH + 40);
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

        static string ProduceText(GeneratorDef g, int owned, double mult)
        {
            if (g.AutoCombine) return Loc.T("ui_combina_solo");
            var names = string.Join(" + ", g.Produces.Select(e => Loc.T(ElementCatalog.Get(e).NameKey)));
            double milestone = GeneratorCatalog.MilestoneMult(owned);
            double total = g.BaseProd * owned * milestone * mult / g.Produces.Length;
            int next = GeneratorCatalog.NextMilestone(owned);

            string line = owned > 0
                ? Loc.T("ui_produce") + " " + NumberFormat.Fmt(total) + "/s " + names
                : Loc.T("ui_produce") + " " + NumberFormat.Fmt(g.BaseProd * mult / g.Produces.Length) + "/s " + names;
            if (milestone > 1) line += "  (x" + NumberFormat.Fmt(milestone) + ")";
            if (next > 0) line += "\n" + Loc.T("ui_hito") + " " + next + " -> x2";
            return line;
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
                c.Info.text = ProduceText(c.Def, owned, mult);
                c.BuyLabel.text = Loc.T("ui_comprar") + " x" + shownCount + "\n" + NumberFormat.Fmt(cost);
                c.Buy.interactable = count > 0 && s.Essence >= cost;
            }

            foreach (var uc in upgradeCards)
            {
                bool ownedUp = s.UpgradesOwned.Contains(uc.Def.Id);
                bool visible = ownedUp || s.LifetimeEssence >= uc.Def.Cost * 0.25;

                uc.Name.text = visible ? uc.Def.Name : Loc.T("ui_desconocido");
                uc.Desc.text = visible ? uc.Def.Desc : "";
                uc.Stripe.color = ownedUp ? UiTheme.Gold : new Color(1, 1, 1, 0.06f);
                uc.Bg.color = ownedUp
                    ? UiTheme.Card
                    : new Color(UiTheme.Card.r, UiTheme.Card.g, UiTheme.Card.b, 0.75f);

                if (ownedUp)
                {
                    uc.BuyLabel.text = Loc.T("ui_adquirida");
                    uc.Buy.interactable = false;
                }
                else
                {
                    uc.BuyLabel.text = visible
                        ? Loc.T("ui_comprar") + "\n" + NumberFormat.Fmt(uc.Def.Cost)
                        : Loc.T("ui_desconocido");
                    uc.Buy.interactable = visible && s.Essence >= uc.Def.Cost;
                }
            }
        }
    }
}
