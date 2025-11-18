using System.Collections.Generic;
using IMK.SettingsUI.Theme;
using IMK.SettingsUI.Providers;

namespace IMK.SettingsUI.Cards
{
    /// <summary>
    /// Deprecated: use InternalMods.CoreShell.CoreShellProvider instead.
    /// Retained for backward compatibility with external code calling PageFactory.BuildHome().
    /// </summary>
    public static class PageFactory
    {
        public static List<ICardModel> BuildHome()
        {
            // Delegate to CoreShellProvider logic.
            var core = new IMK.SettingsUI.InternalMods.CoreShell.CoreShellProvider();
            var models = core.BuildPageModels("Root");
            return models != null ? new List<ICardModel>(models) : new List<ICardModel>();
        }
    }
}
