using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Athanor.Domain;
using Athanor.Game;

namespace Athanor.UI
{
    /// Progresión visual del laboratorio (GDD §7): props flat que aparecen por hitos.
    /// Todo procedural; cuando lleguen los assets reales se reemplazan 1:1.
    public sealed class LabDecor
    {
        GameController game;
        RectTransform root;

        GameObject mesa, estanteriaIzq, alambique, estanteriaDer, simbolos, candelabro, circulo, vitral;
        Image[] vitralPanes;

        static readonly ElementId[] Tier1 =
            { ElementId.Barro, ElementId.Lava, ElementId.Polvo, ElementId.Vapor, ElementId.Niebla, ElementId.Energia };
        static readonly ElementId[] TriaPrima =
            { ElementId.Sal, ElementId.Mercurio, ElementId.Azufre };

        public void Build(RectTransform parent)
        {
            game = GameController.Instance;
            root = Ui.Rect("LabDecor", parent);
            Ui.Fill(root);
            root.SetAsFirstSibling(); // detrás del matraz y los botones

            var wood = new Color(0.35f, 0.24f, 0.16f);     // madera oscura flat
            var woodLight = new Color(0.45f, 0.32f, 0.21f);
            var glass = new Color(0.75f, 0.89f, 0.90f, 0.5f);

            // Piso (siempre): franja inferior más oscura que da profundidad al laboratorio
            var piso = Prop("Piso");
            var pisoImg = Ui.Panel("Suelo", piso.transform, new Color(0f, 0f, 0f, 0.28f), rounded: false);
            pisoImg.raycastTarget = false;
            Ui.Anchor(pisoImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(1400, 200));
            var zocalo = Ui.Panel("Zocalo", piso.transform,
                new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.12f), rounded: false);
            zocalo.raycastTarget = false;
            Ui.Anchor(zocalo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(1400, 5));

            // Mesa (siempre visible): franja inferior
            mesa = Prop("Mesa");
            var mesaImg = Ui.Panel("Tabla", mesa.transform, wood, rounded: false);
            Ui.Anchor(mesaImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 180), new Vector2(1200, 46));
            mesaImg.raycastTarget = false;

            // Estantería izquierda + 3 frascos — hito: 1er generador
            estanteriaIzq = Prop("EstanteriaIzq");
            Shelf(estanteriaIzq.transform, -430, 520, woodLight,
                  new[] { UiTheme.ElementColor("#7FB069"), UiTheme.ElementColor("#3D9BB3"), UiTheme.ElementColor("#E4572E") });

            // Alambique derecha — hito: los 6 compuestos T1 descubiertos
            alambique = Prop("Alambique");
            var body = Ui.Panel("Cuerpo", alambique.transform, glass);
            body.sprite = UiTheme.Circle();
            body.type = Image.Type.Simple;
            body.raycastTarget = false;
            Ui.Anchor(body.rectTransform, new Vector2(1f, 0.5f), new Vector2(-50, 120), new Vector2(140, 140));
            var neck = Ui.Panel("Cuello", alambique.transform, glass, rounded: false);
            neck.raycastTarget = false;
            Ui.Anchor(neck.rectTransform, new Vector2(1f, 0.5f), new Vector2(-40, 210), new Vector2(120, 16));

            // Estantería derecha — hito: 10 generadores
            estanteriaDer = Prop("EstanteriaDer");
            Shelf(estanteriaDer.transform, 430, 520, woodLight,
                  new[] { UiTheme.ElementColor("#E8C547"), UiTheme.ElementColor("#9B72CF"), UiTheme.ElementColor("#B59F7E") });

