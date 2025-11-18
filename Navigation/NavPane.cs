using UnityEngine;
using System.Collections.Generic;
using IMK.SettingsUI.Providers;
using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.Navigation
{
    public sealed class NavPane : MonoBehaviour
    {
        private RectTransform _rt;
        public System.Action<string> OnSelect; // provider or page id
        void Awake(){ _rt = GetComponent<RectTransform>(); if (_rt==null) _rt = gameObject.AddComponent<RectTransform>(); }
        public void BuildFromProviders(IReadOnlyDictionary<string,ISettingsProvider> providers)
        {
            for (int i=transform.childCount-1;i>=0;i--) Destroy(transform.GetChild(i).gameObject);
            float y=-Theme.ThemeMetrics.NavItemSpacing; int idx=0;
            var ordered = ProviderPreferences.BuildOrderedList(providers);
            foreach (var it in ordered)
            {
                if (!it.pref.Enabled) continue;
                var id = it.id; var title = it.title; var btn = MakeButton(title, idx, y); string cap=id; btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=> OnSelect?.Invoke(cap)); y -= (Theme.ThemeMetrics.NavItemHeight + Theme.ThemeMetrics.NavItemSpacing); idx++;
            }
        }
        public void Refresh(){ if (IMK.SettingsUI.Providers.ProviderRegistry.All != null) BuildFromProviders(IMK.SettingsUI.Providers.ProviderRegistry.All); }
        private GameObject MakeButton(string text, int index, float y)
        {
            var btn = new GameObject($"Nav_{index}_{text}"); btn.transform.SetParent(transform,false);
            var rt = btn.AddComponent<RectTransform>(); rt.anchorMin=new Vector2(0f,1f); rt.anchorMax=new Vector2(1f,1f); rt.pivot=new Vector2(0.5f,1f); rt.anchoredPosition=new Vector2(0f,y); rt.sizeDelta=new Vector2(0f,Theme.ThemeMetrics.NavItemHeight);
            var img = btn.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem;
            var b = btn.AddComponent<UnityEngine.UI.Button>(); var colors = b.colors; colors.highlightedColor = Theme.ThemeColors.NavItemHover; colors.pressedColor = Theme.ThemeColors.Accent; b.colors = colors;
            var t = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); t.transform.SetParent(btn.transform,false); t.font = Theme.ThemeColors.DefaultFont; t.color = Color.white; t.alignment = TextAnchor.MiddleLeft; t.text = text; var tr = t.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(8,0); tr.offsetMax=new Vector2(-8,0);
            return btn;
        }
        public void BuildDemo(){ BuildFromProviders(new Dictionary<string,ISettingsProvider>()); }
    }
}
