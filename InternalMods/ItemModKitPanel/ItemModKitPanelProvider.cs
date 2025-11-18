using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Providers;
using UnityEngine;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal sealed class ItemModKitPanelProvider : ISettingsProvider, INavPageModelProvider, IBreadcrumbProvider
    {
        public string Id => "ItemModKit";
        public string Title => "Item Mod Kit";
        public IEnumerable<NavItem> GetNavItems()
        {
            yield return new NavItem { Id = "Inspector", Title = "Inspector" };
            yield return new NavItem { Id = "Detail", Title = "Detail" };
            yield return new NavItem { Id = "VariablesOnly", Title = "Variables" };
            yield return new NavItem { Id = "Constants", Title = "Constants" };
            yield return new NavItem { Id = "Tags", Title = "Tags" };
            yield return new NavItem { Id = "Modifiers", Title = "Modifiers" };
            yield return new NavItem { Id = "Stats", Title = "Stats" };
            yield return new NavItem { Id = "Slots", Title = "Slots" };
        }
        public IEnumerable<ICardModel> BuildPageModels(string pageId)
        {
            var edit = ItemModKitPanelPages.TryBuildEditDetail(pageId); if (edit != null) return edit;
            switch (pageId)
            {
                case "Root":
                    // Root page only offers Inspector entry; other pages are accessible from Inspector/Detail
                    return new List<ICardModel>
                    {
                        new NavigationCardModel{ Id="ItemModKit:Inspector", Title="Inspector", Desc="捕获当前选中物品；捕获后才能编辑其它信息页", OnClick = ()=> FindNav()?.NavigateTo("ItemModKit:Inspector") },
                    };
                case "Inspector": return ItemModKitPanelPages.BuildInspectorPage();
                case "Detail": return ItemModKitPanelState.BuildItemDetailCards();
                case "VariablesOnly": return ItemModKitPanelPages.BuildVariablesOnlyPage();
                case "Constants": return ItemModKitPanelPages.BuildConstantsPage();
                case "Tags": return ItemModKitPanelPages.BuildTagsPage();
                case "Modifiers": return ItemModKitPanelPages.BuildModifiersPage();
                case "Stats": return ItemModKitPanelPages.BuildStatsPage();
                case "Slots": return ItemModKitPanelPages.BuildSlotsPage();
            }
            return new List<ICardModel> { new MarkdownCardModel { Id = "imk.unknown", Title = "Unknown", Markdown = "Unknown page: " + pageId } };
        }
        public void BuildPage(string pageId, Transform parent)
        {
            var models = BuildPageModels(pageId);
            if (models == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            float yOffset = 0f; const float gap = 8f;
            foreach (var m in models)
            {
                var go = IMK.SettingsUI.Cards.CardTemplates.Bind(m, null);
                go.transform.SetParent(parent, false);
                var rt2 = go.GetComponent<RectTransform>();
                rt2.anchoredPosition = new Vector2(0f, -yOffset);
                yOffset += rt2.sizeDelta.y + gap;
            }
        }

        // IBreadcrumbProvider implementations
        public bool TryGetChain(string pageId, out List<(string id, string title)> chain)
        {
            chain = null;
            // dynamic edit pages: Edit:kind:index -> Inspector > Detail > ListPage > Current
            if (pageId.StartsWith("Edit:"))
            {
                var parts = pageId.Split(':'); if (parts.Length == 3)
                {
                    var kind = parts[1]; chain = new List<(string, string)>();
                    chain.Add(("Inspector", "Inspector"));
                    chain.Add(("Detail", "Detail"));
                    var parent = KindToListPage(kind); if (parent != null) chain.Add((parent, MapTitle(parent)));
                    chain.Add((pageId, BuildEditTitle(kind, parts[2])));
                    return true;
                }
            }
            // static pages
            switch (pageId)
            {
                case "Root": chain = new List<(string, string)>(); return true; // no segments beyond provider
                case "Inspector": chain = new List<(string, string)> { ("Inspector", "Inspector") }; return true;
                case "Detail": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail") }; return true;
                case "VariablesOnly": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("VariablesOnly", "Variables") }; return true;
                case "Constants": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("Constants", "Constants") }; return true;
                case "Tags": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("Tags", "Tags") }; return true;
                case "Modifiers": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("Modifiers", "Modifiers") }; return true;
                case "Stats": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("Stats", "Stats") }; return true;
                case "Slots": chain = new List<(string, string)> { ("Inspector", "Inspector"), ("Detail", "Detail"), ("Slots", "Slots") }; return true;
            }
            return false;
        }
        public bool TryGetParent(string pageId, out string parentPageId)
        {
            parentPageId = null;
            if (pageId == "Root") return true; // stop
            if (pageId == "Detail") { parentPageId = "Inspector"; return true; }
            if (pageId == "Inspector") { parentPageId = "Root"; return true; }
            if (pageId.StartsWith("Edit:"))
            {
                var parts = pageId.Split(':'); if (parts.Length == 3) { parentPageId = KindToListPage(parts[1]); return parentPageId != null; }
            }
            // list/detail pages sit under Detail
            if (pageId == "VariablesOnly" || pageId == "Constants" || pageId == "Tags" || pageId == "Modifiers" || pageId == "Stats" || pageId == "Slots")
            { parentPageId = "Detail"; return true; }
            // default
            parentPageId = "Root"; return true;
        }
        public bool TryGetTitle(string pageId, out string title)
        {
            title = null;
            if (pageId.StartsWith("Edit:"))
            {
                var parts = pageId.Split(':'); if (parts.Length == 3) { title = BuildEditTitle(parts[1], parts[2]); return true; }
            }
            title = MapTitle(pageId); return title != null;
        }
        private string MapTitle(string pageId)
        {
            switch (pageId)
            {
                case "Inspector": return "Inspector";
                case "Detail": return "Detail";
                case "VariablesOnly": return "Variables";
                case "Constants": return "Constants";
                case "Tags": return "Tags";
                case "Modifiers": return "Modifiers";
                case "Stats": return "Stats";
                case "Slots": return "Slots";
                default: return null;
            }
        }
        private string KindToListPage(string kind)
        { switch (kind) { case "vars": return "VariablesOnly"; case "consts": return "Constants"; case "tags": return "Tags"; case "modifiers": return "Modifiers"; case "stats": return "Stats"; case "slots": return "Slots"; default: return null; } }
        private string BuildEditTitle(string kind, string index) { return $"Edit ({kind} #{index})"; }

        private IMK.SettingsUI.Navigation.NavController FindNav()
        {
            try
            {
                var go = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window");
                return go != null ? go.GetComponent<IMK.SettingsUI.Navigation.NavController>() : null;
            }
            catch { return null; }
        }
    }
}
