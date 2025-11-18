using System;
using System.Collections.Generic;
using System.Reflection;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.InternalMods.ItemModKitPanel; // for ItemModKitPanelState captured item
using IMK.SettingsUI.Providers;
using UnityEngine;

namespace IMK.SettingsUI.InternalMods.Sample
{
    /// <summary>Sample provider (internal mod) for demonstration. Can be disabled in production builds.</summary>
    public sealed class SampleProvider : ISettingsProvider, INavPageModelProvider
    {
        public string Id => "Sample";
        public string Title => "Sample Mod";
        public IEnumerable<NavItem> GetNavItems()
        {
            // Keep minimal: Modifiers Test + Markdown Demo
            yield return new NavItem { Id = "Sample:ModifiersTest", Title = "Modifiers Test" };
            yield return new NavItem { Id = "Sample:RichMarkdownDemo", Title = "Markdown Demo" };
        }
        public void BuildPage(string pageId, Transform parent)
        {
            Debug.Log("[SampleProvider] BuildPage " + pageId);
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            string pureId = pageId; int colon = pageId.IndexOf(':'); if (colon >= 0 && colon < pageId.Length - 1) pureId = pageId[(colon + 1)..];
            void Render(IEnumerable<ICardModel> models)
            {
                float yOffset = 0f; const float gap = 8f;
                foreach (var m in models)
                {
                    var go = CardTemplates.Bind(m, null); go.transform.SetParent(parent, false);
                    var rt2 = go.GetComponent<RectTransform>(); rt2.anchoredPosition = new Vector2(0f, -yOffset);
                    yOffset += rt2.sizeDelta.y + gap;
                }
            }
            if (pureId == "Root") { Render(BuildRootModels()); return; }
            if (pureId == "ModifiersTest") { Render(BuildModifiersTestModels()); return; }
            if (pureId == "RichMarkdownDemo") { Render(BuildMarkdownDemo()); return; }
            var lbl = new GameObject("Label").AddComponent<UnityEngine.UI.Text>(); lbl.transform.SetParent(parent, false); lbl.font = Theme.ThemeColors.DefaultFont; lbl.color = Color.white; lbl.alignment = TextAnchor.UpperLeft; lbl.text = $"Sample Provider Page: {pureId}"; var rt = lbl.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 40f);
        }

        // INavPageModelProvider implementation used by NavController
        public IEnumerable<ICardModel> BuildPageModels(string pageId)
        {
            // Normalize page id (strip provider prefix before colon)
            string pureId = pageId; int colon = pageId.IndexOf(':'); if (colon >= 0 && colon < pageId.Length - 1) pureId = pageId[(colon + 1)..];
            Debug.Log("[SampleProvider] BuildPageModels pageId=" + pageId + " pure=" + pureId);
            if (string.Equals(pureId, "Root", StringComparison.Ordinal)) return BuildRootModels();
            if (string.Equals(pureId, "ModifiersTest", StringComparison.Ordinal)) return BuildModifiersTestModels();
            if (string.Equals(pureId, "RichMarkdownDemo", StringComparison.Ordinal)) return BuildMarkdownDemo();
            Debug.Log("[SampleProvider] BuildPageModels no match for " + pureId);
            return null;
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
            new MarkdownCardModel{ Id="sample.root.header", Title="Sample", MarkdownText="### Sample Provider\n演示入口。" },
            new NavigationCardModel{ Id="Sample:ModifiersTest", Title="Modifiers Test", Desc="反射写入演示", OnClick = ()=> Navigate("Sample:ModifiersTest") },
            new NavigationCardModel{ Id="Sample:RichMarkdownDemo", Title="Markdown Demo", Desc="基础 Markdown 示例", OnClick = ()=> Navigate("Sample:RichMarkdownDemo") },
        };

