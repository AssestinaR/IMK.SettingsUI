using UnityEngine;

namespace IMK.SettingsUI.Theme
{
    /// <summary>Small helper to create common UI primitives with consistent styling.</summary>
    public static class UIFactory
    {
        public static UnityEngine.UI.Text MakeText(Transform parent, string name, int fontSize, Color color, TextAnchor anchor)
        {
            var go = new GameObject(string.IsNullOrEmpty(name)? "Text" : name);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.transform.SetParent(parent, false);
            t.font = ThemeColors.DefaultFont; t.fontSize = fontSize; t.color = color; t.alignment = anchor; t.raycastTarget = false;
            var rt = t.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0.5f,0.5f);
            return t;
        }
        public static UnityEngine.UI.Button MakeButton(Transform parent, string name, string label, System.Action onClick)
        {
            var go = new GameObject(string.IsNullOrEmpty(name)? "Button" : name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f); rt.pivot = new Vector2(0.5f,0.5f);
            var img = go.AddComponent<UnityEngine.UI.Image>(); img.color = ThemeColors.NavItem;
            var btn = go.AddComponent<UnityEngine.UI.Button>(); var colors = btn.colors; colors.highlightedColor = ThemeColors.NavItemHover; colors.pressedColor = ThemeColors.Accent; btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(()=> onClick());
            var txt = MakeText(go.transform, "Text", 14, Color.white, TextAnchor.MiddleCenter); txt.text = label;
            return btn;
        }
        public static UnityEngine.UI.Image MakePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(string.IsNullOrEmpty(name)? "Panel" : name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>(); img.color = bg; var rt = img.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero; return img;
        }
    }
}
