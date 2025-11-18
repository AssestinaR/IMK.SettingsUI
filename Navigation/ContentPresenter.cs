using System.Collections;
using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Layout;
using UnityEngine;

namespace IMK.SettingsUI.Navigation
{
    public sealed class ContentPresenter : MonoBehaviour
    {
        private RectTransform _rt;
        private ItemsControl _itemsCurrent;
        private StackPanelLayout _stackCurrent;
        private List<ICardModel> _models = new List<ICardModel>();
        private UnityEngine.UI.ScrollRect _scroll;
        private RectTransform _viewport;
        private RectTransform _contentCurrent;
        // reveal overlay and next panel
        private RectTransform _reveal; // mask rect whose width animates
        private UnityEngine.UI.RectMask2D _revealMask;
        private RectTransform _contentNext;
        private ItemsControl _itemsNext;
        private StackPanelLayout _stackNext;
        private Coroutine _anim;
        private RectTransform _cover; // overlay covering old content during wipe
        private bool _firstShow = true;
        private float _wheelVelocity = 0f; private bool _wheelActive = false; private const float WheelStep = 0.18f; private const float WheelDecay = 8f; // higher=faster decay
        private bool _isPointerDragging = false;

        void Awake()
        {
            _rt = GetComponent<RectTransform>(); if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>(); viewport.SetParent(transform, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero; viewport.pivot = new Vector2(0.5f, 0.5f);
            var vpImg = viewport.gameObject.AddComponent<UnityEngine.UI.Image>(); vpImg.color = new Color(0, 0, 0, 0); vpImg.raycastTarget = true;
            var rectMask = viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            _viewport = viewport;
            // current content panel
            _contentCurrent = new GameObject("Content").AddComponent<RectTransform>(); _contentCurrent.SetParent(viewport, false);
            _contentCurrent.anchorMin = new Vector2(0f, 1f); _contentCurrent.anchorMax = new Vector2(1f, 1f); _contentCurrent.pivot = new Vector2(0.5f, 1f); _contentCurrent.anchoredPosition = Vector2.zero; _contentCurrent.sizeDelta = Vector2.zero;
            _scroll = gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); _scroll.viewport = viewport; _scroll.content = _contentCurrent; _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = Theme.ThemeMetrics.ScrollSensitivity;
            var dragEvents = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entryBegin = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag }; entryBegin.callback.AddListener(_ => { _isPointerDragging = true; _wheelVelocity = 0f; _wheelActive = false; }); dragEvents.triggers.Add(entryBegin);
            var entryEnd = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag }; entryEnd.callback.AddListener(_ => { _isPointerDragging = false; }); dragEvents.triggers.Add(entryEnd);
            _itemsCurrent = _contentCurrent.gameObject.AddComponent<ItemsControl>(); _stackCurrent = _contentCurrent.gameObject.AddComponent<StackPanelLayout>();
            _stackCurrent.Spacing = 8f; _itemsCurrent.ItemTemplate = (data, existing) => CardTemplates.Bind((ICardModel)data, existing);
            // reveal overlay
            _reveal = new GameObject("Reveal").AddComponent<RectTransform>(); _reveal.SetParent(viewport, false);
            _reveal.anchorMin = new Vector2(0f, 0f); _reveal.anchorMax = new Vector2(0f, 1f); _reveal.pivot = new Vector2(0f, 0.5f); _reveal.sizeDelta = new Vector2(0f, 0f); _reveal.anchoredPosition = new Vector2(0f, 0f);
            var blocker = _reveal.gameObject.AddComponent<UnityEngine.UI.Image>(); blocker.color = new Color(0, 0, 0, 0); blocker.raycastTarget = false; // transparent reveal container
            _revealMask = _reveal.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            // cover overlay (below reveal, above current content)
            _cover = new GameObject("Cover").AddComponent<RectTransform>(); _cover.SetParent(viewport, false);
            _cover.anchorMin = new Vector2(0f, 0f); _cover.anchorMax = new Vector2(0f, 1f); _cover.pivot = new Vector2(0f, 0.5f); _cover.sizeDelta = new Vector2(0f, 0f); _cover.anchoredPosition = Vector2.zero;
            var coverImg = _cover.gameObject.AddComponent<UnityEngine.UI.Image>(); coverImg.color = Theme.ThemeColors.ContentBg; coverImg.raycastTarget = false;
            // next content lives inside reveal
            _contentNext = new GameObject("NextContent").AddComponent<RectTransform>(); _contentNext.SetParent(_reveal, false);
            _contentNext.anchorMin = new Vector2(0f, 1f); _contentNext.anchorMax = new Vector2(1f, 1f); _contentNext.pivot = new Vector2(0.5f, 1f); _contentNext.anchoredPosition = Vector2.zero; _contentNext.sizeDelta = Vector2.zero;
            _itemsNext = _contentNext.gameObject.AddComponent<ItemsControl>(); _stackNext = _contentNext.gameObject.AddComponent<StackPanelLayout>();
            _stackNext.Spacing = 8f; _itemsNext.ItemTemplate = (data, existing) => CardTemplates.Bind((ICardModel)data, existing);
        }
        public float ViewportWidth => _viewport != null ? _viewport.rect.width : 0f;
        public void Set(List<ICardModel> models)
        {
            _models = models ?? new List<ICardModel>();
            _itemsCurrent.SetItems(ProjectModels(_models));
            Canvas.ForceUpdateCanvases();
            _stackCurrent.PerformLayout();
            // if viewport width was zero when first building, force a second layout next frame
            if (ViewportWidth <= 1f && gameObject.activeInHierarchy) StartCoroutine(DelayedLayoutFix());
            _scroll.scrollSensitivity = Theme.ThemeMetrics.ScrollSensitivity;
            _scroll.verticalNormalizedPosition = 1f;
        }
        public void SetWithTransition(List<ICardModel> models, bool forward)
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            var next = models ?? new List<ICardModel>();
            _itemsNext.SetItems(ProjectModels(next));
            _stackNext.PerformLayout(); Canvas.ForceUpdateCanvases();
            float vw = _viewport.rect.width; if (vw <= 0f) vw = ((RectTransform)transform).rect.width;
            if (forward)
            {
                _reveal.anchorMin = new Vector2(0f, 0f); _reveal.anchorMax = new Vector2(0f, 1f); _reveal.pivot = new Vector2(0f, 0.5f); _reveal.anchoredPosition = Vector2.zero; _reveal.sizeDelta = new Vector2(0f, 0f);
                _cover.anchorMin = new Vector2(0f, 0f); _cover.anchorMax = new Vector2(0f, 1f); _cover.pivot = new Vector2(0f, 0.5f); _cover.anchoredPosition = Vector2.zero; _cover.sizeDelta = new Vector2(0f, 0f);
            }
            else
            {
                _reveal.anchorMin = new Vector2(1f, 0f); _reveal.anchorMax = new Vector2(1f, 1f); _reveal.pivot = new Vector2(1f, 0.5f); _reveal.anchoredPosition = Vector2.zero; _reveal.sizeDelta = new Vector2(0f, 0f);
                _cover.anchorMin = new Vector2(1f, 0f); _cover.anchorMax = new Vector2(1f, 1f); _cover.pivot = new Vector2(1f, 0.5f); _cover.anchoredPosition = Vector2.zero; _cover.sizeDelta = new Vector2(0f, 0f);
            }
            _scroll.enabled = false;
            _contentCurrent.SetSiblingIndex(0); _cover.SetSiblingIndex(1); _reveal.SetSiblingIndex(2);
            _anim = StartCoroutine(WipeAnim(vw, forward, next));
        }
        private IEnumerator WipeAnim(float width, bool forward, List<ICardModel> next)
        {
            UITransitions.Begin();
            float dur = 0.22f; float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float p = Mathf.Clamp01(t / dur); float ease = p * p * (3f - 2f * p);
                float w = width * ease;
                _reveal.sizeDelta = new Vector2(w, 0f);
                _cover.sizeDelta = new Vector2(w, 0f);
                yield return null;
            }
            _reveal.sizeDelta = new Vector2(width, 0f);
            _cover.sizeDelta = new Vector2(width, 0f);
            _models = next;
            _itemsCurrent.SetItems(ProjectModels(_models));
            Canvas.ForceUpdateCanvases();
            _stackCurrent.PerformLayout();
            _itemsNext.SetItems(new List<object>());
            _reveal.sizeDelta = new Vector2(0f, 0f);
            _cover.sizeDelta = new Vector2(0f, 0f);
            _scroll.verticalNormalizedPosition = 1f;
            _scroll.enabled = true; _anim = null;
            UITransitions.End();
        }
        public IReadOnlyList<ICardModel> GetModels() => _models;
        public void ForceRebind()
        {
            _itemsCurrent.SetItems(ProjectModels(_models));
            Canvas.ForceUpdateCanvases(); _stackCurrent?.PerformLayout();
        }
        void Update() { if (_scroll != null) _scroll.scrollSensitivity = Theme.ThemeMetrics.ScrollSensitivity; HandleWheelSmooth(); }
        private void HandleWheelSmooth()
        {
            if (_scroll == null || !_scroll.enabled) return;
            if (_isPointerDragging) { _wheelVelocity = 0f; _wheelActive = false; return; }
            float delta = UnityEngine.Input.mouseScrollDelta.y; // typically + up, - down
            if (Mathf.Abs(delta) > 0.001f)
            {
                _wheelVelocity += delta * WheelStep;
                _wheelActive = true;
            }
            if (!_wheelActive) return;
            float decayFactor = Mathf.Exp(-WheelDecay * Time.unscaledDeltaTime);
            _wheelVelocity *= decayFactor;
            float posTarget = Mathf.Clamp01(_scroll.verticalNormalizedPosition + _wheelVelocity * Time.unscaledDeltaTime);
            float smooth = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            _scroll.verticalNormalizedPosition = Mathf.Lerp(_scroll.verticalNormalizedPosition, posTarget, smooth);
            if (Mathf.Abs(_wheelVelocity) < 0.0005f && Mathf.Abs(posTarget - _scroll.verticalNormalizedPosition) < 0.0005f)
            {
                _wheelVelocity = 0f; _wheelActive = false;
            }
        }
        void OnEnable()
        {
            Canvas.ForceUpdateCanvases();
            if (_stackCurrent != null) _stackCurrent.PerformLayout();
            if (gameObject.activeInHierarchy) StartCoroutine(InitialShowRoutine());
        }
        private IEnumerator InitialShowRoutine()
        {
            float w0 = ViewportWidth;
            Debug.Log("[SettingsUI.ContentPresenter] InitialShow frame0 width=" + w0 + " firstShow=" + _firstShow + " models=" + _models.Count);
            yield return null; // frame1
            Canvas.ForceUpdateCanvases(); _stackCurrent?.PerformLayout(); float w1 = ViewportWidth;
            Debug.Log("[SettingsUI.ContentPresenter] InitialShow frame1 width=" + w1);
            // Force rebind either first show or width change
            if (_firstShow || (w0 <= 5f && w1 > 50f))
            {
                Debug.Log("[SettingsUI.ContentPresenter] Rebinding all cards on initial show.");
                _itemsCurrent.SetItems(ProjectModels(_models));
                Canvas.ForceUpdateCanvases(); _stackCurrent?.PerformLayout();
                _firstShow = false;
            }
            yield return null; // frame2
            Canvas.ForceUpdateCanvases(); _stackCurrent?.PerformLayout(); float w2 = ViewportWidth;
            Debug.Log("[SettingsUI.ContentPresenter] InitialShow frame2 width=" + w2);
            _scroll.verticalNormalizedPosition = 1f;
        }
        private IEnumerator DelayedLayoutFix()
        {
            yield return null; // wait 1 frame
            Canvas.ForceUpdateCanvases(); _stackCurrent?.PerformLayout();
        }
        private static List<object> ProjectModels(List<ICardModel> src)
        {
            if (src == null || src.Count == 0) return new List<object>();
            var list = new List<object>(src.Count);
            for (int i = 0; i < src.Count; i++) list.Add(src[i]);
            return list;
        }
    }
}
