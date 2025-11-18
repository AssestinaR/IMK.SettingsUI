using System.Collections.Generic;
using UnityEngine;
using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.Providers
{
    /// <summary>Represents a navigation entry contributed by a provider (a sub-page within the provider root).</summary>
    public sealed class NavItem
    {
        /// <summary>Page Id local to the provider (combined as ProviderId:PageId).</summary>
        public string Id;
        /// <summary>Display title for the navigation button.</summary>
        public string Title;
    }

    /// <summary>
    /// Implement this interface in your mod to contribute settings pages to IMK.SettingsUI.
    /// Stable API v1: All members are considered public contract.
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>Unique provider Id (case-insensitive). Avoid ':' character.</summary>
        string Id { get; }
        /// <summary>Display title shown in left navigation and breadcrumb.</summary>
        string Title { get; }
        /// <summary>Return zero or more navigation items for this provider root. Called when user selects provider.</summary>
        IEnumerable<NavItem> GetNavItems();
        /// <summary>Optional: Build a fully custom page under parent transform. If not used, rely on card models via NavController extension.</summary>
        void BuildPage(string pageId, Transform parent);
    }

    /// <summary>
    /// Optional extension: Provide unified card models for a given page. If implemented, SettingsUI will always render
    /// the page via card list + transition animation, keeping a single page type in the right content area.
    /// </summary>
    public interface INavPageModelProvider
    {
        /// <summary>Build and return card models for the given pageId (local to the provider).</summary>
        IEnumerable<ICardModel> BuildPageModels(string pageId);
    }

    /// <summary>
    /// Optional: providers can implement breadcrumb mapping for their internal pages.
    /// Returned chain should be local to the provider (page ids without provider prefix), from top to bottom, excluding Root.
    /// Example: for pageId "Detail" with parent Inspector, return [ ("Inspector","Inspector"), ("Detail","Detail") ].
    /// </summary>
    public interface IBreadcrumbProvider
    {
        /// <summary>Try to get the breadcrumb chain (excluding Root) for the given local pageId. Return false to fallback.</summary>
        bool TryGetChain(string pageId, out List<(string id, string title)> chain);
        /// <summary>Optional: Try to get a parent page id for the given page (local ids). Return true if found.</summary>
        bool TryGetParent(string pageId, out string parentPageId);
        /// <summary>Optional: Try to get a display title for the given page (local id). Return true if found.</summary>
        bool TryGetTitle(string pageId, out string title);
    }
}
