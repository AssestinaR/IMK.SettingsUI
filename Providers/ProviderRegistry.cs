using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMK.SettingsUI.Providers
{
    /// <summary>
    /// Global registry for external settings providers that wish to contribute pages to the Settings UI.
    /// Public Stable API: Register, TryGet, All, Unregister (v1).
    /// </summary>
    public static class ProviderRegistry
    {
        private static readonly Dictionary<string, ISettingsProvider> _providers = new Dictionary<string, ISettingsProvider>(StringComparer.OrdinalIgnoreCase);
        public static event Action ProvidersChanged;
        /// <summary>Raised after a provider is successfully registered.</summary>
        public static event Action<ISettingsProvider> OnRegister;
        /// <summary>Raised after a provider is successfully unregistered (provides the previous instance).</summary>
        public static event Action<string,ISettingsProvider> OnUnregister;

        /// <summary>
        /// Registers a provider. If Id already exists the existing provider is replaced and a warning is logged.
        /// Id must be non-empty and unique (case-insensitive).
        /// </summary>
        public static void Register(ISettingsProvider provider) => Register(provider, replaceExisting:true);
        /// <summary>
        /// Registers a provider with optional replace policy. When replaceExisting=false and conflict occurs, the call is ignored.
        /// </summary>
        public static bool Register(ISettingsProvider provider, bool replaceExisting)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.Id)) { Debug.LogWarning("[SettingsUI.ProviderRegistry] Reject empty provider or Id"); return false; }
            if (provider.Id.IndexOf(':') >= 0) { Debug.LogWarning("[SettingsUI.ProviderRegistry] Provider Id must not contain ':' : " + provider.Id); return false; }
            bool exists = _providers.ContainsKey(provider.Id);
            if (exists && !replaceExisting) { Debug.LogWarning("[SettingsUI.ProviderRegistry] Duplicate Id, ignoring register: " + provider.Id); return false; }
            ISettingsProvider old = null; if (exists) old = _providers[provider.Id];
            if (exists) Debug.LogWarning("[SettingsUI.ProviderRegistry] Provider Id collision, replacing: " + provider.Id);
            _providers[provider.Id] = provider;
            try { ProvidersChanged?.Invoke(); } catch (Exception ex) { Debug.LogWarning("[SettingsUI.ProviderRegistry] ProvidersChanged handler error: "+ex.Message); }
            try { OnRegister?.Invoke(provider); } catch (Exception ex) { Debug.LogWarning("[SettingsUI.ProviderRegistry] OnRegister handler error: "+ex.Message); }
            return true;
        }
        public static bool TryGet(string id, out ISettingsProvider provider) => _providers.TryGetValue(id, out provider);
        public static IReadOnlyDictionary<string, ISettingsProvider> All => _providers;
        public static bool Unregister(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (_providers.TryGetValue(id, out var old))
            {
                bool removed = _providers.Remove(id);
                if (removed)
                {
                    try { ProvidersChanged?.Invoke(); } catch (Exception ex) { Debug.LogWarning("[SettingsUI.ProviderRegistry] ProvidersChanged handler error: "+ex.Message); }
                    try { OnUnregister?.Invoke(id, old); } catch (Exception ex) { Debug.LogWarning("[SettingsUI.ProviderRegistry] OnUnregister handler error: "+ex.Message); }
                    return true;
                }
            }
            return false;
        }
    }
}
