using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMK.SettingsUI.Layout
{
    public sealed class ItemsControl : MonoBehaviour
    {
        public Func<object, GameObject, GameObject> ItemTemplate;
        private readonly List<GameObject> _live = new List<GameObject>();
        private RectTransform _rt; private StackPanelLayout _stack;
        void Awake(){ _rt = GetComponent<RectTransform>(); if (_rt==null) _rt = gameObject.AddComponent<RectTransform>(); _stack = GetComponent<StackPanelLayout>(); }
        public void Clear()
        {
            for (int i=transform.childCount-1;i>=0;i--)
            {
                var go = transform.GetChild(i).gameObject;
                try { DestroyImmediate(go); } catch { Destroy(go); }
            }
            _live.Clear();
            _stack?.MarkDirty();
        }
        public void SetItems(IEnumerable<object> data)
        {
            if (ItemTemplate == null){ Clear(); return; }
            Clear(); if (data==null) return;
            int index = 0;
            foreach (var it in data)
            {
                var go = new GameObject("Item"); go.transform.SetParent(transform,false);
                var rt = go.AddComponent<RectTransform>(); rt.anchorMin=new Vector2(0f,1f); rt.anchorMax=new Vector2(1f,1f); rt.pivot=new Vector2(0.5f,1f);
                go = ItemTemplate(it, go) ?? go;
                go.transform.SetSiblingIndex(index);
                _live.Add(go);
                index++;
            }
            _stack?.MarkDirty();
        }
    }
}
