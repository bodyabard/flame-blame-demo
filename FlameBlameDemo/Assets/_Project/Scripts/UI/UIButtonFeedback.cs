using UnityEngine;
using UnityEngine.EventSystems;

namespace FlameBlame.UI
{
    /// Hover grows, press squashes. Works while timeScale is 0 because the runner uses unscaled time.
    public sealed class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        UITweenRunner tween;
        UIMotionPresets motion;
        RectTransform rect;
        bool hover;

        public void Initialize(UITweenRunner runner, UIMotionPresets presets)
        {
            tween = runner; motion = presets; rect = (RectTransform)transform;
        }

        void To(Vector3 scale, float seconds, Ease ease)
        {
            if (tween != null) tween.Scale(rect, rect.localScale, scale, seconds, ease);
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; To(Vector3.one * motion.hoverScale, 0.15f, Ease.OutBack); }
        public void OnPointerExit(PointerEventData e) { hover = false; To(Vector3.one, 0.15f, Ease.OutCubic); }
        public void OnPointerDown(PointerEventData e) { To(new Vector3(1.04f, 0.86f, 1f), 0.08f, Ease.OutCubic); }
        public void OnPointerUp(PointerEventData e) { To(hover ? Vector3.one * motion.hoverScale : Vector3.one, 0.2f, Ease.OutBack); }
    }
}
