using UnityEngine;

namespace IMK.SettingsUI.Theme
{
    public static class ThemeColors
    {
        public static readonly Color WindowBg = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color ContentBg = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        public static readonly Color TopBarBg = new Color(0f, 0f, 0f, 0.25f);
        public static readonly Color NavBg = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        public static readonly Color NavItem = new Color(0.20f, 0.20f, 0.20f, 0.85f);
        public static readonly Color NavItemHover = new Color(0.35f, 0.35f, 0.35f, 0.90f);
        public static readonly Color Card = new Color(0.15f, 0.15f, 0.15f, 0.92f);
        public static readonly Color CardBorder = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color TextDim = new Color(1f, 1f, 1f, 0.65f);
        public static readonly Color Accent = new Color(0.10f, 0.55f, 0.95f, 1f);

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("Arial.ttf");
        public static readonly Color BreadcrumbSeg = new Color(0.25f, 0.25f, 0.25f, 0.70f);
        public static readonly Color BreadcrumbSegHover = new Color(0.38f, 0.38f, 0.38f, 0.85f);
    }
}
