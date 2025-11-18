using System;
using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal static class ItemModKitPanelPages
    {
        private static void Navigate(string id)
        {
            try
            {
                IMK.SettingsUI.PublicApi.EnsureInitialized();
                var nav = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window")?.GetComponent<IMK.SettingsUI.Navigation.NavController>();
                if (nav != null) nav.NavigateTo(id);
            }
            catch { }
        }
        private static SchemaTableController FindCurrentTable()
        {
            try
            {
                var presenterGo = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window/Content/ContentInset");
                var presenter = presenterGo != null ? presenterGo.GetComponent<IMK.SettingsUI.Navigation.ContentPresenter>() : null;
                if (presenter == null) return null;
                var table = presenter.GetComponentInChildren<SchemaTableController>(true);
                return table;
            }
            catch { return null; }
        }

        // Registry for active datasets per kind (variables/constants/tags/stats/slots/modifiers)
        private static readonly Dictionary<string,(ITableSchema schema, ITableDataSet data)> _activeDataSets = new Dictionary<string,(ITableSchema, ITableDataSet)>(StringComparer.OrdinalIgnoreCase);
        internal static void RegisterActive(string kind, ITableSchema schema, ITableDataSet data){ if (string.IsNullOrEmpty(kind) || schema==null || data==null) return; _activeDataSets[kind]= (schema,data); }
        internal static bool TryGetActive(string kind, out ITableSchema schema, out ITableDataSet data)
        { if(_activeDataSets.TryGetValue(kind, out var tup)){ schema=tup.schema; data=tup.data; return true;} schema=null; data=null; return false; }

        // Editing context storage (row snapshots prior to entering detail page)
        private sealed class EditContext
        {
            public string Kind; public int Index; public Dictionary<string,object> Values = new Dictionary<string,object>();
        }
        private static readonly Dictionary<string, EditContext> _editContexts = new Dictionary<string, EditContext>(StringComparer.OrdinalIgnoreCase);
        private static string MakeEditContextKey(string kind, int index)=> kind+":"+index;
        private static void CaptureEditContext(string kind, int index)
        {
            if (!_activeDataSets.TryGetValue(kind, out var tuple)) return; if (index < 0 || index >= tuple.data.Count) return;
            var adapter = tuple.data.GetRow(index);
            var ctx = new EditContext{ Kind=kind, Index=index };
            foreach (var col in tuple.schema.Columns) ctx.Values[col.Id] = adapter.Get(col.Id);
            _editContexts[MakeEditContextKey(kind,index)] = ctx;
        }

        private static List<ICardModel> BuildDetailPage(string kind, int index)
        {
            var list = new List<ICardModel>();
            if (!TryGetActive(kind, out var schema, out var data) || index < 0 || index >= data.Count)
            {
                list.Add(new MarkdownCardModel{ Id="imk.edit.invalid", Title="Edit", Markdown="无法找到要编辑的对象 (可能已删除)。" });
                list.Add(new NavigationCardModel{ Id="ItemModKit:BackTo"+kind, Title="Back", Desc="返回", OnClick = ()=> Navigate("ItemModKit:"+KindToPage(kind)) });
                return list;
            }
            var ctxKey = MakeEditContextKey(kind,index); _editContexts.TryGetValue(ctxKey, out var ctx);
            string headerInfo = ctx==null? "(no snapshot)" : string.Join("; ", ctx.Values);
            list.Add(new MarkdownCardModel{ Id=$"imk.edit.{kind}.header", Title="Detail", Markdown=$"### Editing {kind} Row {index}\n{headerInfo}" });

            // descriptor registry integration
            var descriptors = ItemModKitDetailRegistry.BuildDescriptors(kind, index);
            if (descriptors != null && descriptors.Count > 0)
            {
                foreach (var d in descriptors)
                {
                    if (!d.Editable)
                    {
                        list.Add(new MarkdownCardModel{ Id=$"imk.edit.{kind}.ro.{d.Id}", Title=d.Title, Markdown=$"`{d.Id}`: {FormatVal(d.Get())}" });
                        continue;
                    }
                    list.Add(new SettingCardModel{ Id=$"imk.edit.{kind}.{d.Id}", Title=d.Title, Desc=d.Id, Initial=d.Get(), Pending=d.Get(), Options=d.Options, Min=d.Min, Max=d.Max });
                }
            }
            else
            {
                foreach (var col in schema.Columns)
                {
                    var current = (ctx != null && ctx.Values.ContainsKey(col.Id)) ? ctx.Values[col.Id] : data.GetRow(index).Get(col.Id);
                    if (col.ReadOnly)
                    {
                        list.Add(new MarkdownCardModel{ Id=$"imk.edit.{kind}.ro.{col.Id}", Title=col.Title, Markdown=$"`{col.Id}`: {FormatVal(current)}" });
                        continue;
                    }
                    list.Add(new SettingCardModel{ Id=$"imk.edit.{kind}.{col.Id}", Title=col.Title, Desc=col.Id, Initial=current, Pending=current, Options=col.Options, Min=col.Min, Max=col.Max });
                }
            }
            list.Add(new ActionCardModel{ Id=$"imk.edit.{kind}.save", Title="Save Changes", Desc="应用当前修改并返回", OnInvoke = ()=> {
                // apply changes
                if (descriptors != null && descriptors.Count > 0)
                {
                    foreach (var m in list)
                        if (m is SettingCardModel sc && sc.Pending != null)
                        {
                            var d = descriptors.Find(x=> x.Id==sc.Desc); if (d!=null && !Equals(sc.Pending, d.Get())) d.Set(sc.Pending);
                        }
                }
                else
                {
                    var adapter = data.GetRow(index);
                    foreach (var m in list)
                        if (m is SettingCardModel sc && sc.Pending != null) adapter.Set(sc.Desc, sc.Pending);
                }
                try { data.Commit(); } catch { }
                Navigate("ItemModKit:"+KindToPage(kind));
            }});
            list.Add(new ActionCardModel{ Id=$"imk.edit.{kind}.revert", Title="Revert", Desc="丢弃修改并返回", OnInvoke = ()=> Navigate("ItemModKit:"+KindToPage(kind)) });
            list.Add(new ActionCardModel{ Id=$"imk.edit.{kind}.delete", Title="Delete Row", Desc="删除本行并返回", OnInvoke = ()=> { try { data.RemoveAt(index); data.Commit(); } catch { } Navigate("ItemModKit:"+KindToPage(kind)); } });
            list.Add(new ActionCardModel{ Id=$"imk.edit.{kind}.back", Title="Back", Desc="返回", OnInvoke = ()=> Navigate("ItemModKit:"+KindToPage(kind)) });
            return list;
        }
        private static string FormatVal(object v){ return v==null? "<null>" : v.ToString(); }
        private static string KindToPage(string kind)
        { switch(kind){ case "vars": return "VariablesOnly"; case "consts": return "Constants"; case "tags": return "Tags"; case "modifiers": return "Modifiers"; case "stats": return "Stats"; case "slots": return "Slots"; default: return "VariablesOnly"; } }

        private static List<ICardModel> BuildStandardTablePage(string idPrefix, string title, string headerMarkdown,
            Func<ITableSchema> schemaFactory, Func<ITableDataSet> dataFactory, string kind)
        {
            var list = new List<ICardModel>();
            if (ItemModKitPanelState.CapturedItem == null)
            { list.Add(new MarkdownCardModel{ Id=$"{idPrefix}.none", Title="No Item", Markdown="请在 Inspector 页面捕获一个物品。" }); return list; }
            list.Add(new MarkdownCardModel{ Id=$"{idPrefix}.header", Title=title, Markdown=headerMarkdown });
            var schema = schemaFactory(); var data = dataFactory(); RegisterActive(kind, schema, data);
            var table = new TableCardModel{ Id=$"{idPrefix}.table", Title=title, Schema = schema, DataSet = data, ShowAddButton = false, ShowImportExport = false }; table.Size = CardSize.XLarge; list.Add(table);
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.edit", Title="Edit Selected", Desc="进入详情编辑页", OnInvoke = ()=> { var t=FindCurrentTable(); if (t==null || t.SelectedIndex<0){ UnityEngine.Debug.LogWarning("[ItemModKitPanel] 无选中行"); return; } CaptureEditContext(kind, t.SelectedIndex); Navigate($"ItemModKit:Edit:{kind}:{t.SelectedIndex}"); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.add", Title="Add", Desc="新增", OnInvoke = ()=> { var t=FindCurrentTable(); t?.AddNew(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.remove", Title="Remove", Desc="移除选中", OnInvoke = ()=> { var t=FindCurrentTable(); t?.RemoveSelected(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.up", Title="Up", Desc="上移选中", OnInvoke = ()=> { var t=FindCurrentTable(); t?.MoveSelectedUp(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.down", Title="Down", Desc="下移选中", OnInvoke = ()=> { var t=FindCurrentTable(); t?.MoveSelectedDown(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.save", Title="Save", Desc="保存", OnInvoke = ()=> { var t=FindCurrentTable(); t?.Save(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.reload", Title="Reload", Desc="重新读取", OnInvoke = ()=> { var t=FindCurrentTable(); t?.Reload(); } });
            list.Add(new ActionCardModel{ Id=$"{idPrefix}.back", Title="Back", Desc="返回详情", OnInvoke = ()=> Navigate("ItemModKit:Detail") });
            return list;
        }

        public static List<ICardModel> BuildVariablesOnlyPage(){ return BuildStandardTablePage("imk.varsonly", "Variables", "编辑变量", () => new VariablesOnlySchema(), () => new VariablesOnlyDataSet(), "vars"); }
        public static List<ICardModel> BuildConstantsPage(){ return BuildStandardTablePage("imk.consts", "Constants", "编辑常量", () => new ConstantsSchema(), () => new ConstantsOnlyDataSet(), "consts"); }
        public static List<ICardModel> BuildTagsPage(){ return BuildStandardTablePage("imk.tags", "Tags", "编辑标签", () => new TagsSchema(), () => new TagsOnlyDataSet(), "tags"); }
        public static List<ICardModel> BuildStatsPage(){ return BuildStandardTablePage("imk.stats", "Stats", "查看并编辑当前物品的 Stats", () => new StatsSchema(), () => new StatsDataSet(), "stats"); }
        public static List<ICardModel> BuildSlotsPage(){ return BuildStandardTablePage("imk.slots", "Slots", "查看并编辑当前物品的插槽", () => new SlotsSchema(), () => new SlotsDataSet(), "slots"); }
        public static List<ICardModel> BuildModifiersPage(){ return BuildStandardTablePage("imk.mods", "Modifiers", "查看并编辑当前物品的 Modifiers（ModifierDescription）", () => new ModifiersSchema(), () => new ModifiersDataSet(), "modifiers"); }

        public static List<ICardModel> BuildInspectorPage()
        {
            var list = new List<ICardModel>(); string capturedTitle = ItemModKitPanelState.CapturedItemTitle ?? "<none>";
            list.Add(new MarkdownCardModel{ Id="imk.inspector.header", Title="Inspector", Markdown=$"### Inspector\n当前捕获物品: **{capturedTitle}**\n\n使用下方按钮捕获当前选中物品。" });
            list.Add(new ActionCardModel{ Id="imk.inspector.capture", Title="Capture Selected Item", Desc="尝试捕获当前选中物品", OnInvoke = ()=> { string err; if (ItemModKitPanelState.TryCaptureSelected(out err)) { UnityEngine.Debug.Log("[ItemModKitPanel] Captured item: "+ ItemModKitPanelState.CapturedItemTitle); Navigate("ItemModKit:Inspector"); } else { UnityEngine.Debug.LogWarning("[ItemModKitPanel] Capture failed: "+err); } } });
            if (ItemModKitPanelState.CapturedItem != null)
            {
                list.Add(new NavigationCardModel{ Id="ItemModKit:Detail", Title="Detail", Desc="物品详细视图", OnClick = ()=> Navigate("ItemModKit:Detail") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:VariablesOnly", Title="Variables", Desc="变量编辑", OnClick = ()=> Navigate("ItemModKit:VariablesOnly") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:Constants", Title="Constants", Desc="常量编辑", OnClick = ()=> Navigate("ItemModKit:Constants") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:Tags", Title="Tags", Desc="标签编辑", OnClick = ()=> Navigate("ItemModKit:Tags") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:Modifiers", Title="Modifiers", Desc="修饰器编辑", OnClick = ()=> Navigate("ItemModKit:Modifiers") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:Stats", Title="Stats", Desc="统计查看", OnClick = ()=> Navigate("ItemModKit:Stats") });
                list.Add(new NavigationCardModel{ Id="ItemModKit:Slots", Title="Slots", Desc="插槽查看", OnClick = ()=> Navigate("ItemModKit:Slots") });
            }
            return list;
        }

        // External entry from provider for detail page
        internal static List<ICardModel> TryBuildEditDetail(string pageId)
        {
            // pageId expected: Edit:kind:index
            if (!pageId.StartsWith("Edit:")) return null;
            var parts = pageId.Split(':'); if (parts.Length != 3) return null; string kind = parts[1]; if (!int.TryParse(parts[2], out var index)) return null;
            return BuildDetailPage(kind, index);
        }
    }
}
