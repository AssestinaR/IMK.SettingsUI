using System;
using System.Collections.Generic;
using IMK.SettingsUI.Theme;

namespace IMK.SettingsUI.Settings
{
    /// <summary>
    /// Registry that maps SettingCardModel.Id to an apply action. Applies should mutate ThemeMetrics/SettingsStore.Current as needed.
    /// External mods may register their own ids to participate in SettingsApplyService without modifying the core.
    /// </summary>
    public static class SettingsApplyRegistry
    {
        private static readonly Dictionary<string, Action<object>> _map = new Dictionary<string, Action<object>>(StringComparer.OrdinalIgnoreCase);
        static SettingsApplyRegistry()
        {
            // Pre-register built-in mappings (equivalent to previous switch cases)
            Register("ui.window.width",  v=> ThemeMetrics.WindowWidth = ToF(v, ThemeMetrics.WindowWidth));
            Register("ui.window.height", v=> ThemeMetrics.WindowHeight = ToF(v, ThemeMetrics.WindowHeight));
            Register("ui.nav.width",     v=> ThemeMetrics.NavWidth = ToF(v, ThemeMetrics.NavWidth));
            Register("ui.nav.itemHeight",v=> { ThemeMetrics.NavItemHeight = ToF(v, ThemeMetrics.NavItemHeight); SettingsStore.Current.nav.itemHeight = ThemeMetrics.NavItemHeight; });
            Register("ui.nav.itemSpacing",v=> { ThemeMetrics.NavItemSpacing = ToF(v, ThemeMetrics.NavItemSpacing); SettingsStore.Current.nav.itemSpacing = ThemeMetrics.NavItemSpacing; });
            Register("ui.content.padX",  v=> { ThemeMetrics.ContentPaddingX = ToF(v, ThemeMetrics.ContentPaddingX); SettingsStore.Current.content.padX = ThemeMetrics.ContentPaddingX; });
            Register("ui.content.padY",  v=> { ThemeMetrics.ContentPaddingY = ToF(v, ThemeMetrics.ContentPaddingY); SettingsStore.Current.content.padY = ThemeMetrics.ContentPaddingY; });
            Register("ui.card.spacing",  v=> { ThemeMetrics.CardSpacing = ToF(v, ThemeMetrics.CardSpacing); SettingsStore.Current.cards.spacing = ThemeMetrics.CardSpacing; });
            Register("ui.card.padX",     v=> { ThemeMetrics.CardPaddingX = ToF(v, ThemeMetrics.CardPaddingX); SettingsStore.Current.cards.padX = ThemeMetrics.CardPaddingX; });
            Register("ui.card.padY",     v=> { ThemeMetrics.CardPaddingY = ToF(v, ThemeMetrics.CardPaddingY); SettingsStore.Current.cards.padY = ThemeMetrics.CardPaddingY; });
            Register("ui.scroll.sensitivity", v=> { ThemeMetrics.ScrollSensitivity = ToF(v, ThemeMetrics.ScrollSensitivity); SettingsStore.Current.scroll.sensitivity = ThemeMetrics.ScrollSensitivity; });
            Register("ui.card.h.small",  v=> { ThemeMetrics.CardHeightSmall = ToF(v, ThemeMetrics.CardHeightSmall); SettingsStore.Current.cards.smallH = ThemeMetrics.CardHeightSmall; });
            Register("ui.card.h.medium", v=> { ThemeMetrics.CardHeightMedium = ToF(v, ThemeMetrics.CardHeightMedium); SettingsStore.Current.cards.mediumH = ThemeMetrics.CardHeightMedium; });
            Register("ui.card.h.large",  v=> { ThemeMetrics.CardHeightLarge = ToF(v, ThemeMetrics.CardHeightLarge); SettingsStore.Current.cards.largeH = ThemeMetrics.CardHeightLarge; });
            Register("ui.card.h.markdown", v=> { ThemeMetrics.CardHeightMarkdown = ToF(v, ThemeMetrics.CardHeightMarkdown); SettingsStore.Current.cards.markdownH = ThemeMetrics.CardHeightMarkdown; });
            Register("ui.card.font.title", v=> { ThemeMetrics.CardTitleFontSize = (int)ToF(v, ThemeMetrics.CardTitleFontSize); SettingsStore.Current.cards.titleFont = ThemeMetrics.CardTitleFontSize; });
            Register("ui.card.font.desc",  v=> { ThemeMetrics.CardDescFontSize = (int)ToF(v, ThemeMetrics.CardDescFontSize); SettingsStore.Current.cards.descFont = ThemeMetrics.CardDescFontSize; });
            Register("ui.input.w.sm", v=> { ThemeMetrics.InputWidthSmall = ToF(v, ThemeMetrics.InputWidthSmall); SettingsStore.Current.cards.inputSmall = ThemeMetrics.InputWidthSmall; });
            Register("ui.input.w.md", v=> { ThemeMetrics.InputWidthMedium = ToF(v, ThemeMetrics.InputWidthMedium); SettingsStore.Current.cards.inputMedium = ThemeMetrics.InputWidthMedium; });
            Register("ui.input.w.lg", v=> { ThemeMetrics.InputWidthLarge = ToF(v, ThemeMetrics.InputWidthLarge); SettingsStore.Current.cards.inputLarge = ThemeMetrics.InputWidthLarge; });
            Register("ui.slider.gap",   v=> { ThemeMetrics.SliderGapToInput = ToF(v, ThemeMetrics.SliderGapToInput); SettingsStore.Current.cards.sliderGap = ThemeMetrics.SliderGapToInput; });
            Register("ui.slider.handle.w", v=> { ThemeMetrics.SliderHandleWidth = ToF(v, ThemeMetrics.SliderHandleWidth); SettingsStore.Current.cards.sliderHandleW = ThemeMetrics.SliderHandleWidth; });
            Register("ui.slider.handle.h", v=> { ThemeMetrics.SliderHandleHeight = ToF(v, ThemeMetrics.SliderHandleHeight); SettingsStore.Current.cards.sliderHandleH = ThemeMetrics.SliderHandleHeight; });
            Register("ui.slider.fixedW", v=> { ThemeMetrics.SliderFixedWidth = ToF(v, ThemeMetrics.SliderFixedWidth); SettingsStore.Current.cards.sliderFixedW = ThemeMetrics.SliderFixedWidth; });
        }
        public static void Register(string id, Action<object> apply)
        {
            if (string.IsNullOrWhiteSpace(id) || apply == null) return; _map[id] = apply;
        }
        /// <summary>Try to apply a value for a given id. Returns true if a mapping existed and was invoked.</summary>
        public static bool TryApply(string id, object value)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (_map.TryGetValue(id, out var act)) { try { act(value); return true; } catch { return false; } }
            return false;
        }
        private static float ToF(object o, float fallback){ try { return System.Convert.ToSingle(o); } catch { return fallback; } }
    }
}
