using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Athanor.UI
{
    /// Feedback táctil: el botón se encoge levemente al presionar.
    public sealed class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        const float PressedScale = 0.95f;

        public void OnPointerDown(PointerEventData _)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            transform.localScale = Vector3.one * PressedScale;
        }

        public void OnPointerUp(PointerEventData _) =>
            transform.localScale = Vector3.one;

        void OnDisable() => transform.localScale = Vector3.one;
    }

    /// Mantener presionado repite el click (tras 0.45 s, 8 veces/s). Ideal para Combinar/Comprar.
    public sealed class RepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Delay = 0.45f;
        const float Interval = 0.125f;

        Button button;
        Coroutine loop;

        void Awake() => button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData _)
        {
            if (loop != null) StopCoroutine(loop);
            loop = StartCoroutine(Repeat());
        }

        public void OnPointerUp(PointerEventData _) => Stop();
        public void OnPointerExit(PointerEventData _) => Stop();
        void OnDisable() => Stop();

        void Stop()
        {
            if (loop != null) StopCoroutine(loop);
            loop = null;
        }

        IEnumerator Repeat()
        {
            yield return new WaitForSeconds(Delay);
            while (button != null && button.interactable)
            {
                button.onClick.Invoke();
                yield return new WaitForSeconds(Interval);
            }
        }
    }

    /// Animaciones utilitarias compartidas.
    public static class UiFx
    {
        /// Aparición con "pop": escala 0.85 → 1 con easing suave.
        public static IEnumerator PopIn(RectTransform rt, float duration = 0.18f)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float s = 0.85f + 0.15f * (1f - (1f - k) * (1f - k)); // ease-out
                if (rt == null) yield break;
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        /// Fundido de entrada de un panel al cambiar de pestaña.
        public static IEnumerator FadeIn(CanvasGroup cg, float duration = 0.15f)
        {
            if (cg == null) yield break;
            cg.alpha = 0;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                if (cg == null) yield break;
                cg.alpha = t / duration;
                yield return null;
            }
            if (cg != null) cg.alpha = 1;
        }
    }
}