        private List<ICardModel> BuildMarkdownDemo()
        {
            // Use normal string with real newlines (plain markdown only)
            string md = "# Markdown Demo\n\n" +
                        "本页展示简化版 Markdown 渲染。高级功能(图片/表格/多级嵌套/链接点击)已关闭。\n\n" +
                        "## 支持的语法\n" +
                        "- 标题 (#, ##)\n" +
                        "- 普通段落\n" +
                        "- 行内 `code`\n" +
                        "- **加粗** 与 *斜体*\n" +
                        "- 简单列表:\n" +
                        "  - 项目 A\n" +
                        "  - 项目 B\n\n" +
                        "## 示例\n" +
                        "**粗体** *斜体* `代码`\n\n" +
                        "链接文本示例: [Example](https://example.com) (显示为普通着色文本)。\n";
            return new List<ICardModel> { new MarkdownCardModel { Id = "sample.markdown.demo", Title = "Markdown Demo", MarkdownText = md, HeightOverride = -1 } };
        }

        private List<ICardModel> BuildModifiersTestModels() => new()
        {
            new MarkdownCardModel{ Id="mods.test.header", Title="Modifier Write Tests", MarkdownText="### Modifier Write Diagnostics\n对当前捕获的物品执行不同写入方式，帮助确认哪些路径有效。请先在 ItemModKit 面板捕获一个物品。" },
            new ActionCardModel{ Id="mods.test.addDesc", Title="Add Description", Desc="TryAddModifierDescription(TestModDesc, Add, value=1)", OnInvoke = ()=> Run(()=> TryAddModifierDescription("TestModDesc","Add",1f,true,0,null)) },
            new ActionCardModel{ Id="mods.test.addRaw", Title="Add Raw Modifier", Desc="TryAddModifier(TestModRaw, value=2)", OnInvoke = ()=> Run(()=> TryAddModifierRaw("TestModRaw",2f,false,"Add")) },
            new ActionCardModel{ Id="mods.test.updateVal", Title="Update Desc Value", Desc="TrySetModifierDescriptionValue(TestModDesc, +0.5)", OnInvoke = ()=> Run(UpdateTestModDescValue) },
            new ActionCardModel{ Id="mods.test.updateType", Title="Update Desc Type", Desc="TrySetModifierDescriptionType(TestModDesc, PercentageAdd)", OnInvoke = ()=> Run(()=> TrySetModifierDescriptionType("TestModDesc","PercentageAdd")) },
            new ActionCardModel{ Id="mods.test.type.add", Title="Set Type: Add", Desc="TrySetModifierDescriptionType(TestModDesc, Add)", OnInvoke = ()=> Run(()=> TrySetModifierDescriptionType("TestModDesc","Add")) },
            new ActionCardModel{ Id="mods.test.type.padd", Title="Set Type: PercentageAdd", Desc="TrySetModifierDescriptionType(TestModDesc, PercentageAdd)", OnInvoke = ()=> Run(()=> TrySetModifierDescriptionType("TestModDesc","PercentageAdd")) },
            new ActionCardModel{ Id="mods.test.type.pmul", Title="Set Type: PercentageMultiply", Desc="TrySetModifierDescriptionType(TestModDesc, PercentageMultiply)", OnInvoke = ()=> Run(()=> TrySetModifierDescriptionType("TestModDesc","PercentageMultiply")) },
            new ActionCardModel{ Id="mods.test.removeDesc", Title="Remove Description", Desc="TryRemoveModifierDescription(TestModDesc)", OnInvoke = ()=> Run(()=> TryRemoveModifierDescription("TestModDesc")) },
            new ActionCardModel{ Id="mods.test.dedup", Title="Deduplicate", Desc="TrySanitizeModifierDescriptions()", OnInvoke = ()=> Run(TryDedup) },
            new ActionCardModel{ Id="mods.test.dump", Title="Dump Read", Desc="Read descriptors & raw modifiers", OnInvoke = ()=> Run(DumpRead) },
            new ActionCardModel{ Id="mods.test.dumpMembers", Title="Dump Desc Members", Desc="List members of TestModDesc", OnInvoke = ()=> Run(()=> DumpDescMembers("TestModDesc")) },
            new ActionCardModel{ Id="mods.test.setTypeDirect", Title="Set Type (Direct String)", Desc="Set Type/type='PercentageAdd' via reflection", OnInvoke = ()=> Run(()=> DirectSetTypeString("TestModDesc","PercentageAdd")) },
        };

