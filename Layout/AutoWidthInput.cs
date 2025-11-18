using UnityEngine;
using UnityEngine.UI;

namespace IMK.SettingsUI.Layout
{
    /// <summary>
    /// Expands the attached input host width leftwards (anchored to right) based on the text's preferred width.
    /// Expect host RectTransform to have anchorMin.x=anchorMax.x=1.
    /// </summary>
    public sealed class AutoWidthInput : MonoBehaviour
    {
        public InputField Target;
        public float MinWidth = 180f;
        public float MaxWidth = 320f;
        public float ExtraPadding = 20f;
        private RectTransform _rt;
        private float _rightOffset; // cached offsetMax.x

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (_rt != null) _rightOffset = _rt.offsetMax.x;
        }
        void OnEnable()
        {
            TryHook();
            UpdateWidth();
        }
        void OnDisable()
        {
            Unhook();
        }
        private UnityEngine.Events.UnityAction<string> _cachedHandler;
        void TryHook()
        {
            if (Target == null) Target = GetComponent<InputField>();
            if (Target != null && _cachedHandler == null)
            {
                _cachedHandler = _ => UpdateWidth();
                Target.onValueChanged.AddListener(_cachedHandler);
            }
        }
        void Unhook()
        {
            if (Target != null && _cachedHandler != null)
            {
                Target.onValueChanged.RemoveListener(_cachedHandler);
                _cachedHandler = null;
            }
        }
        void UpdateWidth()
        {
            if (_rt == null || Target == null || Target.textComponent == null) return;
            var pref = Target.textComponent.preferredWidth + ExtraPadding;
            var w = Mathf.Clamp(pref, MinWidth, MaxWidth);
            // expand to left by adjusting offsetMin.x; keep offsetMax.x constant
            var offMin = _rt.offsetMin; offMin.x = _rightOffset - w; _rt.offsetMin = offMin;
            // optional: keep height offsets unchanged
        }
    }
}
