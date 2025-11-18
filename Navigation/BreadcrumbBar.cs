using System.Collections.Generic;
using UnityEngine;

namespace IMK.SettingsUI.Navigation
{
    public sealed class BreadcrumbBar : MonoBehaviour
    {
        public System.Action<string> OnNavigate;
        private RectTransform _rt;
        void Awake(){ _rt = GetComponent<RectTransform>(); if (_rt==null) _rt = gameObject.AddComponent<RectTransform>(); }
        public void SetSegments(IReadOnlyList<(string id,string title)> chain)
        {
            Debug.Log("[SettingsUI.Breadcrumb] SetSegments chain="+chain.Count);
            for (int i=_rt.childCount-1;i>=0;i--) Object.DestroyImmediate(_rt.GetChild(i).gameObject);
            float x=0f; float h = _rt.rect.height>0? _rt.rect.height:36f;
            for (int i=0;i<chain.Count;i++)
            {
                var (id,title) = chain[i]; bool isLast = i==chain.Count-1;
                var seg = new GameObject($"seg_{i}"); seg.transform.SetParent(_rt,false);
                var rt = seg.AddComponent<RectTransform>(); rt.anchorMin=new Vector2(0f,0f); rt.anchorMax=new Vector2(0f,1f); rt.pivot=new Vector2(0f,0.5f); rt.anchoredPosition=new Vector2(x,0); float w=Mathf.Max(60f,title.Length*9f+16f); rt.sizeDelta=new Vector2(w,0f);
                var img = seg.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.BreadcrumbSeg; img.raycastTarget=true;
                var btn = seg.AddComponent<UnityEngine.UI.Button>(); btn.interactable = !isLast; var colors = btn.colors; colors.highlightedColor = Theme.ThemeColors.BreadcrumbSegHover; colors.pressedColor = Theme.ThemeColors.Accent; btn.colors = colors; if (!isLast){ string cap=id; btn.onClick.AddListener(()=> OnNavigate?.Invoke(cap)); }
                var t = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); t.transform.SetParent(seg.transform,false); t.font = Theme.ThemeColors.DefaultFont; t.color = Color.white; t.alignment = TextAnchor.MiddleLeft; t.text = title; var tr=t.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(8,0); tr.offsetMax=new Vector2(-8,0);
                x += w + 8f;
                if (i<chain.Count-1)
                {
                    var sep = new GameObject("sep").AddComponent<UnityEngine.UI.Text>();
                    sep.transform.SetParent(_rt,false);
                    sep.font = Theme.ThemeColors.DefaultFont; sep.color = Theme.ThemeColors.TextDim; sep.text = "\u203A"; sep.alignment = TextAnchor.MiddleCenter;
                    var srt = sep.GetComponent<RectTransform>();
                    srt.anchorMin=new Vector2(0f,0.5f); srt.anchorMax=new Vector2(0f,0.5f); srt.pivot=new Vector2(0.5f,0.5f);
                    srt.anchoredPosition=new Vector2(x + 9f,0); // center arrow in its 18px slot
                    srt.sizeDelta=new Vector2(18f,18f);
                    x += 18f;
                }
            }
        }
    }
}
