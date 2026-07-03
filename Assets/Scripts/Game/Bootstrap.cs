using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Athanor.UI;

namespace Athanor.Game
{
    /// Arranque 100% por código: cámara, EventSystem, Canvas y pantalla principal.
    /// La escena puede estar vacía; todo se construye acá.
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            // Cámara (solo limpia el fondo; la UI es ScreenSpaceOverlay)
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.Background;
            cam.cullingMask = 0;

            // Input para uGUI
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Controlador del juego + audio (el orden importa: audio lee el estado)
            var gameGo = new GameObject("GameController");
            Object.DontDestroyOnLoad(gameGo);
            gameGo.AddComponent<GameController>();
            gameGo.AddComponent<AudioManager>();

            // Canvas raíz (referencia 1080×1920 portrait)
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Portrait: fijar SIEMPRE 1080 unidades de ancho; el alto sobrante queda libre.
            // (Con 0.5, en pantallas 20:9 como Pixel 8 Pro el ancho útil caía a ~965 y la UI se cortaba.)
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo del laboratorio: ilustración si existe, degradado si no
            var bgHolder = Ui.Rect("Background", canvas.transform);
            Ui.Fill(bgHolder);
            var bakedBg = Resources.Load<Sprite>("Art/Core/lab_fondo");
            if (bakedBg != null)
            {
                var illus = Ui.Panel("Illustration", bgHolder, Color.white, rounded: false);
                illus.sprite = bakedBg;
                illus.type = Image.Type.Simple;
                illus.raycastTarget = false;
                Ui.Place(illus.rectTransform, 0, 0, 1080, 1920);
                var fitter = illus.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = 1080f / 1920f;

                // velo oscuro sutil para que la UI y el matraz respiren sobre la ilustración
                var veil = Ui.Panel("Veil", bgHolder, new Color(0.03f, 0.02f, 0.06f, 0.30f), rounded: false);
                veil.raycastTarget = false;
                Ui.Fill(veil.rectTransform);
            }
            else
            {
                var bg = Ui.Panel("Gradient", bgHolder, Color.white, rounded: false);
                bg.sprite = UiTheme.VerticalGradient();
                bg.type = Image.Type.Simple;
                bg.raycastTarget = false;
                Ui.Fill(bg.rectTransform);
            }

            // Contenedor ajustado al área segura (notch, barra de gestos)
            var safeRoot = Ui.Rect("SafeArea", canvas.transform);
            Ui.Fill(safeRoot);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Pantalla principal
            canvasGo.AddComponent<MainScreen>().Build(safeRoot);

            // Aviso de nueva versión (GitHub Releases). En el build de Play Store se
            // compila fuera (símbolo PLAY_STORE): Play prohíbe avisar de updates externos.
#if !PLAY_STORE
            canvasGo.AddComponent<Athanor.Infra.UpdateChecker>().Init(safeRoot);
#endif
        }
    }
}
