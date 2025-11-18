namespace IMK.SettingsUI.Theme
{
    public static class ThemeMetrics
    {
        // Window & Nav
        public static float WindowWidth = 980f;
        public static float WindowHeight = 640f;
        public static float NavWidth = 220f;
        public static float NavItemHeight = 32f;
        public static float NavItemSpacing = 8f; // default 8

        // Content padding (defaults 8)
        public static float ContentPaddingX = 8f;
        public static float ContentPaddingY = 8f;

        // Card heights
        public static float CardHeightSmall = 56f;
        public static float CardHeightMedium = 72f;
        public static float CardHeightLarge = 120f;
        public static float CardHeightXLarge = 480f;
        public static float CardHeightMarkdown = 240f;
        // Card padding and label column width
        public static float CardPaddingX = 8f; // default 8
        public static float CardPaddingY = 8f; // default 8
        public static float CardLabelWidth = 360f;
        public static float CardSpacing = 8f; // spacing between cards
        // Text sizes
        public static int CardTitleFontSize = 18;
        public static int CardDescFontSize = 12;
        // Input widths
        public static float InputWidthSmall = 120f;
        public static float InputWidthMedium = 180f;
        public static float InputWidthLarge = 260f;
        // Slider
        public static float SliderHandleWidth = 10f;
        public static float SliderHandleHeight = 18f; // restored
        public static float SliderGapToInput = 14f;
        public static float SliderShortenFactor = 0.33f; // legacy factor, not used when fixed width is available
        public static float SliderTrackHeight = 4f; // track/ fill height
        public static float SliderFixedWidth = 320f; // new: keep slider width stable, right-aligned to input
        // Toggle
        public static float ToggleWidth = 60f;
        // Scroll
        public static float ScrollSensitivity = 1f; // slower
    }
}
