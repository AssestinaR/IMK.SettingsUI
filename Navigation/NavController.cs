using System.Collections.Generic;
using UnityEngine;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Providers;
using IMK.SettingsUI.Theme;

namespace IMK.SettingsUI.Navigation
{
    public sealed class NavController : MonoBehaviour
    {
        private BreadcrumbBar _breadcrumb;
        private ContentPresenter _presenter;
        private NavPane _nav;
        private int _depth = 1; // HOME = 1
        private bool _firstShowExpandTriggered = false;
        private string _currentPageId = "HOME";
        void Awake()
        {
            _breadcrumb = GetComponentInChildren<BreadcrumbBar>(true);
            var contentInset = transform.Find("Content/ContentInset");
            _presenter = contentInset != null ? contentInset.GetComponent<ContentPresenter>() : GetComponentInChildren<ContentPresenter>(true);
            _nav = transform.Find("Nav")?.GetComponent<NavPane>();
            if (_breadcrumb != null) _breadcrumb.OnNavigate = NavigateTo;
            if (_nav != null) _nav.OnSelect = HandleNavSelect;
        }
        /// <summary>
        /// Navigate to the home page.
        /// This is typically the first page shown and contains the main navigation elements.
        /// </summary>
        public void RebuildHome()
        {
            NavigateTo("HOME");
        }
        /// <summary>
        /// Schedule automatic expansion of the first Markdown card after a delay.
        /// This is useful for emphasizing information on the first show.
        /// </summary>
        public void ScheduleAutoExpandFirstMarkdown(float delaySeconds)
        {
            if (_firstShowExpandTriggered) return;
            _firstShowExpandTriggered = true;
            StartCoroutine(AutoExpandFirstMarkdown(delaySeconds));
        }
        private System.Collections.IEnumerator AutoExpandFirstMarkdown(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (_presenter == null) yield break;
            // find first MarkdownExpandable beneath presenter
            var exp = _presenter.GetComponentInChildren<MarkdownExpandable>(true);
            if (exp != null) { try { exp.TryExpand(); } catch { } }
        }
        private void Log(string msg){ Debug.Log("[SettingsUI.Nav] " + msg); }
        private void HandleNavSelect(string id)
        {
            Log("NavSelect: " + id);
            if (ProviderRegistry.All.TryGetValue(id, out var provider))
            {
                // Selecting provider always goes to ProviderId:Root
                NavigateTo(provider.Id + ":Root");
                return;
            }
            if (id == "HOME") NavigateTo("HOME");
        }
        /// <summary>
        /// Navigate to a logical page Id. Accepts: HOME or ProviderId:PageId.
        /// Triggers wipe transition based on depth.
        /// External mods can use this for programmatic navigation.
        /// </summary>
        public void NavigateTo(string id)
        {
            // Guard: warn about uncommitted transaction when leaving ItemModKit:Detail
            if (_currentPageId == "ItemModKit:Detail" && id != _currentPageId)
            {
                try
                {
                    var st = IMK.SettingsUI.InternalMods.ItemModKitPanel.ItemModKitPanelState.HasActiveTransaction;
                    if (st)
                    {
                        ConfirmModal.Show(
                            "Uncommitted Changes",
                            "You have uncommitted changes. What would you like to do?",
                            "Commit",
                            () => { IMK.SettingsUI.InternalMods.ItemModKitPanel.ItemModKitPanelState.CommitTransactionAndFlush(); InternalNavigate(id); },
                            "Discard",
                            () => { IMK.SettingsUI.InternalMods.ItemModKitPanel.ItemModKitPanelState.RollbackPreferTransaction(); InternalNavigate(id); },
                            "Cancel",
                            () => { /* do nothing, stay */ }
                        );
                        return; // wait user choice
                    }
                }
                catch { }
            }
            InternalNavigate(id);
        }

