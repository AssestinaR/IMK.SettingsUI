using UnityEngine;

namespace IMK.SettingsUI.App
{
    /// <summary>Entry point for the new Settings UI. Independent of IMK.UI.</summary>
    public static class SettingsShell
    {
        /// <summary>Semantic version of IMK.SettingsUI public API baseline.</summary>
        public const string Version = "1.0.0";

        private static GameObject _rootGo;
        private static Canvas _canvas;
        private static RectTransform _window;
        private static GameObject _left;
        private static GameObject _top;
        private static GameObject _content;
        private static RectTransform _contentInset;
        private static bool _inited;

        /// <summary>
        /// Initializes the Settings UI infrastructure (Canvas, EventSystem, window hierarchy). Idempotent.
        /// Mods may call this early if they need to register providers before first toggle.
        /// </summary>
        public static void Init()
        {
            if (_inited) return; _inited = true;
            Settings.SettingsStore.Load();
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("IMK.SettingsUI.EventSystem"); Object.DontDestroyOnLoad(es);
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            // Register core internal providers before building window so nav shows them immediately.
            try
            {
                IMK.SettingsUI.Providers.ProviderRegistry.Register(new IMK.SettingsUI.InternalMods.CoreShell.CoreShellProvider());
                IMK.SettingsUI.Providers.ProviderRegistry.Register(new IMK.SettingsUI.InternalMods.SettingsPanel.SettingsPanelProvider());
            }
            catch { }

            _rootGo = new GameObject("IMK.SettingsUI.Canvas"); Object.DontDestroyOnLoad(_rootGo);
            _canvas = _rootGo.AddComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 32761; _canvas.overrideSorting = true;
            _rootGo.AddComponent<UnityEngine.UI.CanvasScaler>(); _rootGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            _window = new GameObject("Window").AddComponent<RectTransform>(); _window.SetParent(_rootGo.transform, false);
            _window.anchorMin = new Vector2(0f, 1f); _window.anchorMax = new Vector2(0f, 1f); _window.pivot = new Vector2(0f, 1f);
            _window.sizeDelta = new Vector2(Theme.ThemeMetrics.WindowWidth, Theme.ThemeMetrics.WindowHeight);
            var screenW = Screen.width; var screenH = Screen.height; var posX = (screenW - Theme.ThemeMetrics.WindowWidth) * 0.5f; var posY = -(screenH - Theme.ThemeMetrics.WindowHeight) * 0.5f; _window.anchoredPosition = new Vector2(posX, posY);
            var bg = _window.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color = Theme.ThemeColors.WindowBg;
            _window.gameObject.AddComponent<Layout.DockPanelLayout>();

            _top = new GameObject("Breadcrumb"); _top.transform.SetParent(_window, false);
            var topChild = _top.AddComponent<Layout.DockPanelChild>(); topChild.Side = Layout.DockSide.Top; topChild.Size = 36f;
            var topRt = _top.AddComponent<RectTransform>(); var topImg = _top.AddComponent<UnityEngine.UI.Image>(); topImg.color = Theme.ThemeColors.TopBarBg;
            // Right-aligned Apply button on the top bar
            var applyBtnGo = new GameObject("ApplyButton"); applyBtnGo.transform.SetParent(_top.transform, false);
            var applyRt = applyBtnGo.AddComponent<RectTransform>(); applyRt.anchorMin = new Vector2(1f, 0.5f); applyRt.anchorMax = new Vector2(1f, 0.5f); applyRt.pivot = new Vector2(1f, 0.5f); applyRt.sizeDelta = new Vector2(90f, 24f); applyRt.anchoredPosition = new Vector2(-8f, 0f);
            var applyImg = applyBtnGo.AddComponent<UnityEngine.UI.Image>(); applyImg.color = Theme.ThemeColors.NavItem;
            var applyBtn = applyBtnGo.AddComponent<UnityEngine.UI.Button>(); var ac = applyBtn.colors; ac.highlightedColor = Theme.ThemeColors.NavItemHover; ac.pressedColor = Theme.ThemeColors.Accent; applyBtn.colors = ac;
            var applyTxt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); applyTxt.transform.SetParent(applyBtnGo.transform, false); applyTxt.font = Theme.ThemeColors.DefaultFont; applyTxt.color = Color.white; applyTxt.alignment = TextAnchor.MiddleCenter; applyTxt.text = "Apply"; var atr = applyTxt.GetComponent<RectTransform>(); atr.anchorMin = Vector2.zero; atr.anchorMax = Vector2.one; atr.offsetMin = Vector2.zero; atr.offsetMax = Vector2.zero;
            applyBtn.onClick.AddListener(() =>
            {
                var presenterRef = GameObject.Find("IMK.SettingsUI.Canvas/Window/Content/ContentInset")?.GetComponent<Navigation.ContentPresenter>();
                if (presenterRef != null)
                {
                    var models = presenterRef.GetModels();
                    var changed = IMK.SettingsUI.Settings.SettingsApplyService.Apply(models);
                    if (changed) { var imgApply = applyBtnGo.GetComponent<UnityEngine.UI.Image>(); if (imgApply != null) imgApply.color = Theme.ThemeColors.NavItem; }
                }
            });
            // dirty indicator coroutine
            applyBtnGo.AddComponent<DirtyIndicator>().Init(applyBtnGo.GetComponent<UnityEngine.UI.Image>(), () =>
            {
                var presenterRef = GameObject.Find("IMK.SettingsUI.Canvas/Window/Content/ContentInset")?.GetComponent<Navigation.ContentPresenter>();
                if (presenterRef == null) return false;
                foreach (var m in presenterRef.GetModels())
                {
                    if (m is Cards.BoundSettingCardModel b && b.Pending != null && !Equals(b.Pending, b.OriginalValue)) return true;
                    if (m is Cards.SettingCardModel s && s.Pending != null && !Equals(s.Pending, s.Initial)) return true;
                    if (m is Cards.ListSettingCardModel ls && ls.PendingValues != null)
                    {
                        if (ls.InitialValues == null || ls.PendingValues.Length != ls.InitialValues.Length) return true;
                        bool diff = false; for (int i = 0; i < ls.PendingValues.Length; i++) { if (!System.String.Equals(ls.PendingValues[i], ls.InitialValues[i])) { diff = true; break; } }
                        if (diff) return true;
                    }
                    if (m is IMK.SettingsUI.Cards.ToggleSliderSettingCardModel ts && ts.Pending != ts.Initial) return true;
                }
                return false;
            });
            // Breadcrumb content area that leaves space for the right button
            var bcHost = new GameObject("Content"); bcHost.transform.SetParent(_top.transform, false);
            var bcRt = bcHost.AddComponent<RectTransform>(); bcRt.anchorMin = new Vector2(0f, 0f); bcRt.anchorMax = new Vector2(1f, 1f); bcRt.offsetMin = new Vector2(0f, 0f); bcRt.offsetMax = new Vector2(-(applyRt.sizeDelta.x + 16f), 0f);
            var bc = bcHost.AddComponent<Navigation.BreadcrumbBar>(); var list = new System.Collections.Generic.List<(string id, string title)>(); list.Add(("HOME", "Home")); bc.SetSegments(list);
            var drag = _top.AddComponent<Navigation.DragWindowOnBar>(); drag.Window = _window;

