using UnityEngine;

namespace Athanor.UI
{
    /// Ajusta este RectTransform al área segura (notch, cámara, barra de gestos).
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect applied = Rect.zero;

        void Update()
        {
            if (Screen.safeArea != applied) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            var rt = (RectTransform)transform;
            Vector2 min = applied.position;
            Vector2 max = applied.position + applied.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
