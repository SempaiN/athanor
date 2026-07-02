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

            // Fondo del laboratorio: degradado vertical sutil, flat
            var bg = Ui.Panel("Background", canvas.transform, Color.white, rounded: false);
            bg.sprite = UiTheme.VerticalGradient();
            bg.type = Image.Type.Simple;
            bg.raycastTarget = false;
            Ui.Fill(bg.rectTransform);

            // Contenedor ajustado al área segura (notch, barra de gestos)
            var safeRoot = Ui.Rect("SafeArea", canvas.transform);
            Ui.Fill(safeRoot);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Pantalla principal
            canvasGo.AddComponent<MainScreen>().Build(safeRoot);

            // Aviso de nueva versión (GitHub Releases) — quitar si se publica en una store
            canvasGo.AddComponent<Athanor.Infra.UpdateChecker>().Init(safeRoot);
        }
    }
}
