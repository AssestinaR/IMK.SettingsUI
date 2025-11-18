using System;
namespace IMK.SettingsUI
{
    /// <summary>
    /// Stable public API surface entry point (v1). External mods should rely only on members documented here.
    /// If a future version introduces breaking changes, <see cref="ApiVersion"/> Major will increment.
    /// </summary>
    public static class PublicApi
    {
        /// <summary>Semantic version string of the current SettingsUI API (same as <see cref="App.SettingsShell.Version"/>).</summary>
        public const string ApiVersion = App.SettingsShell.Version;

        /// <summary>
        /// Returns true if the current API version is greater or equal to the requested minimal version.
        /// Accepts forms: "1.0.0", "1.0" or just "1". Ignores pre-release tags.
        /// </summary>
        public static bool IsVersionAtLeast(string minVersion)
        {
            if (string.IsNullOrWhiteSpace(minVersion)) return true;
            try
            {
                var cur = Parse(ApiVersion);
                var req = Parse(minVersion);
                return cur.major != req.major
                    ? cur.major > req.major
                    : cur.minor != req.minor
                        ? cur.minor >= req.minor
                        : cur.patch >= req.patch;
            }
            catch { return false; }
        }
        private static (int major, int minor, int patch) Parse(string v)
        {
            var core = v.Split('-')[0]; // drop prerelease if any
            var parts = core.Split('.');
            int[] nums = { 0, 0, 0 };
            for (int i = 0; i < parts.Length && i < 3; i++) int.TryParse(parts[i], out nums[i]);
            return (nums[0], nums[1], nums[2]);
        }

        /// <summary>Registers an external settings provider. Wrapper for <see cref="Providers.ProviderRegistry.Register"/>.</summary>
        public static void RegisterProvider(Providers.ISettingsProvider provider) => Providers.ProviderRegistry.Register(provider);
        /// <summary>Attempts to retrieve a provider by Id.</summary>
        public static bool TryGetProvider(string id, out Providers.ISettingsProvider provider) => Providers.ProviderRegistry.TryGet(id, out provider);
        /// <summary>Unregisters a provider by Id.</summary>
        public static bool UnregisterProvider(string id) => Providers.ProviderRegistry.Unregister(id);

        /// <summary>Ensures Settings UI initialized (safe to call multiple times).</summary>
        public static void EnsureInitialized() => App.SettingsShell.Init();
        /// <summary>Toggles visibility of Settings window.</summary>
        public static void ToggleWindow() => App.SettingsShell.Toggle();

        // Expose Table API namespace shortcuts for external mods (type aliases for discoverability)
        public static class Table
        {
            public static Type TableCardModel => typeof(IMK.SettingsUI.Table.TableCardModel);
            public static Type ITableSchema => typeof(IMK.SettingsUI.Table.ITableSchema);
            public static Type ITableDataSet => typeof(IMK.SettingsUI.Table.ITableDataSet);
            public static Type IRowAdapter => typeof(IMK.SettingsUI.Table.IRowAdapter);
            public static Type TableColumn => typeof(IMK.SettingsUI.Table.TableColumn);
        }
    }
}
