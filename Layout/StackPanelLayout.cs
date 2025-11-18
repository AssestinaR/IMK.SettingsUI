using UnityEngine;

namespace IMK.SettingsUI.Layout
{
    [ExecuteAlways]
    public sealed class StackPanelLayout : MonoBehaviour
    {
        public float Spacing = 8f;
        private RectTransform _rt;
        private bool _dirty = true;
        void Awake() { _rt = GetComponent<RectTransform>(); MarkDirty(); }
        void OnEnable() { MarkDirty(); }
        void OnTransformChildrenChanged() { MarkDirty(); }
        void OnRectTransformDimensionsChange() { MarkDirty(); }
        public void MarkDirty() { _dirty = true; }
        void LateUpdate() { if (_dirty) { _dirty = false; PerformLayout(); } }
        public void PerformLayout()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>(); if (_rt == null) return;
            float y = 0f; int count = 0; float spacing = Theme.ThemeMetrics.CardSpacing;
            for (int i = 0; i < transform.childCount; i++)
            {
                var ch = transform.GetChild(i) as RectTransform; if (ch == null || !ch.gameObject.activeSelf) continue; count++;
                ch.anchorMin = new Vector2(0f, 1f); ch.anchorMax = new Vector2(1f, 1f); ch.pivot = new Vector2(0.5f, 1f);
                ch.anchoredPosition = new Vector2(0f, -y);
                y += ch.sizeDelta.y + spacing;
            }
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, count == 0 ? 0f : y + spacing);
        }
    }
}
