using IMK.SettingsUI.App; // for DirtyIndicator
using UnityEngine;

namespace IMK.SettingsUI.Cards
{
    public static partial class CardTemplates
    {
        // Bind now delegates to CardBindingRegistry handlers
        public static GameObject Bind(ICardModel model, GameObject existing)
        {
            CardBindingRegistry.EnsureInit();
            try
            {
                if (model == null) return existing;
                var handler = CardBindingRegistry.TryGetKindHandler(model.Kind);
                GameObject result = null;
                if (handler != null)
                {
                    result = handler(model, existing);
                }
                if (result == null)
                {
                    // Fallback diagnostic markdown card
                    var err = new MarkdownCardModel { Id = (model?.Id ?? "unknown") + ":bind_null", Title = "Bind Null", Markdown = $"### Bind Null\nKind: `{model.Kind}` handler returned null. Fallback markdown rendered." };
                    result = BindMarkdown(err, existing);
                    if (IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled) UnityEngine.Debug.Log("[SettingsUI.Bind] handler returned null for kind=" + model.Kind);
                }
                return result;
            }
            catch (System.Exception ex)
            {
                var err = new MarkdownCardModel { Id = model?.Id + ":bind_error", Title = "Card Error", Markdown = $"卡片绑定失败: `{ex.Message}`" };
                return BindMarkdown(err, existing);
            }
        }
        internal static GameObject BindSettingStrategy(ICardModel model, GameObject existing)
        {
            var t = model.GetType();
            var handler = CardBindingRegistry.TryGetSettingSubtypeHandler(t);
            if (handler != null) return handler(model, existing);
            if (model is SettingCardModel sc) return BindSetting(sc, existing);
            var err = new MarkdownCardModel { Id = model.Id + ":unsupported_setting", Title = "Unsupported Setting", Markdown = $"未注册的设置卡类型: `{t.FullName}`" };
            return BindMarkdown(err, existing);
        }
        private static void StyleButton(UnityEngine.UI.Button btn)
        {
            var colors = btn.colors; colors.highlightedColor = Theme.ThemeColors.NavItemHover; colors.pressedColor = Theme.ThemeColors.Accent; btn.colors = colors;
        }
        private static void EnsureTextFont(UnityEngine.UI.Text t)
        {
            if (t.font != null) return;
            var builtin = Theme.ThemeColors.DefaultFont;
            if (builtin != null) t.font = builtin;
            else
            {
                var any = GameObject.FindObjectOfType<UnityEngine.UI.Text>();
                if (any != null && any.font != null) t.font = any.font;
            }
        }
        private static GameObject EnsureBase(GameObject go, float h)
        {
            if (go == null) go = new GameObject("Card");
            var rt = go.GetComponent<RectTransform>(); if (rt == null) rt = go.AddComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, h);
            var img = go.GetComponent<UnityEngine.UI.Image>(); if (img == null) img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = Theme.ThemeColors.Card;
            var border = go.transform.Find("Border"); if (border == null) { border = new GameObject("Border").transform; border.SetParent(go.transform, false); var bimg = border.gameObject.AddComponent<UnityEngine.UI.Image>(); bimg.color = Theme.ThemeColors.CardBorder; var brt = bimg.GetComponent<RectTransform>(); brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = new Vector2(1, 1); brt.offsetMax = new Vector2(-1, -1); } else { var brt = border.GetComponent<RectTransform>(); brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = new Vector2(1, 1); brt.offsetMax = new Vector2(-1, -1); }
            return go;
        }
        private static UnityEngine.UI.Text FindOrMakeText(GameObject card, string name, int size, Color color, TextAnchor anchor)
        {
            var tr = card.transform.Find(name) as RectTransform; UnityEngine.UI.Text t;
            if (tr == null) { t = Theme.UIFactory.MakeText(card.transform, name, size, color, anchor); }
            else t = tr.GetComponent<UnityEngine.UI.Text>();
            if (t.font == null) t.font = Theme.ThemeColors.DefaultFont; return t;
        }
        private static void ResetCard(GameObject go, bool keepBorder)
        {
            // Remove all children except optional Border
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                var ch = go.transform.GetChild(i);
                if (keepBorder && ch.name == "Border") continue;
                GameObject.DestroyImmediate(ch.gameObject);
            }
            // Remove any root Button if present
            var btn = go.GetComponent<UnityEngine.UI.Button>(); if (btn != null) GameObject.DestroyImmediate(btn);
        }
        internal static GameObject BindNavigation(NavigationCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 18, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            var arrow = FindOrMakeText(go, "Arrow", 18, Theme.ThemeColors.TextDim, TextAnchor.MiddleCenter);
            title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; desc.text = m.Desc ?? ""; arrow.text = ">";
            PositionTitleDesc(go, h); PositionArrow(go, h);
            var btn = go.AddComponent<UnityEngine.UI.Button>(); btn.onClick.RemoveAllListeners(); if (m.OnClick != null) btn.onClick.AddListener(() => m.OnClick()); StyleButton(btn);
            return go;
        }
        internal static GameObject BindMarkdown(MarkdownCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 18, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            title.text = m.Title;
            // fallback content retrieval supporting legacy MarkdownText field/property
            string raw = m.Markdown;
            if (string.IsNullOrEmpty(raw))
            {
                var t = m.GetType();
                var fld = t.GetField("MarkdownText", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fld != null) raw = fld.GetValue(m) as string;
                if (string.IsNullOrEmpty(raw))
                {
                    var prop = t.GetProperty("MarkdownText", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prop != null) raw = prop.GetValue(m) as string;
                }
                if (string.IsNullOrEmpty(raw)) raw = "(empty markdown)";
                if (IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled) UnityEngine.Debug.Log("[CardTemplates.BindMarkdown] Fallback content used id=" + m.Id + " len=" + raw.Length);
            }
            desc.supportRichText = true; desc.text = MarkdownParser.ToRichText(raw ?? string.Empty);
            float padX = Theme.ThemeMetrics.CardPaddingX; float padY = Theme.ThemeMetrics.CardPaddingY; float titleH = 30f;
            var trT = title.GetComponent<RectTransform>(); trT.anchorMin = new Vector2(0f, 1f); trT.anchorMax = new Vector2(1f, 1f); trT.pivot = new Vector2(0f, 1f); trT.offsetMin = new Vector2(padX, -padY - titleH); trT.offsetMax = new Vector2(-padX, -padY); trT.sizeDelta = new Vector2(0f, titleH);
            var trD = desc.GetComponent<RectTransform>(); trD.anchorMin = new Vector2(0f, 0f); trD.anchorMax = new Vector2(1f, 1f); trD.pivot = new Vector2(0f, 0f); trD.offsetMin = new Vector2(padX, padY); trD.offsetMax = new Vector2(-padX, -(padY + titleH));
            // add expandable behaviour
            var exp = go.GetComponent<MarkdownExpandable>(); if (exp == null) exp = go.AddComponent<MarkdownExpandable>();
            exp.TitleText = title; exp.ContentText = desc; exp.InitialHeight = h; exp.PaddingTop = padY; exp.PaddingBottom = padY; exp.TitleHeight = titleH; exp.PaddingX = padX;
            // click anywhere on card to expand
            var btn = go.GetComponent<UnityEngine.UI.Button>(); if (btn == null) { btn = go.AddComponent<UnityEngine.UI.Button>(); StyleButton(btn); }
            btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => exp.TryExpand());
            return go;
        }
        internal static GameObject BindAction(ActionCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 18, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; desc.text = m.Desc ?? "";
            PositionTitleDesc(go, h);
            // action button inline on the right
            var host = go.transform.Find("ActionBtn") as RectTransform;
            if (host == null) { host = new GameObject("ActionBtn").AddComponent<RectTransform>(); host.SetParent(go.transform, false); host.anchorMin = new Vector2(1f, 0.25f); host.anchorMax = new Vector2(1f, 0.75f); host.pivot = new Vector2(1f, 0.5f); var img = host.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem; }
            // apply global right padding
            {
                float padX = Theme.ThemeMetrics.CardPaddingX; float w = 140f;
                host.offsetMax = new Vector2(-padX, 0f);
                host.offsetMin = new Vector2(-padX - w, 0f);
                host.sizeDelta = new Vector2(w, 0f);
            }
            var btn = host.GetComponent<UnityEngine.UI.Button>(); if (btn == null) btn = host.gameObject.AddComponent<UnityEngine.UI.Button>(); StyleButton(btn); btn.onClick.RemoveAllListeners(); if (m.OnInvoke != null) btn.onClick.AddListener(() => m.OnInvoke());
            var txt = host.transform.Find("Text") as RectTransform; UnityEngine.UI.Text tt; if (txt == null) { tt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); tt.transform.SetParent(host, false); } else tt = txt.GetComponent<UnityEngine.UI.Text>(); EnsureTextFont(tt); tt.color = Color.white; tt.alignment = TextAnchor.MiddleCenter; tt.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; var tr = tt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            return go;
        }
        internal static GameObject BindBoundSetting(BoundSettingCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 16, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; desc.text = m.Desc ?? "";
            PositionTitleDesc(go, h);
            var shadow = new SettingCardModel { Id = m.Id, Title = m.Title, Desc = m.Desc, Initial = m.Getter?.Invoke(), Pending = m.Getter?.Invoke(), Min = m.Min, Max = m.Max, Options = m.Options };
            if (m.OriginalValue == null) { try { m.OriginalValue = shadow.Initial; } catch { } }
            System.Func<bool> dirtyCheck = () => { try { var cur = m.Getter?.Invoke(); return m.OriginalValue == null ? cur != null : (cur == null ? m.OriginalValue != null : !cur.Equals(m.OriginalValue)); } catch { return false; } };
            AttachDirtyPulse(go, dirtyCheck);
            if (m.Options != null && m.Options.Length > 0)
            {
                // enum or option dropdown
                var host = go.transform.Find("Dropdown") as RectTransform; float padX = Theme.ThemeMetrics.CardPaddingX; float w = Theme.ThemeMetrics.InputWidthSmall;
                if (host == null) { host = new GameObject("Dropdown").AddComponent<RectTransform>(); host.SetParent(go.transform, false); host.anchorMin = new Vector2(1f, 0.2f); host.anchorMax = new Vector2(1f, 0.8f); host.pivot = new Vector2(1f, 0.5f); var img = host.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem; }
                host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - w, 0f); host.sizeDelta = new Vector2(w, 0f);
                var dp = host.GetComponent<UnityEngine.UI.Dropdown>(); if (dp == null) dp = host.gameObject.AddComponent<UnityEngine.UI.Dropdown>(); dp.options.Clear(); foreach (var o in m.Options) dp.options.Add(new UnityEngine.UI.Dropdown.OptionData(o)); int sel = 0; var cur = shadow.Initial?.ToString() ?? string.Empty; for (int i = 0; i < dp.options.Count; i++) if (dp.options[i].text == cur) { sel = i; break; }
                dp.value = sel; dp.onValueChanged.RemoveAllListeners(); dp.onValueChanged.AddListener(i => { if (i >= 0 && i < dp.options.Count) { var val = dp.options[i].text; m.Pending = val; m.Setter?.Invoke(val); } });
                return go;
            }
            // existing boolean / numeric handling follows
            if (m.ValueType == typeof(bool))
            {
                var host = go.transform.Find("MiniToggle") as RectTransform; if (host == null) { host = new GameObject("MiniToggle").AddComponent<RectTransform>(); host.SetParent(go.transform, false); }
                host.anchorMin = new Vector2(1f, 0.26f); host.anchorMax = new Vector2(1f, 0.74f); host.pivot = new Vector2(1f, 0.5f);
                float padX = Theme.ThemeMetrics.CardPaddingX; float visualW = Theme.ThemeMetrics.SliderFixedWidth * 0.2f; float hitExtra = 12f;
                host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - (visualW + hitExtra), 0f); host.sizeDelta = new Vector2(visualW + hitExtra, 0f);
                var inner = host.transform.Find("Inner") as RectTransform; if (inner == null) { inner = new GameObject("Inner").AddComponent<RectTransform>(); inner.SetParent(host, false); }
                inner.anchorMin = new Vector2(0f, 0f); inner.anchorMax = new Vector2(1f, 1f); inner.offsetMin = new Vector2(hitExtra * 0.5f, 0f); inner.offsetMax = new Vector2(-hitExtra * 0.5f, 0f);
                var track = inner.transform.Find("Track") as RectTransform; UnityEngine.UI.Image trackImg; if (track == null) { trackImg = new GameObject("Track").AddComponent<UnityEngine.UI.Image>(); trackImg.transform.SetParent(inner, false); track = trackImg.GetComponent<RectTransform>(); track.anchorMin = new Vector2(0f, 0f); track.anchorMax = new Vector2(1f, 1f); track.pivot = new Vector2(0.5f, 0.5f); track.offsetMin = Vector2.zero; track.offsetMax = Vector2.zero; } else { trackImg = track.GetComponent<UnityEngine.UI.Image>(); }
                trackImg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
                var knob = inner.transform.Find("Knob") as RectTransform; UnityEngine.UI.Image knobImg; if (knob == null) { knobImg = new GameObject("Knob").AddComponent<UnityEngine.UI.Image>(); knobImg.transform.SetParent(inner, false); knob = knobImg.GetComponent<RectTransform>(); } else knobImg = knob.GetComponent<UnityEngine.UI.Image>();
                knob.anchorMin = new Vector2(0f, 0f); knob.anchorMax = new Vector2(0f, 1f); knob.pivot = new Vector2(0f, 0.5f); knob.sizeDelta = new Vector2(24f, 0f); knobImg.color = Color.white;
                float trackWidth = inner.rect.width > 1 ? inner.rect.width : visualW;
                void ApplyMini(bool on) { float width = inner.sizeDelta.x > 1 ? inner.sizeDelta.x : trackWidth; float x = on ? width - knob.sizeDelta.x : 0f; knob.anchoredPosition = new Vector2(x, 0f); trackImg.color = on ? Theme.ThemeColors.Accent : new Color(0.25f, 0.25f, 0.25f, 0.95f); }
                bool cur = false; try { cur = System.Convert.ToBoolean(shadow.Initial); } catch { }
                m.Pending = cur; ApplyMini(cur);
                var btn2 = host.GetComponent<UnityEngine.UI.Button>(); if (btn2 == null) { btn2 = host.gameObject.AddComponent<UnityEngine.UI.Button>(); StyleButton(btn2); }
                btn2.onClick.RemoveAllListeners(); btn2.onClick.AddListener(() => { m.Pending = !System.Convert.ToBoolean(m.Pending); m.Setter?.Invoke(m.Pending); ApplyMini(System.Convert.ToBoolean(m.Pending)); });
            }
            else if (m.Min.HasValue || m.Max.HasValue)
            {
                MakeSliderWithValue(go, shadow);
                var slider = go.transform.Find("Slider")?.GetComponent<UnityEngine.UI.Slider>(); if (slider != null) { slider.onValueChanged.AddListener(v => m.Pending = v); }
                var input2 = go.transform.Find("SliderValue")?.GetComponent<UnityEngine.UI.InputField>(); if (input2 != null) { input2.onEndEdit.AddListener(_ => m.Pending = shadow.Pending); }
                m.Pending = shadow.Pending;
            }
            else
            {
                MakeValueInput(go, shadow);
                var input = go.transform.Find("ValueHost")?.GetComponent<UnityEngine.UI.InputField>(); if (input != null) { input.onEndEdit.AddListener(_ => { m.Pending = shadow.Pending; m.Setter?.Invoke(shadow.Pending); }); }
                m.Pending = shadow.Pending;
            }
            return go;
        }
        internal static GameObject BindListSetting(ListSettingCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 16, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; desc.text = m.Desc ?? "";
            PositionTitleDesc(go, h);
            var host = go.transform.Find("ValueHost") as RectTransform;
            if (host == null) { host = new GameObject("ValueHost").AddComponent<RectTransform>(); host.SetParent(go.transform, false); host.anchorMin = new Vector2(1f, 0.2f); host.anchorMax = new Vector2(1f, 0.8f); host.pivot = new Vector2(1f, 0.5f); var img = host.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0.85f, 0.85f, 0.85f, 0.95f); }
            float padX = Theme.ThemeMetrics.CardPaddingX; float w = Theme.ThemeMetrics.InputWidthLarge; host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - w, 0f); host.sizeDelta = new Vector2(w, 0f);
            var input = host.GetComponent<UnityEngine.UI.InputField>(); if (input == null) { input = host.gameObject.AddComponent<UnityEngine.UI.InputField>(); var txt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); txt.transform.SetParent(host, false); EnsureTextFont(txt); txt.color = Color.black; txt.alignment = TextAnchor.MiddleCenter; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(4, 0); tr.offsetMax = new Vector2(-4, 0); input.textComponent = txt; }
            string sep = string.IsNullOrEmpty(m.Separator) ? "," : m.Separator; input.text = m.InitialValues == null ? string.Empty : string.Join(sep, m.InitialValues);
            input.onEndEdit.RemoveAllListeners(); input.onEndEdit.AddListener(val => { var parts = (val ?? string.Empty).Split(new[] { sep }, System.StringSplitOptions.RemoveEmptyEntries); for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim(); m.PendingValues = parts; m.Setter?.Invoke(parts); });
            AttachDirtyPulse(go, () => { if (m.InitialValues == null && m.PendingValues == null) return false; string a = m.InitialValues == null ? "" : string.Join(sep, m.InitialValues); string b = m.PendingValues == null ? "" : string.Join(sep, m.PendingValues); return a != b; });
            return go;
        }
        internal static GameObject BindSetting(SettingCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 16, Color.white, TextAnchor.UpperLeft);
            var desc = FindOrMakeText(go, "Desc", 12, Theme.ThemeColors.TextDim, TextAnchor.UpperLeft);
            title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; desc.text = m.Desc ?? "";
            PositionTitleDesc(go, h);
            AttachDirtyPulse(go, () => m.Pending != null && m.Initial != null ? !m.Pending.Equals(m.Initial) : (m.Pending != m.Initial));
            MakeValueInput(go, m);
            return go;
        }
        internal static GameObject BindToggleSlider(ToggleSliderSettingCardModel m, GameObject existing)
        {
            var h = ResolveHeight(m);
            var go = EnsureBase(existing, h);
            ResetCard(go, keepBorder: true);
            var title = FindOrMakeText(go, "Title", 14, Color.white, TextAnchor.MiddleLeft); var tRT = title.GetComponent<RectTransform>(); title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; tRT.anchorMin = new Vector2(0f, 0f); tRT.anchorMax = new Vector2(0.6f, 1f); tRT.offsetMin = new Vector2(Theme.ThemeMetrics.CardPaddingX, 0f); tRT.offsetMax = new Vector2(-4f, 0f);
            var host = go.transform.Find("MiniSlider") as RectTransform; if (host == null) { host = new GameObject("MiniSlider").AddComponent<RectTransform>(); host.SetParent(go.transform, false); }
            host.anchorMin = new Vector2(1f, 0.26f); host.anchorMax = new Vector2(1f, 0.74f); host.pivot = new Vector2(1f, 0.5f); float padX = Theme.ThemeMetrics.CardPaddingX; float visualW = Theme.ThemeMetrics.SliderFixedWidth * 0.2f; float hitExtra = 12f; host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - (visualW + hitExtra), 0f); host.sizeDelta = new Vector2(visualW + hitExtra, 0f);
            var inner = host.transform.Find("Inner") as RectTransform; if (inner == null) { inner = new GameObject("Inner").AddComponent<RectTransform>(); inner.SetParent(host, false); }
            inner.anchorMin = new Vector2(0f, 0f); inner.anchorMax = new Vector2(1f, 1f); inner.offsetMin = new Vector2(hitExtra * 0.5f, 0f); inner.offsetMax = new Vector2(-hitExtra * 0.5f, 0f);
            var track = inner.transform.Find("Track") as RectTransform; UnityEngine.UI.Image trackImg; if (track == null) { trackImg = new GameObject("Track").AddComponent<UnityEngine.UI.Image>(); trackImg.transform.SetParent(inner, false); track = trackImg.GetComponent<RectTransform>(); track.anchorMin = new Vector2(0f, 0f); track.anchorMax = new Vector2(1f, 1f); track.pivot = new Vector2(0.5f, 0.5f); track.offsetMin = Vector2.zero; track.offsetMax = Vector2.zero; } else { trackImg = track.GetComponent<UnityEngine.UI.Image>(); }
            var knob = inner.transform.Find("Knob") as RectTransform; UnityEngine.UI.Image knobImg; if (knob == null) { knobImg = new GameObject("Knob").AddComponent<UnityEngine.UI.Image>(); knobImg.transform.SetParent(inner, false); knob = knobImg.GetComponent<RectTransform>(); } else knobImg = knob.GetComponent<UnityEngine.UI.Image>(); knob.anchorMin = new Vector2(0f, 0f); knob.anchorMax = new Vector2(0f, 1f); knob.pivot = new Vector2(0f, 0.5f); knob.sizeDelta = new Vector2(24f, 0f); knobImg.color = Color.white;
            void Apply(bool on) { float width = inner.rect.width > 1 ? inner.rect.width : visualW; float x = on ? width - knob.sizeDelta.x : 0f; knob.anchoredPosition = new Vector2(x, 0f); trackImg.color = on ? Theme.ThemeColors.Accent : new Color(0.25f, 0.25f, 0.25f, 0.95f); }
            m.Pending = m.Initial; Apply(m.Pending);
            var btn = host.GetComponent<UnityEngine.UI.Button>(); if (btn == null) { btn = host.gameObject.AddComponent<UnityEngine.UI.Button>(); StyleButton(btn); }
            btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => { m.Pending = !m.Pending; m.Setter?.Invoke(m.Pending); Apply(m.Pending); });
            return go;
        }
        internal static GameObject BindSchemaTable(IMK.SettingsUI.Table.TableCardModel m, GameObject existing)
        {
            var go = EnsureBase(existing, ResolveHeight(m));
            ResetCard(go, keepBorder: true);
            float padX = Theme.ThemeMetrics.CardPaddingX; float padY = Theme.ThemeMetrics.CardPaddingY; const float headerH = 26f; const float barH = 28f;
            var title = FindOrMakeText(go, "Title", 18, Color.white, TextAnchor.UpperLeft); title.text = string.IsNullOrEmpty(m.Title) ? m.Id : m.Title; var tRT = title.GetComponent<RectTransform>(); tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f); tRT.pivot = new Vector2(0f, 1f); tRT.offsetMin = new Vector2(padX, -padY - headerH); tRT.offsetMax = new Vector2(-padX, -padY); tRT.sizeDelta = new Vector2(0f, headerH);
            var bar = go.transform.Find("Bar") as RectTransform; if (bar == null) { bar = new GameObject("Bar").AddComponent<RectTransform>(); bar.SetParent(go.transform, false); }
            bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(0.5f, 0f); bar.sizeDelta = new Vector2(0f, barH); bar.anchoredPosition = new Vector2(0f, 0f);
            var barImg = bar.GetComponent<UnityEngine.UI.Image>(); if (barImg == null) barImg = bar.gameObject.AddComponent<UnityEngine.UI.Image>(); barImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            for (int i = bar.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(bar.GetChild(i).gameObject);
            var vp = go.transform.Find("Viewport") as RectTransform; if (vp == null) { vp = new GameObject("Viewport").AddComponent<RectTransform>(); vp.SetParent(go.transform, false); }
            vp.anchorMin = new Vector2(0f, 0f); vp.anchorMax = new Vector2(1f, 1f); vp.pivot = new Vector2(0.5f, 1f); vp.offsetMin = new Vector2(padX, barH + padY); vp.offsetMax = new Vector2(-padX, -(headerH + padY));
            var ctrl = go.GetComponent<IMK.SettingsUI.Table.SchemaTableController>(); if (ctrl == null) ctrl = go.AddComponent<IMK.SettingsUI.Table.SchemaTableController>();
            ctrl.Init(m.Schema, m.DataSet, vp);
            // buttons on bar
            float x = 4f;
            MakeBarButton(bar, "Add", x, () => ctrl.AddNew()); x += 72f;
            MakeBarButton(bar, "Remove", x, () => ctrl.RemoveSelected()); x += 72f;
            MakeBarButton(bar, "Up", x, () => ctrl.MoveSelectedUp()); x += 72f;
            MakeBarButton(bar, "Down", x, () => ctrl.MoveSelectedDown()); x += 72f;
            MakeBarButton(bar, "Save", x, () => ctrl.Save()); x += 72f;
            MakeBarButton(bar, "Reload", x, () => ctrl.Reload()); x += 16f;
            return go;
        }
        private static void MakeBarButton(RectTransform bar, string label, float x, System.Action act) { var btnRt = new GameObject(label).AddComponent<RectTransform>(); btnRt.SetParent(bar, false); btnRt.anchorMin = new Vector2(0f, 0f); btnRt.anchorMax = new Vector2(0f, 1f); btnRt.pivot = new Vector2(0f, 0.5f); btnRt.anchoredPosition = new Vector2(x, 0f); btnRt.sizeDelta = new Vector2(64f, 0f); var img = btnRt.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem; var btn = btnRt.gameObject.AddComponent<UnityEngine.UI.Button>(); StyleButton(btn); btn.onClick.AddListener(() => act()); var txt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); txt.transform.SetParent(btnRt, false); EnsureTextFont(txt); txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter; txt.text = label; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero; }
        private static void PositionTitleDesc(GameObject card, float h)
        {
            float padX = Theme.ThemeMetrics.CardPaddingX; float padY = Theme.ThemeMetrics.CardPaddingY; float labelWidth = Theme.ThemeMetrics.CardLabelWidth;
            var t = card.transform.Find("Title") as RectTransform; if (t != null) { t.anchorMin = new Vector2(0f, 0.55f); t.anchorMax = new Vector2(0f, 1f); t.pivot = new Vector2(0f, 1f); t.anchoredPosition = new Vector2(padX, -padY); t.sizeDelta = new Vector2(labelWidth - padX * 2f, 0f); }
            var d = card.transform.Find("Desc") as RectTransform; if (d != null) { d.anchorMin = new Vector2(0f, 0f); d.anchorMax = new Vector2(0f, 0.55f); d.pivot = new Vector2(0f, 0f); d.anchoredPosition = new Vector2(padX, padY - 6f); d.sizeDelta = new Vector2(labelWidth - padX * 2f, 0f); }
        }
        private static void PositionArrow(GameObject card, float h)
        {
            var a = card.transform.Find("Arrow") as RectTransform; if (a != null) { a.anchorMin = new Vector2(1f, 0f); a.anchorMax = new Vector2(1f, 1f); float aw = Mathf.Max(24f, h - 16f); a.sizeDelta = new Vector2(aw, 0f); a.offsetMin = new Vector2(-aw, 0f); a.offsetMax = Vector2.zero; }
        }
        private static float ResolveHeight(ICardModel m) => m.HeightOverride switch
        {
            > 0 => m.HeightOverride,
            < 0 => Theme.ThemeMetrics.CardHeightMarkdown,
            _ => m.Size switch
            {
                CardSize.Small => Theme.ThemeMetrics.CardHeightSmall,
                CardSize.Large => Theme.ThemeMetrics.CardHeightLarge,
                CardSize.XLarge => Theme.ThemeMetrics.CardHeightXLarge,
                _ => Theme.ThemeMetrics.CardHeightMedium
            }
        };
        private static void AttachDirtyPulse(GameObject card, System.Func<bool> isDirty)
        {
            var border = card.transform.Find("Border")?.GetComponent<UnityEngine.UI.Image>(); if (border == null) return;
            var ind = card.GetComponent<DirtyIndicator>(); if (ind == null) ind = card.AddComponent<DirtyIndicator>(); ind.Init(border, isDirty);
        }
        private static void MakeValueInput(GameObject card, SettingCardModel m)
        {
            if (m.Options != null && m.Options.Length > 0) { MakeDropdown(card, m); return; }
            if (m.Min.HasValue || m.Max.HasValue) { MakeSliderWithValue(card, m); return; }
            var host = card.transform.Find("ValueHost") as RectTransform;
            float padX = Theme.ThemeMetrics.CardPaddingX; float w = Theme.ThemeMetrics.InputWidthSmall;
            if (host == null) { host = new GameObject("ValueHost").AddComponent<RectTransform>(); host.SetParent(card.transform, false); host.anchorMin = new Vector2(1f, 0.2f); host.anchorMax = new Vector2(1f, 0.8f); host.pivot = new Vector2(1f, 0.5f); var img = host.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0.85f, 0.85f, 0.85f, 0.95f); }
            host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - w, 0f); host.sizeDelta = new Vector2(w, 0f);
            var input = host.GetComponent<UnityEngine.UI.InputField>(); if (input == null) { input = host.gameObject.AddComponent<UnityEngine.UI.InputField>(); var txt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); txt.transform.SetParent(host, false); EnsureTextFont(txt); txt.color = Color.black; txt.alignment = TextAnchor.MiddleCenter; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(4, 0); tr.offsetMax = new Vector2(-4, 0); input.textComponent = txt; }
            bool isNumeric = false; if (m.Initial != null)
            { var t = m.Initial.GetType(); isNumeric = t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(float) || t == typeof(double) || t == typeof(decimal); }
            input.contentType = isNumeric ? UnityEngine.UI.InputField.ContentType.DecimalNumber : UnityEngine.UI.InputField.ContentType.Standard;
            input.onEndEdit.RemoveAllListeners(); input.text = m.Initial?.ToString() ?? string.Empty; input.onEndEdit.AddListener(val => { float f; if (isNumeric && float.TryParse(val, out f)) { m.Pending = f; } else m.Pending = val; });
            var auto = host.GetComponent<IMK.SettingsUI.Layout.AutoWidthInput>(); if (auto == null) auto = host.gameObject.AddComponent<IMK.SettingsUI.Layout.AutoWidthInput>(); auto.Target = input; auto.MinWidth = Theme.ThemeMetrics.InputWidthSmall; auto.MaxWidth = Theme.ThemeMetrics.InputWidthLarge; auto.ExtraPadding = 24f;
        }
        private static void MakeDropdown(GameObject card, SettingCardModel m)
        {
            var host = card.transform.Find("Dropdown") as RectTransform; float padX = Theme.ThemeMetrics.CardPaddingX; float w = Theme.ThemeMetrics.InputWidthSmall;
            if (host == null) { host = new GameObject("Dropdown").AddComponent<RectTransform>(); host.SetParent(card.transform, false); host.anchorMin = new Vector2(1f, 0.2f); host.anchorMax = new Vector2(1f, 0.8f); host.pivot = new Vector2(1f, 0.5f); var img = host.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem; }
            host.offsetMax = new Vector2(-padX, 0f); host.offsetMin = new Vector2(-padX - w, 0f); host.sizeDelta = new Vector2(w, 0f);
            var dp = host.GetComponent<UnityEngine.UI.Dropdown>(); if (dp == null) dp = host.gameObject.AddComponent<UnityEngine.UI.Dropdown>(); dp.options.Clear(); foreach (var o in m.Options) dp.options.Add(new UnityEngine.UI.Dropdown.OptionData(o)); int sel = 0; var cur = m.Initial?.ToString() ?? string.Empty; for (int i = 0; i < dp.options.Count; i++) if (dp.options[i].text == cur) { sel = i; break; }
            dp.value = sel; dp.onValueChanged.RemoveAllListeners(); dp.onValueChanged.AddListener(i => { if (i >= 0 && i < dp.options.Count) m.Pending = dp.options[i].text; });
        }
        private static void MakeSliderWithValue(GameObject card, SettingCardModel m)
        {
            float min = m.Min ?? 0f; float max = m.Max ?? 1f; float cur = 0f; float.TryParse((m.Initial ?? 0f).ToString(), out cur);
            var valHost = card.transform.Find("SliderValue") as RectTransform; float padX = Theme.ThemeMetrics.CardPaddingX; float w = Theme.ThemeMetrics.InputWidthSmall;
            if (valHost == null) { valHost = new GameObject("SliderValue").AddComponent<RectTransform>(); valHost.SetParent(card.transform, false); var img = valHost.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0.85f, 0.85f, 0.85f, 0.95f); }
            valHost.anchorMin = new Vector2(1f, 0.2f); valHost.anchorMax = new Vector2(1f, 0.8f); valHost.pivot = new Vector2(1f, 0.5f); valHost.offsetMax = new Vector2(-padX, 0f); valHost.offsetMin = new Vector2(-padX - w, 0f); valHost.sizeDelta = new Vector2(w, 0f);
            var input = valHost.GetComponent<UnityEngine.UI.InputField>();
            if (input == null)
            {
                input = valHost.gameObject.AddComponent<UnityEngine.UI.InputField>();
                var txt = new GameObject("Text").AddComponent<UnityEngine.UI.Text>(); txt.transform.SetParent(valHost, false); EnsureTextFont(txt); txt.color = Color.black; txt.alignment = TextAnchor.MiddleCenter; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(4, 0); tr.offsetMax = new Vector2(-4, 0); input.textComponent = txt; input.contentType = UnityEngine.UI.InputField.ContentType.DecimalNumber;
            }
            input.onEndEdit.RemoveAllListeners(); input.text = Mathf.Clamp(cur, min, max).ToString("0.###");
            var sliderGo = card.transform.Find("Slider") as RectTransform; if (sliderGo == null) { sliderGo = new GameObject("Slider").AddComponent<RectTransform>(); sliderGo.SetParent(card.transform, false); }
            float right = Theme.ThemeMetrics.CardPaddingX + w + Theme.ThemeMetrics.SliderGapToInput; float fixedW = Theme.ThemeMetrics.SliderFixedWidth;
            sliderGo.anchorMin = new Vector2(1f, 0.5f); sliderGo.anchorMax = new Vector2(1f, 0.5f); sliderGo.pivot = new Vector2(1f, 0.5f); sliderGo.anchoredPosition = new Vector2(-right, 0f); sliderGo.sizeDelta = new Vector2(fixedW, Theme.ThemeMetrics.SliderTrackHeight);
            var bgTr = sliderGo.transform.Find("BG") as RectTransform; UnityEngine.UI.Image bg; if (bgTr == null) { bg = new GameObject("BG").AddComponent<UnityEngine.UI.Image>(); bg.transform.SetParent(sliderGo, false); bgTr = bg.GetComponent<RectTransform>(); } else bg = bgTr.GetComponent<UnityEngine.UI.Image>();
            bgTr.anchorMin = new Vector2(0f, 0.5f); bgTr.anchorMax = new Vector2(1f, 0.5f); bgTr.pivot = new Vector2(0.5f, 0.5f); bgTr.anchoredPosition = Vector2.zero; bgTr.sizeDelta = new Vector2(0f, Theme.ThemeMetrics.SliderTrackHeight); bg.color = new Color(1, 1, 1, 0.15f);
            var slider = sliderGo.GetComponent<UnityEngine.UI.Slider>(); if (slider == null) { slider = sliderGo.gameObject.AddComponent<UnityEngine.UI.Slider>(); var fill = new GameObject("Fill").AddComponent<UnityEngine.UI.Image>(); fill.transform.SetParent(sliderGo, false); var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = new Vector2(0f, 0.5f); fr.anchorMax = new Vector2(0f, 0.5f); fr.pivot = new Vector2(0f, 0.5f); fr.sizeDelta = new Vector2(0f, Theme.ThemeMetrics.SliderTrackHeight); fill.color = Theme.ThemeColors.Accent; slider.fillRect = fr; var handle = new GameObject("Handle").AddComponent<UnityEngine.UI.Image>(); handle.transform.SetParent(sliderGo, false); var hr = handle.GetComponent<RectTransform>(); hr.anchorMin = new Vector2(0f, 0.5f); hr.anchorMax = new Vector2(0f, 0.5f); hr.pivot = new Vector2(0.5f, 0.5f); handle.color = Color.white; slider.handleRect = hr; slider.targetGraphic = handle; }
            if (slider.fillRect != null) { var fr = slider.fillRect; fr.sizeDelta = new Vector2(fr.sizeDelta.x, Theme.ThemeMetrics.SliderTrackHeight); }
            if (slider.handleRect != null) { var hr = slider.handleRect; hr.sizeDelta = new Vector2(Theme.ThemeMetrics.SliderHandleWidth, Theme.ThemeMetrics.SliderHandleHeight); }
            slider.minValue = min; slider.maxValue = max; slider.onValueChanged.RemoveAllListeners(); slider.value = Mathf.Clamp(cur, min, max);
            slider.onValueChanged.AddListener(v => { var clipped = Mathf.Clamp(v, min, max); if (!Mathf.Approximately(slider.value, clipped)) slider.value = clipped; if (input != null) { var s = clipped.ToString("0.###"); if (input.text != s) input.text = s; } m.Pending = clipped; });
            input.onEndEdit.AddListener(val => { float f; if (float.TryParse(val, out f)) { f = Mathf.Clamp(f, min, max); if (!Mathf.Approximately(slider.value, f)) slider.value = f; m.Pending = f; } });
        }
    }
}
