using UnityEngine;
using UnityEngine.UI;

namespace IMK.SettingsUI.App
{
    public sealed class DirtyIndicator : MonoBehaviour
    {
        private Image _img; private System.Func<bool> _isDirty; private Color _baseColor; private float _t;
        public void Init(Image img, System.Func<bool> isDirty) { _img = img; _isDirty = isDirty; if (_img != null) _baseColor = _img.color; }
        void Update()
        {
            if (_img == null || _isDirty == null) return;
            if (_isDirty())
            {
                _t += Time.unscaledDeltaTime * 2f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(_t);
                _img.color = Color.Lerp(Theme.ThemeColors.Accent, Color.white, pulse * 0.25f);
            }
            else
            {
                _t = 0f; _img.color = _baseColor;
            }
        }
    }
}
