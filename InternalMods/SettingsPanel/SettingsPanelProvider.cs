using System;
using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.InternalMods.CoreShell; // reuse ProviderManagerSchema/DataSet
using IMK.SettingsUI.Providers;
using IMK.SettingsUI.Table;
using UnityEngine;

namespace IMK.SettingsUI.InternalMods.SettingsPanel
{
    /// <summary>Internal provider exposing SettingsUI self-configuration page.</summary>
    public sealed class SettingsPanelProvider : ISettingsProvider, INavPageModelProvider
    {
        public string Id => "SettingsUI";
        public string Title => "Settings UI";
        // Provide direct nav items (optional) but Root will still be shown first when selecting provider
        public IEnumerable<NavItem> GetNavItems()
        {
            yield return new NavItem { Id = "SettingsUI:Configure", Title = "Configure" };
            yield return new NavItem { Id = "SettingsUI:Diagnostics", Title = "Diagnostics" };
            yield return new NavItem { Id = "SettingsUI:ProviderManager", Title = "Manage Providers" };
        }
        public void BuildPage(string pageId, Transform parent)
        {
            var pure = pageId; int c = pageId.IndexOf(':'); if (c >= 0 && c < pageId.Length - 1) pure = pageId[(c + 1)..];
            var models = BuildPageModels(pure);
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            if (models == null)
            {
                var lbl = new GameObject("SettingsUILabel").AddComponent<UnityEngine.UI.Text>(); lbl.transform.SetParent(parent, false); lbl.font = Theme.ThemeColors.DefaultFont; lbl.color = Color.white; lbl.alignment = TextAnchor.UpperLeft; lbl.text = "Settings UI provider page: " + pure + " (no models)"; var rt = lbl.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 40f); return;
            }
            float y = 0f; const float gap = 8f; foreach (var m in models) { var go = CardTemplates.Bind(m, null); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(0f, -y); y += rt.sizeDelta.y + gap; }
        }
        private static void Navigate(string logicalPageId)
        {
            try
            {
                IMK.SettingsUI.PublicApi.EnsureInitialized();
                var nav = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window")?.GetComponent<IMK.SettingsUI.Navigation.NavController>();
                if (nav != null) nav.NavigateTo(logicalPageId);
            }
            catch { }
        }
        private List<ICardModel> BuildRootModels() => new()
        {
            new MarkdownCardModel{ Id="settings.root.header", Title="Settings UI", Markdown="# Settings UI Root\n请选择一个子页面:" },
            new NavigationCardModel{ Id="SettingsUI:Configure", Title="Configure", Desc="配置窗口与卡片参数", OnClick = ()=> Navigate("SettingsUI:Configure") },
            new NavigationCardModel{ Id="SettingsUI:Diagnostics", Title="Diagnostics", Desc="调试与日志开关", OnClick = ()=> Navigate("SettingsUI:Diagnostics") },
            new NavigationCardModel{ Id="SettingsUI:ProviderManager", Title="Manage Providers", Desc="调整左侧导航栏 Provider 的显示与顺序", OnClick = ()=> Navigate("SettingsUI:ProviderManager") },
        };
        private List<ICardModel> BuildConfigureModels() { return UiSettingsPageBuilder.Build(); }
        private List<ICardModel> BuildDiagnosticsModels() => new()
        {
            new MarkdownCardModel{ Id="ui.debug.header", Title="Diagnostics", Markdown="## Diagnostics\n启用或关闭内部调试信息。" },
            new BoundSettingCardModel{ Id="ui.debug.textDiag", Title="Text Diagnostics", Desc="输出卡片绑定/文本渲染日志", Getter=()=> IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled, Setter=v=> IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled = System.Convert.ToBoolean(v), ValueType=typeof(bool) },
            new BoundSettingCardModel{ Id="ui.debug.tableDiag", Title="Table Diagnostics", Desc="输出表格布局/数据日志", Getter=()=> IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled, Setter=v=> IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled = System.Convert.ToBoolean(v), ValueType=typeof(bool) },
        };
        private List<ICardModel> BuildProviderManagerModels()
        {
            var schema = new ProviderManagerSchema();
            var data = new ProviderManagerDataSet();
            var table = new TableCardModel { Id = "providers.table", Title = "Providers", Schema = schema, DataSet = data, ShowAddButton = false, ShowImportExport = false, ShowMoveButtons = false, Size = CardSize.XLarge };
            return new List<ICardModel>
            {
                new MarkdownCardModel{ Id="providers.header", Title="Provider Manager", Markdown="### Provider Manager\n隐藏或显示某些 Provider，并调整顺序。\n- Core 与 Sample 默认为隐藏。\n- 修改 `Enabled` 或 `Order`，点击下方 Save 保存并刷新左侧导航。" },
                table,
                new ActionCardModel{ Id="providers.save", Title="Save", Desc="保存并刷新左侧导航", OnInvoke = ()=> { try{ data.Commit(); } catch {} } },
                new ActionCardModel{ Id="providers.reload", Title="Reload", Desc="从文件重新加载偏好", OnInvoke = ()=> { try{ data.Reload(); } catch {} } },
            };
        }
        public IEnumerable<ICardModel> BuildPageModels(string pageId) => pageId switch
        {
            var p when string.Equals(p, "Root", StringComparison.Ordinal) => BuildRootModels(),
            var p when string.Equals(p, "Configure", StringComparison.Ordinal) => BuildConfigureModels(),
            var p when string.Equals(p, "Diagnostics", StringComparison.Ordinal) => BuildDiagnosticsModels(),
            var p when string.Equals(p, "ProviderManager", StringComparison.Ordinal) => BuildProviderManagerModels(),
            _ => null
        };
    }
}