            // Símbolos de pared — hito: Tria Prima
            simbolos = Prop("Simbolos");
            for (int i = 0; i < 3; i++)
            {
                var sym = Ui.Panel("Sym" + i, simbolos.transform, new Color(0.95f, 0.90f, 0.80f, 0.18f));
                sym.sprite = UiTheme.Circle();
                sym.type = Image.Type.Simple;
                sym.raycastTarget = false;
                Ui.Anchor(sym.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 200, -40), new Vector2(90 + i * 8, 90 + i * 8));
            }

            // Candelabro — hito: primer Oro
            candelabro = Prop("Candelabro");
            var stem = Ui.Panel("Pie", candelabro.transform, UiTheme.ElementColor("#8C5A3C"), rounded: false);
            stem.raycastTarget = false;
            Ui.Anchor(stem.rectTransform, new Vector2(0f, 0.5f), new Vector2(70, 140), new Vector2(14, 180));
            var flame = Ui.Panel("Llama", candelabro.transform, UiTheme.ElementColor("#F2C14E"));
            flame.sprite = UiTheme.Circle();
            flame.type = Image.Type.Simple;
            flame.raycastTarget = false;
            Ui.Anchor(flame.rectTransform, new Vector2(0f, 0.5f), new Vector2(64, 250), new Vector2(28, 38));

            // Círculo de transmutación completo — hito: Piedra Filosofal
            circulo = Prop("Circulo");
            var ring = Ui.Panel("Aro", circulo.transform, new Color(0.91f, 0.77f, 0.28f, 0.30f));
            ring.sprite = UiTheme.Circle();
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            Ui.Place(ring.rectTransform, 0, 90, 720, 720);
            var ringInner = Ui.Panel("AroInterior", circulo.transform, UiTheme.Background);
            ringInner.sprite = UiTheme.Circle();
            ringInner.type = Image.Type.Simple;
            ringInner.raycastTarget = false;
            Ui.Place(ringInner.rectTransform, 0, 90, 690, 690);

            // Vitral superior — un panel de color por prestigio (hasta 5)
            vitral = Prop("Vitral");
            vitralPanes = new Image[5];
            string[] cols = { "#9B72CF", "#3D9BB3", "#7FB069", "#E8C547", "#E4572E" };
            for (int i = 0; i < 5; i++)
            {
                var pane = Ui.Panel("Pane" + i, vitral.transform, UiTheme.ElementColor(cols[i]));
                pane.raycastTarget = false;
                Ui.Anchor(pane.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 2) * 130, -6), new Vector2(110, 26));
                vitralPanes[i] = pane;
            }

            Refresh();
        }

        GameObject Prop(string name)
        {
            var rt = Ui.Rect(name, root);
            Ui.Fill(rt);
            return rt.gameObject;
        }

        void Shelf(Transform parent, float x, float y, Color wood, Color[] flaskColors)
        {
            var board = Ui.Panel("Tabla", parent, wood, rounded: false);
            board.raycastTarget = false;
            Ui.Place(board.rectTransform, x, y, 220, 18);
            for (int i = 0; i < flaskColors.Length; i++)
            {
                var fl = Ui.Panel("Frasco" + i, parent, flaskColors[i]);
                fl.sprite = UiTheme.Circle();
                fl.type = Image.Type.Simple;
                fl.raycastTarget = false;
                Ui.Place(fl.rectTransform, x - 60 + i * 60, y + 34, 42, 42);
            }
        }

        public void Refresh()
        {
            var s = game.State;
            int totalGens = s.GeneratorsOwned.Values.Sum();

            estanteriaIzq.SetActive(totalGens >= 1);
            alambique.SetActive(Tier1.All(e => s.Discovered.Contains(e)));
            estanteriaDer.SetActive(totalGens >= 10);
            simbolos.SetActive(TriaPrima.All(e => s.Discovered.Contains(e)));
            candelabro.SetActive(s.Discovered.Contains(ElementId.Oro));
            circulo.SetActive(s.Discovered.Contains(ElementId.PiedraFilosofal));

            vitral.SetActive(s.PrestigeCount >= 1);
            for (int i = 0; i < vitralPanes.Length; i++)
                vitralPanes[i].gameObject.SetActive(s.PrestigeCount > i);
        }
    }
}
