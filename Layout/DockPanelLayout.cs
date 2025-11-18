using UnityEngine;

namespace IMK.SettingsUI.Layout
{
    public enum DockSide { Top, Left, Right, Bottom, Fill }
    public sealed class DockPanelLayout : MonoBehaviour
    {
        void OnTransformChildrenChanged(){ Layout(); }
        void OnRectTransformDimensionsChange(){ Layout(); }
        public void Layout()
        {
            var rt = GetComponent<RectTransform>(); if (rt==null) return;
            float x=0f, y=0f, w=rt.rect.width, h=rt.rect.height;
            for (int i=0;i<transform.childCount;i++)
            {
                var ch = transform.GetChild(i).gameObject; var c = ch.GetComponent<DockPanelChild>(); var crt = ch.GetComponent<RectTransform>(); if (crt==null) continue;
                if (c==null || c.Side==DockSide.Fill){ crt.anchorMin=new Vector2(0f,1f); crt.anchorMax=new Vector2(0f,1f); crt.pivot=new Vector2(0f,1f); crt.anchoredPosition=new Vector2(x,-y); crt.sizeDelta=new Vector2(w,h); }
                else if (c.Side==DockSide.Top){ crt.anchorMin=new Vector2(0f,1f); crt.anchorMax=new Vector2(0f,1f); crt.pivot=new Vector2(0f,1f); crt.anchoredPosition=new Vector2(0f,-y); crt.sizeDelta=new Vector2(w,c.Size); y += c.Size; h -= c.Size; }
                else if (c.Side==DockSide.Bottom){ crt.anchorMin=new Vector2(0f,1f); crt.anchorMax=new Vector2(0f,1f); crt.pivot=new Vector2(0f,1f); crt.anchoredPosition=new Vector2(0f,-(rt.rect.height-c.Size)); crt.sizeDelta=new Vector2(w,c.Size); h -= c.Size; }
                else if (c.Side==DockSide.Left){ crt.anchorMin=new Vector2(0f,1f); crt.anchorMax=new Vector2(0f,1f); crt.pivot=new Vector2(0f,1f); crt.anchoredPosition=new Vector2(x,-y); crt.sizeDelta=new Vector2(c.Size,h); x += c.Size; w -= c.Size; }
                else if (c.Side==DockSide.Right){ crt.anchorMin=new Vector2(0f,1f); crt.anchorMax=new Vector2(0f,1f); crt.pivot=new Vector2(0f,1f); crt.anchoredPosition=new Vector2(rt.rect.width-c.Size,-y); crt.sizeDelta=new Vector2(c.Size,h); w -= c.Size; }
            }
        }
    }
    public sealed class DockPanelChild : MonoBehaviour { public DockSide Side = DockSide.Fill; public float Size = 32f; }
}
