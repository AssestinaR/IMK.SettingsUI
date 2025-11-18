namespace IMK.SettingsUI.Navigation
{
    internal static class UITransitions
    {
        private static int _depth;
        public static bool Animating => _depth > 0;
        public static void Begin() { if (_depth < 1000) _depth++; }
        public static void End() { if (_depth > 0) _depth--; }
    }
}