        private object AcquireWrite()
        {
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) { Log("IMKDuckov type not found"); return null; }
                var prop = duck.GetProperty("Write", BindingFlags.Public | BindingFlags.Static);
                if (prop != null) return prop.GetValue(null);
                var fld = duck.GetField("Write", BindingFlags.Public | BindingFlags.Static);
                if (fld != null) return fld.GetValue(null);
                Log("IMKDuckov.Write not found");
                return null;
            }
            catch (Exception ex) { Log("AcquireWrite exception: " + ex.Message); return null; }
        }
        private object AcquireRead()
        {
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) { Log("IMKDuckov type not found"); return null; }
                var prop = duck.GetProperty("Read", BindingFlags.Public | BindingFlags.Static);
                if (prop != null) return prop.GetValue(null);
                var fld = duck.GetField("Read", BindingFlags.Public | BindingFlags.Static);
                if (fld != null) return fld.GetValue(null);
                Log("IMKDuckov.Read not found");
                return null;
            }
            catch (Exception ex) { Log("AcquireRead exception: " + ex.Message); return null; }
        }
        private static Type FindType(string fullName) { var t = Type.GetType(fullName); if (t != null) return t; foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) { try { t = asm.GetType(fullName); if (t != null) return t; } catch { } } return null; }
        private void Log(string msg) { Debug.Log("[SampleModifiersTest] " + msg); }
        private bool HasItem(out object item) { item = ItemModKitPanelState.CapturedItem; if (item == null) { Log("No captured item. Capture first in ItemModKit panel."); return false; } return true; }

        private void Run(Action act) { try { act?.Invoke(); } catch (Exception ex) { Log("Exception: " + ex.Message); } }

        // Test operations
        private void TryAddModifierDescription(string key, string type, float value, bool display, int order, string target)
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            var m = write.GetType().GetMethod("TryAddModifierDescription"); if (m == null) { Log("Method TryAddModifierDescription not found"); return; }
            var rr = m.Invoke(write, new object[] { item, key, type, value, display, order, target }); LogResult("AddDesc", key, rr);
        }
        private void TryAddModifierRaw(string key, float value, bool isPercent, string type)
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            var m = write.GetType().GetMethod("TryAddModifier"); if (m == null) { Log("Method TryAddModifier not found"); return; }
            var rr = m.Invoke(write, new object[] { item, key, value, isPercent, type, null }); LogResult("AddRaw", key, rr);
        }
        private void UpdateTestModDescValue()
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            bool found = false; float cur = 0f; var read = AcquireRead(); if (read != null) { var mRead = read.GetType().GetMethod("TryReadModifierDescriptions"); var rr = mRead?.Invoke(read, new object[] { item }); bool ok = rr != null && (bool)(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); if (ok) { var list = rr.GetType().GetProperty("Value")?.GetValue(rr) as System.Collections.IEnumerable; if (list != null) { foreach (var d in list) { if (d == null) continue; string k = Convert.ToString(GetMaybe(d, new[] { "Key", "key" })); if (k == "TestModDesc") { cur = ConvertToFloat(GetMaybe(d, new[] { "Value", "value" })); found = true; break; } } } } }
            if (!found)
            {
                // upsert ensure descriptor exists
                var mAdd = write.GetType().GetMethod("TryAddModifierDescription"); var rrAdd = mAdd?.Invoke(write, new object[] { item, "TestModDesc", "Add", 0f, true, 0, null }); LogResult("EnsureDesc(Add)", "TestModDesc", rrAdd);
            }
            float next = cur + 0.5f;
            var mSet = write.GetType().GetMethod("TrySetModifierDescriptionValue"); if (mSet == null) { Log("Method TrySetModifierDescriptionValue not found"); return; }
            var rr2 = mSet.Invoke(write, new object[] { item, "TestModDesc", next }); LogResult("SetVal", "TestModDesc", rr2);
        }
        private void TrySetModifierDescriptionType(string key, string type)
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            // ensure descriptor exists (idempotent add)
            var mAdd = write.GetType().GetMethod("TryAddModifierDescription"); var rrAdd = mAdd?.Invoke(write, new object[] { item, key, type, 0f, true, 0, null }); LogResult("EnsureDesc(Add)", key, rrAdd);
            var m = write.GetType().GetMethod("TrySetModifierDescriptionType"); if (m == null) { Log("Method TrySetModifierDescriptionType not found"); return; }
            var rr = m.Invoke(write, new object[] { item, key, type }); LogResult("SetType", key, rr);
        }
        private void TryRemoveModifierDescription(string key)
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            var m = write.GetType().GetMethod("TryRemoveModifierDescription"); if (m == null) { Log("Method TryRemoveModifierDescription not found"); return; }
            var rr = m.Invoke(write, new object[] { item, key }); LogResult("RemoveDesc", key, rr);
        }
        private void TryDedup()
        {
            if (!HasItem(out var item)) return; var write = AcquireWrite(); if (write == null) { Log("Write service null"); return; }
            var m = write.GetType().GetMethod("TrySanitizeModifierDescriptions"); if (m == null) { Log("Method TrySanitizeModifierDescriptions not found"); return; }
            var rr = m.Invoke(write, new object[] { item }); LogResult("Dedup", "*", rr);
        }
        private void DumpRead()
        {
            if (!HasItem(out var item)) return; var read = AcquireRead(); if (read == null) { Log("Read service null"); return; }
            var mDesc = read.GetType().GetMethod("TryReadModifierDescriptions"); var rr = mDesc?.Invoke(read, new object[] { item }); bool ok = rr != null && (bool)(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); int count = 0; if (ok) { var list = rr.GetType().GetProperty("Value")?.GetValue(rr) as System.Collections.IEnumerable; if (list != null) { foreach (var d in list) { if (d == null) continue; string k = Convert.ToString(GetMaybe(d, new[] { "Key", "key" })) ?? ""; float v = ConvertToFloat(GetMaybe(d, new[] { "Value", "value" })); string t = Convert.ToString(GetMaybe(d, new[] { "Type", "type" })) ?? ""; Log($"Desc[{count}] key={k} val={v} type={t}"); count++; } } }
            Log($"DumpRead descriptors total={count}");
            var mRaw = read.GetType().GetMethod("TryReadModifiers"); var rrRaw = mRaw?.Invoke(read, new object[] { item }); bool okRaw = rrRaw != null && (bool)(rrRaw.GetType().GetProperty("Ok")?.GetValue(rrRaw) ?? false); int countRaw = 0; if (okRaw) { var list2 = rrRaw.GetType().GetProperty("Value")?.GetValue(rrRaw) as System.Collections.IEnumerable; if (list2 != null) { foreach (var d in list2) { if (d == null) continue; string k = Convert.ToString(GetMaybe(d, new[] { "Key", "key" })) ?? ""; float v = ConvertToFloat(GetMaybe(d, new[] { "Value", "value" })); string t = Convert.ToString(GetMaybe(d, new[] { "Type", "type" })) ?? ""; Log($"Raw[{countRaw}] key={k} val={v} type={t}"); countRaw++; } } }
            Log($"DumpRead raw total={countRaw}");
        }

        private void DumpDescMembers(string key)
        {
            if (!HasItem(out var item)) return; var read = AcquireRead(); if (read == null) { Log("Read service null"); return; }
            var mRead = read.GetType().GetMethod("TryReadModifierDescriptions"); var rr = mRead?.Invoke(read, new object[] { item }); bool ok = rr != null && (bool)(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); if (!ok) { Log("Read descriptions failed"); return; }
            var list = rr.GetType().GetProperty("Value")?.GetValue(rr) as System.Collections.IEnumerable; if (list == null) { Log("Descriptions null"); return; }
            foreach (var d in list)
            {
                if (d == null) continue; string k = Convert.ToString(GetMaybe(d, new[] { "Key", "key", "Name", "name" })); if (k != key) continue; var dt = d.GetType(); Log($"DescType={dt.FullName}");
                try { foreach (var p in dt.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) { Log($" prop {p.Name}:{p.PropertyType.Name}"); } } catch { }
                try { foreach (var f in dt.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) { Log($" field {f.Name}:{f.FieldType.Name}"); } } catch { }
                var curType = GetMaybe(d, new[] { "Type", "type", "Kind", "kind", "Op", "op", "Mode", "mode", "Operation", "operation", "Modifier", "modifier" }); Log(" currentType=" + (curType == null ? "<null>" : curType.ToString()));
                break;
            }
        }
        private void DirectSetTypeString(string key, string type)
        {
            if (!HasItem(out var item)) return; var read = AcquireRead(); var write = AcquireWrite(); if (read == null || write == null) { Log("Read/Write null"); return; }
            var mRead = read.GetType().GetMethod("TryReadModifierDescriptions"); var rr = mRead?.Invoke(read, new object[] { item }); bool ok = rr != null && (bool)(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); if (!ok) { Log("Read descriptions failed"); return; }
            var list = rr.GetType().GetProperty("Value")?.GetValue(rr) as System.Collections.IEnumerable; if (list == null) { Log("Descriptions null"); return; }
            foreach (var d in list)
            {
                if (d == null) continue; string k = Convert.ToString(GetMaybe(d, new[] { "Key", "key", "Name", "name" })); if (k != key) continue; var dt = d.GetType();
                bool set = false; foreach (var name in new[] { "Type", "type", "Kind", "kind", "Op", "op", "Mode", "mode", "Operation", "operation", "Modifier", "modifier" })
                {
                    try
                    {
                        var p = dt.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) { if (p.PropertyType == typeof(string)) { p.SetValue(d, type); set = true; break; } }
                        var f = dt.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { if (f.FieldType == typeof(string)) { f.SetValue(d, type); set = true; break; } }
                    }
                    catch { }
                }
                Log("DirectSetTypeString result=" + set);
                break;
            }
            try { var mRe = write.GetType().GetMethod("TryReapplyModifiers"); var rr2 = mRe?.Invoke(write, new object[] { item }); LogResult("Reapply", key, rr2); } catch { }
        }
        private static object GetMaybe(object obj, string[] names) { if (obj == null) return null; var tt = obj.GetType(); foreach (var n in names) { var p = tt.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) { try { return p.GetValue(obj); } catch { } } var f = tt.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { try { return f.GetValue(obj); } catch { } } } return null; }
        private static float ConvertToFloat(object v) { try { if (v == null) return 0f; if (v is float f) return f; if (v is double d) return (float)d; if (v is int i) return i; if (v is long l) return l; if (v is string s && float.TryParse(s, out var pf)) return pf; return Convert.ToSingle(v); } catch { return 0f; } }
        private void LogResult(string action, string key, object rr)
        {
            try
            {
                bool ok = rr != null && (bool)(rr?.GetType().GetProperty("Ok")?.GetValue(rr) ?? false);
                string err = rr?.GetType().GetProperty("Error")?.GetValue(rr)?.ToString();
                Log($"{action} key={key} ok={ok} err={err}");
            }
            catch { Log($"{action} key={key} result parse failed"); }
        }
    }
}
