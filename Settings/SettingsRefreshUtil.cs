using UnityEngine;
using IMK.SettingsUI.Theme;
using IMK.SettingsUI.Navigation;

namespace IMK.SettingsUI.Settings
{
    public static class SettingsRefreshUtil
    {
        public static void RefreshLayout()
        {
            var window = GameObject.Find("IMK.SettingsUI.Canvas/Window")?.GetComponent<RectTransform>();
            if (window!=null) window.sizeDelta = new Vector2(ThemeMetrics.WindowWidth, ThemeMetrics.WindowHeight);
            // refresh nav pane and rebuild its buttons to reflect metrics
            var navPane = GameObject.Find("IMK.SettingsUI.Canvas/Window/Nav")?.GetComponent<NavPane>();
            if (navPane!=null) navPane.BuildFromProviders(Providers.ProviderRegistry.All);

            // rebind current content to apply metrics (sizes, paddings) without replacing page models
            var presenterGo = GameObject.Find("IMK.SettingsUI.Canvas/Window/Content/ContentInset");
            var presenter = presenterGo != null ? presenterGo.GetComponent<ContentPresenter>() : null;
            presenter?.ForceRebind();

            var inset = presenterGo != null ? presenterGo.GetComponent<RectTransform>() : null;
            if (inset!=null)
            {
                inset.offsetMin = new Vector2(Theme.ThemeMetrics.ContentPaddingX, Theme.ThemeMetrics.ContentPaddingY);
                inset.offsetMax = new Vector2(-Theme.ThemeMetrics.ContentPaddingX, -Theme.ThemeMetrics.ContentPaddingY);
            }
        }
    }
}
