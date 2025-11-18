using UnityEngine; using UnityEngine.UI; using System.Collections;
namespace IMK.SettingsUI.Cards
{
    public sealed class MarkdownExpandable : MonoBehaviour
    {
        public Text TitleText; public Text ContentText;
        public float InitialHeight = 240f; public float TitleHeight = 30f; public float PaddingTop = 8f; public float PaddingBottom = 8f; public float PaddingX = 8f;
        private RectTransform _rt; private bool _expanded; private Coroutine _anim;
        void Awake(){ _rt = GetComponent<RectTransform>(); }
        public void TryExpand()
        {
            if (_expanded) return; if (_rt==null || ContentText==null) return;
            float preferred = ContentText.preferredHeight + TitleHeight + PaddingTop + PaddingBottom;
            float target = Mathf.Max(preferred, InitialHeight);
            if (target <= _rt.sizeDelta.y + 0.5f) return; // no need
            _expanded = true; if (_anim!=null) StopCoroutine(_anim); _anim = StartCoroutine(AnimateHeight(_rt.sizeDelta.y, target));
        }
        private IEnumerator AnimateHeight(float from, float to)
        {
            float dur = 0.28f; float t=0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float p = Mathf.Clamp01(t/dur); // ease out cubic
                float ease = 1f - Mathf.Pow(1f-p,3f);
                float h = Mathf.Lerp(from,to,ease);
                _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, h);
                yield return null;
            }
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, to);
            _anim=null; // relayout parent stack
            var stack = GetComponentInParent<IMK.SettingsUI.Layout.StackPanelLayout>(); if (stack!=null) stack.PerformLayout();
        }
    }
}
