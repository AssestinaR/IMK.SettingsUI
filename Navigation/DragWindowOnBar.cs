using UnityEngine;
using UnityEngine.EventSystems;

namespace IMK.SettingsUI.Navigation
{
    /// <summary>Allow dragging the settings window by dragging anywhere on the top bar (breadcrumb area).
    /// Uses a small threshold so simple clicks on buttons仍然可触发，超过阈值则进入拖拽。</summary>
    public sealed class DragWindowOnBar : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Window;
        private bool _dragging;
        private Vector2 _startMouse;
        private Vector2 _startPos;
        private const float Threshold = 4f;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Window == null) return;
            _dragging = false; _startMouse = eventData.position; _startPos = Window.anchoredPosition;
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Window == null) return;
            var delta = (Vector2)eventData.position - _startMouse;
            if (delta.sqrMagnitude >= Threshold*Threshold) _dragging = true;
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (Window == null) return;
            if (!_dragging)
            {
                var delta0 = (Vector2)eventData.position - _startMouse;
                if (delta0.sqrMagnitude < Threshold*Threshold) return;
                _dragging = true;
            }
            var delta = (Vector2)eventData.position - _startMouse;
            // For top-left anchored window, increasing anchoredPosition.y moves the rect upward relative to the top anchor.
            Window.anchoredPosition = _startPos + new Vector2(delta.x, delta.y);
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }
    }
}
