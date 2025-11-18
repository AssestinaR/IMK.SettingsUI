using System.Collections.Generic;
using UnityEngine;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Providers;
using System;
using IMK.SettingsUI.InternalMods.CoreShell; // reuse ProviderManagerSchema/DataSet
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.SettingsPanel
{
    /// <summary>Internal provider exposing SettingsUI self-configuration page.</summary>
    public sealed class SettingsPanelProvider : ISettingsProvider, INavPageModelProvider
    {
        public string Id => "SettingsUI";
        public string Title => "Settings UI";
        // Provide direct nav items (optional) but Root will still be shown first when selecting provider
        public IEnumerable<NavItem> GetNavItems(){
            yield return new NavItem{ Id="SettingsUI:Configure", Title="Configure" };
            yield return new NavItem{ Id="SettingsUI:Diagnostics", Title="Diagnostics" };
            yield return new NavItem{ Id="SettingsUI:ProviderManager", Title="Manage Providers" };
        }
        public void BuildPage(string pageId, Transform parent)
        {
            // Legacy path: delegate to model building if NavController uses BuildPage instead
            var pure = pageId; int c = pageId.IndexOf(':'); if (c>=0 && c<pageId.Length-1) pure = pageId.Substring(c+1);
            var models = BuildPageModels(pure);
            for (int i=parent.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            if (models != null)
            {
                float y=0f; const float gap=8f; foreach (var m in models){ var go = CardTemplates.Bind(m,null); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.anchoredPosition=new Vector2(0f,-y); y += rt.sizeDelta.y + gap; }
            }
            else
            {
                var lbl = new GameObject("SettingsUILabel").AddComponent<UnityEngine.UI.Text>(); lbl.transform.SetParent(parent,false); lbl.font = Theme.ThemeColors.DefaultFont; lbl.color = Color.white; lbl.alignment = TextAnchor.UpperLeft; lbl.text = "Settings UI provider page: " + pure + " (no models)"; var rt = lbl.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(0f,1f); rt.anchorMax=new Vector2(1f,1f); rt.pivot=new Vector2(0.5f,1f); rt.sizeDelta=new Vector2(0f,40f);
            }
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
        private List<ICardModel> BuildRootModels()
        {
            var list = new List<ICardModel>();
            list.Add(new MarkdownCardModel{ Id="settings.root.header", Title="Settings UI", Markdown="# Settings UI Root\n请选择一个子页面:" });
            list.Add(new NavigationCardModel{ Id="SettingsUI:Configure", Title="Configure", Desc="配置窗口与卡片参数", OnClick = ()=> Navigate("SettingsUI:Configure") });
            list.Add(new NavigationCardModel{ Id="SettingsUI:Diagnostics", Title="Diagnostics", Desc="调试与日志开关", OnClick = ()=> Navigate("SettingsUI:Diagnostics") });
            list.Add(new NavigationCardModel{ Id="SettingsUI:ProviderManager", Title="Manage Providers", Desc="调整左侧导航栏 Provider 的显示与顺序", OnClick = ()=> Navigate("SettingsUI:ProviderManager") });
            return list;
        }
        private List<ICardModel> BuildConfigureModels(){ return UiSettingsPageBuilder.Build(); }
        private List<ICardModel> BuildDiagnosticsModels()
        {
            var list = new List<ICardModel>();
            list.Add(new MarkdownCardModel{ Id="ui.debug.header", Title="Diagnostics", Markdown="## Diagnostics\n启用或关闭内部调试信息。" });
            list.Add(new BoundSettingCardModel{ Id="ui.debug.textDiag", Title="Text Diagnostics", Desc="输出卡片绑定/文本渲染日志", Getter=()=> IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled, Setter=v=> IMK.SettingsUI.Diagnostics.DebugFlags.TextDiagEnabled = System.Convert.ToBoolean(v), ValueType=typeof(bool) });
            list.Add(new BoundSettingCardModel{ Id="ui.debug.tableDiag", Title="Table Diagnostics", Desc="输出表格布局/数据日志", Getter=()=> IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled, Setter=v=> IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled = System.Convert.ToBoolean(v), ValueType=typeof(bool) });
            return list;
        }
        private List<ICardModel> BuildProviderManagerModels()
        {
            var list = new List<ICardModel>();
            list.Add(new MarkdownCardModel{ Id="providers.header", Title="Provider Manager", Markdown="### Provider Manager\n隐藏或显示某些 Provider，并调整顺序。\n- Core 与 Sample 默认为隐藏。\n- 修改 `Enabled` 或 `Order`，点击下方 Save 保存并刷新左侧导航。" });
            var schema = new IMK.SettingsUI.InternalMods.CoreShell.ProviderManagerSchema();
            var data = new IMK.SettingsUI.InternalMods.CoreShell.ProviderManagerDataSet();
            var table = new TableCardModel{ Id="providers.table", Title="Providers", Schema=schema, DataSet=data, ShowAddButton=false, ShowImportExport=false, ShowMoveButtons=false };
            table.Size = CardSize.XLarge;
            list.Add(table);
            list.Add(new ActionCardModel{ Id="providers.save", Title="Save", Desc="保存并刷新左侧导航", OnInvoke = ()=> { try{ data.Commit(); } catch {} } });
            list.Add(new ActionCardModel{ Id="providers.reload", Title="Reload", Desc="从文件重新加载偏好", OnInvoke = ()=> { try{ data.Reload(); } catch {} } });
            return list;
        }
        public IEnumerable<ICardModel> BuildPageModels(string pageId)
        {
            if (string.Equals(pageId, "Root", StringComparison.Ordinal)) return BuildRootModels();
            if (string.Equals(pageId, "Configure", StringComparison.Ordinal)) return BuildConfigureModels();
            if (string.Equals(pageId, "Diagnostics", StringComparison.Ordinal)) return BuildDiagnosticsModels();
            if (string.Equals(pageId, "ProviderManager", StringComparison.Ordinal)) return BuildProviderManagerModels();
            return null;
        }
    }
}
