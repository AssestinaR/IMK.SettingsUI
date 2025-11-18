using IMK.SettingsUI.App;
using IMK.SettingsUI.InternalMods.ItemModKitPanel;
using IMK.SettingsUI.InternalMods.SettingsPanel;
using IMK.SettingsUI.Providers;
using UnityEngine;
using SampleInternal = IMK.SettingsUI.InternalMods.Sample.SampleProvider;

namespace IMK.SettingsUI
{
    // Optional ModBehaviour so IMK.SettingsUI can be loaded as its own mod.
    public sealed class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public bool EnableSampleProvider = true; // future: can be loaded from config
        public bool EnableSettingsPanelProvider = true;
        public bool EnableItemModKitPanelProvider = true;
        void Awake()
        {
            if (EnableSampleProvider) ProviderRegistry.Register(new SampleInternal());
            if (EnableSettingsPanelProvider) ProviderRegistry.Register(new SettingsPanelProvider());
            if (EnableItemModKitPanelProvider) ProviderRegistry.Register(new ItemModKitPanelProvider());
            SettingsShell.Init();
            Debug.Log("[IMK.SettingsUI] ModBehaviour initialized.");
        }
        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6)) SettingsShell.Toggle();
        }
    }
}