        private void InternalNavigate(string id)
        {
            Log("NavigateTo: " + id);
            int newDepth = 1;
            List<ICardModel> modelsOut = null; List<(string,string)> chainOut = null;
            // If navigating to provider id without page, rewrite to ProviderId:Root
            if (!string.IsNullOrEmpty(id) && !id.Contains(":"))
            {
                if (ProviderRegistry.All.TryGetValue(id, out var providerOnly))
                {
                    id = providerOnly.Id + ":Root"; // rewrite and continue parsing below
                }
            }
            if (id == "HOME")
            {
                // HOME delegates to CoreShell provider Root page
                if (ProviderRegistry.All.TryGetValue("CoreShell", out var core) && core is INavPageModelProvider mpHome)
                {
                    try
                    {
                        var built = mpHome.BuildPageModels("Root");
                        var listBuilt = built != null ? new List<ICardModel>(built) : new List<ICardModel>();
                        modelsOut = listBuilt; chainOut = new List<(string,string)>{ ("HOME","Home") }; newDepth = chainOut.Count;
                    }
                    catch (System.Exception ex)
                    {
                        modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateError("CoreShell","Root", ex.Message) }; chainOut = new List<(string,string)>{ ("HOME","Home") }; newDepth = chainOut.Count;
                    }
                }
                else
                {
                    modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateError("CoreShell","Root","CoreShell provider missing") }; chainOut = new List<(string,string)>{ ("HOME","Home") }; newDepth = chainOut.Count;
                }
            }
            else
            {
                // split only at the first colon to allow pageId to contain ':'
                int sep = id.IndexOf(':');
                if (sep > 0 && sep < id.Length-1)
                {
                    var providerId = id.Substring(0, sep);
                    var pageId = id.Substring(sep+1);
                    if (ProviderRegistry.All.TryGetValue(providerId, out var provider))
                    {
                        if (provider is INavPageModelProvider mp)
                        {
                            try
                            {
                                var built = mp.BuildPageModels(pageId);
                                if (built != null)
                                {
                                    var listBuilt = new List<ICardModel>(built); if (listBuilt.Count > 0) modelsOut = listBuilt;
                                }
                                if (modelsOut == null) modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateEmpty(providerId, pageId) };
                            }
                            catch (System.Exception ex)
                            {
                                UnityEngine.Debug.LogWarning("[SettingsUI.Nav] Provider BuildPageModels error: "+ex.Message);
                                modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateError(providerId, pageId, ex.Message) };
                            }
                        }
                        else
                        {
                            modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateError(providerId, pageId, "Provider does not implement INavPageModelProvider") };
                        }

                        // Build breadcrumb chain
                        chainOut = new List<(string,string)>{ ("HOME","Home"), (providerId, provider.Title) };
                        if (!string.Equals(pageId, "Root"))
                        {
                            // Provider breadcrumb support
                            if (provider is IBreadcrumbProvider bp)
                            {
                                if (bp.TryGetChain(pageId, out var chain) && chain != null && chain.Count > 0)
                                {
                                    foreach (var seg in chain)
                                    {
                                        var fullId = providerId + ":" + seg.id;
                                        chainOut.Add((fullId, string.IsNullOrEmpty(seg.title)? seg.id : seg.title));
                                    }
                                }
                                else
                                {
                                    // Try parent mapping walk-up
                                    var stack = new System.Collections.Generic.Stack<(string id,string title)>();
                                    string cur = pageId; int guard=0;
                                    while (!string.IsNullOrEmpty(cur) && guard++ < 20)
                                    {
                                        string title = cur; if (bp.TryGetTitle(cur, out var t)) title = t;
                                        stack.Push((cur, title));
                                        if (bp.TryGetParent(cur, out var parent))
                                        {
                                            if (string.Equals(parent, "Root")) break; // stop at root, do not include it
                                            cur = parent; continue;
                                        }
                                        break;
                                    }
                                    while (stack.Count>0)
                                    {
                                        var seg = stack.Pop(); chainOut.Add((providerId+":"+seg.id, seg.title));
                                    }
                                }
                            }
                            else
                            {
                                // Default: single segment for current page
                                chainOut.Add((providerId+":"+pageId, pageId));
                            }
                        }
                        newDepth = chainOut.Count;
                    }
                    else
                    {
                        modelsOut = new List<ICardModel>{ ErrorCardFactory.CreateError(providerId, pageId, "Provider not found") };
                        chainOut = new List<(string,string)>{ ("HOME","Home"), (providerId, providerId), (id, pageId) }; newDepth = chainOut.Count;
                    }
                }
            }
            if (modelsOut != null)
            {
                bool forward = newDepth > _depth;
                if (_presenter != null) _presenter.SetWithTransition(modelsOut, forward); else _presenter?.Set(modelsOut);
                _breadcrumb?.SetSegments(chainOut);
                _depth = newDepth; _currentPageId = id;
            }
        }
    }
}
