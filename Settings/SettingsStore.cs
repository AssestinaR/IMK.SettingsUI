using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace IMK.SettingsUI.Settings
{
    public sealed class SettingsStore
    {
        private const string Folder = "IMK.SettingsUI";
        private const string FileName = "ui.json";
        public static UiConfig Current = new UiConfig();

        public static string GetDir(){ return Path.Combine(Application.persistentDataPath, "Mods", Folder); }
        public static string GetFile(){ return Path.Combine(GetDir(), FileName); }

        public static void Load()
        {
            try
            {
                var path = GetFile();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonConvert.DeserializeObject<UiConfig>(json);
                    if (cfg != null) Current = cfg;
                }
                Apply(Current);
            }
            catch (Exception ex){ Debug.LogWarning($"[IMK.SettingsUI] Load settings failed: {ex.Message}"); }
        }
        public static void Save()
        {
            try
            {
                var dir = GetDir(); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(GetFile(), json);
            }
            catch (Exception ex){ Debug.LogWarning($"[IMK.SettingsUI] Save settings failed: {ex.Message}"); }
        }
        public static void Apply(UiConfig cfg)
        {
            try
            {
                Theme.ThemeMetrics.WindowWidth = cfg.window.width;
                Theme.ThemeMetrics.WindowHeight = cfg.window.height;
                Theme.ThemeMetrics.NavWidth = cfg.nav.width;
                Theme.ThemeMetrics.NavItemHeight = cfg.nav.itemHeight;
                Theme.ThemeMetrics.NavItemSpacing = cfg.nav.itemSpacing;
                Theme.ThemeMetrics.ContentPaddingX = cfg.content.padX;
                Theme.ThemeMetrics.ContentPaddingY = cfg.content.padY;
                Theme.ThemeMetrics.CardPaddingX = cfg.cards.padX;
                Theme.ThemeMetrics.CardPaddingY = cfg.cards.padY;
                Theme.ThemeMetrics.CardSpacing = cfg.cards.spacing;
                Theme.ThemeMetrics.CardHeightSmall = cfg.cards.smallH;
                Theme.ThemeMetrics.CardHeightMedium = cfg.cards.mediumH;
                Theme.ThemeMetrics.CardHeightLarge = cfg.cards.largeH;
                Theme.ThemeMetrics.CardHeightMarkdown = cfg.cards.markdownH;
                Theme.ThemeMetrics.CardTitleFontSize = cfg.cards.titleFont;
                Theme.ThemeMetrics.CardDescFontSize = cfg.cards.descFont;
                Theme.ThemeMetrics.InputWidthSmall = cfg.cards.inputSmall;
                Theme.ThemeMetrics.InputWidthMedium = cfg.cards.inputMedium;
                Theme.ThemeMetrics.InputWidthLarge = cfg.cards.inputLarge;
                Theme.ThemeMetrics.SliderGapToInput = cfg.cards.sliderGap;
                Theme.ThemeMetrics.SliderHandleWidth = cfg.cards.sliderHandleW;
                Theme.ThemeMetrics.SliderHandleHeight = cfg.cards.sliderHandleH;
                Theme.ThemeMetrics.ScrollSensitivity = cfg.scroll.sensitivity;
                Theme.ThemeMetrics.SliderFixedWidth = cfg.cards.sliderFixedW;
            }
            catch { }
        }
    }

    [Serializable]
    public sealed class UiConfig
    {
        public WindowCfg window = new WindowCfg();
        public NavCfg nav = new NavCfg();
        public ContentCfg content = new ContentCfg();
        public CardsCfg cards = new CardsCfg();
        public ScrollCfg scroll = new ScrollCfg();
    }
    [Serializable] public sealed class WindowCfg { public float width = 980f; public float height = 640f; }
    [Serializable] public sealed class NavCfg { public float width = 220f; public float itemHeight = 32f; public float itemSpacing = 8f; }
    [Serializable] public sealed class ContentCfg { public float padX = 8f; public float padY = 8f; }
    [Serializable] public sealed class CardsCfg { public float padX = 8f; public float padY = 8f; public float spacing = 8f; public float smallH = 56f; public float mediumH = 72f; public float largeH = 120f; public float markdownH = 240f; public int titleFont = 18; public int descFont = 12; public float inputSmall = 120f; public float inputMedium = 180f; public float inputLarge = 260f; public float sliderGap = 14f; public float sliderHandleW = 10f; public float sliderHandleH = 18f; public float sliderFixedW = 320f; }
    [Serializable] public sealed class ScrollCfg { public float sensitivity = 1f; }
}
