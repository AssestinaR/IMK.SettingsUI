using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.App
{
    public sealed class DirtyIndicator : MonoBehaviour
    {
        private Image _img; private System.Func<bool> _isDirty; private Color _baseColor; private Coroutine _pulse; private float _t;
        public void Init(Image img, System.Func<bool> isDirty){ _img=img; _isDirty=isDirty; if (_img!=null) _baseColor=_img.color; }
        void Update()
        {
            if (_img==null || _isDirty==null) return;
            bool dirty = _isDirty();
            if (dirty)
            {
                _t += Time.unscaledDeltaTime * 2f; float pulse = 0.5f + 0.5f*Mathf.Sin(_t);
                var accent = Theme.ThemeColors.Accent; _img.color = Color.Lerp(accent, Color.white, pulse*0.25f);
            }
            else
            {
                _t = 0f; _img.color = _baseColor;
            }
        }
    }
}