            _left = new GameObject("Nav"); _left.transform.SetParent(_window, false);
            var leftChild = _left.AddComponent<Layout.DockPanelChild>(); leftChild.Side = Layout.DockSide.Left; leftChild.Size = Theme.ThemeMetrics.NavWidth;
            _left.AddComponent<RectTransform>(); var leftImg = _left.AddComponent<UnityEngine.UI.Image>(); leftImg.color = Theme.ThemeColors.NavBg;
            var navPane = _left.AddComponent<Navigation.NavPane>(); navPane.BuildFromProviders(Providers.ProviderRegistry.All);

            // subscribe to providers changed to refresh nav list
            Providers.ProviderRegistry.ProvidersChanged += () =>
            {
                var np = _left != null ? _left.GetComponent<Navigation.NavPane>() : null;
                if (np != null) np.BuildFromProviders(Providers.ProviderRegistry.All);
            };

            _content = new GameObject("Content"); _content.transform.SetParent(_window, false);
            var fillChild = _content.AddComponent<Layout.DockPanelChild>(); fillChild.Side = Layout.DockSide.Fill;
            var contentRT = _content.AddComponent<RectTransform>(); var contentBg = _content.AddComponent<UnityEngine.UI.Image>(); contentBg.color = Theme.ThemeColors.ContentBg;
            // create inset to apply padding for inner cards only
            var insetGo = new GameObject("ContentInset"); insetGo.transform.SetParent(_content.transform, false);
            _contentInset = insetGo.AddComponent<RectTransform>(); _contentInset.anchorMin = Vector2.zero; _contentInset.anchorMax = Vector2.one; _contentInset.pivot = new Vector2(0.5f, 0.5f);
            _contentInset.offsetMin = new Vector2(Theme.ThemeMetrics.ContentPaddingX, Theme.ThemeMetrics.ContentPaddingY);
            _contentInset.offsetMax = new Vector2(-Theme.ThemeMetrics.ContentPaddingX, -Theme.ThemeMetrics.ContentPaddingY);
            var presenter = insetGo.AddComponent<Navigation.ContentPresenter>();
            _window.gameObject.AddComponent<Navigation.NavController>();

            var dock = _window.GetComponent<Layout.DockPanelLayout>(); if (dock != null) dock.Layout();
            _window.gameObject.SetActive(false);
        }

        /// <summary>Shows or hides the settings window (toggle). On first show rebuilds Home page with current metrics.</summary>
        public static void Toggle()
        {
            if (!_inited) Init();
            var vis = !_window.gameObject.activeSelf; _window.gameObject.SetActive(vis);
            if (vis)
            {
                var nav = _window.GetComponent<Navigation.NavController>(); nav?.RebuildHome();
                // schedule first-show auto expand
                nav?.ScheduleAutoExpandFirstMarkdown(2f);
            }
        }

        /// <summary>Ensures initialized without changing current visibility. Does not force show.</summary>
        public static void ShowHome() { if (!_inited) Init(); }
    }
}
