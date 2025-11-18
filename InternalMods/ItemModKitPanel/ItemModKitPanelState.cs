using System;
using System.Collections.Generic;
using System.Reflection;
using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    /// <summary>Runtime state holder for ItemModKit panel (captured item & generated pages).</summary>
    internal static class ItemModKitPanelState
    {
        public static object CapturedItem; // last captured raw item object
        public static string CapturedItemTitle; // display name (Name or RawName)
        public static DateTime CapturedTime;
        // Original values snapshot at capture time (memberName -> value)
        private static Dictionary<string, object> _originalSnapshot = new Dictionary<string, object>();
        // Latest read values (for incremental refresh if needed)
        private static Dictionary<string, object> _currentValues = new Dictionary<string, object>();
        private static readonly HashSet<string> s_coreIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Name", "RawName", "TypeId", "Quality", "DisplayQuality", "Value" };
        private static object _originalCoreFields; // ItemModKit.Core.CoreFields snapshot
        private static string _txToken; // active transaction token if any
        public static bool HasActiveTransaction => !string.IsNullOrEmpty(_txToken);

        /// <summary>Attempt to capture current selected item via IMKDuckov.UISelection.</summary>
        public static bool TryCaptureSelected(out string error)
        {
            error = null;
            try
            {
                object current;
                // IMKDuckov.UISelection.TryGetCurrentItem(out object item)
                var uiSelType = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov");
                if (uiSelType == null) { error = "IMKDuckov type not found"; return false; }
                var uiSelProp = uiSelType.GetProperty("UISelection", BindingFlags.Public|BindingFlags.Static);
                object uiSelObj = null;
                if (uiSelProp != null)
                {
                    uiSelObj = uiSelProp.GetValue(null);
                }
                else
                {
                    // fallback: field lookup
                    var uiSelField = uiSelType.GetField("UISelection", BindingFlags.Public|BindingFlags.Static);
                    if (uiSelField != null) uiSelObj = uiSelField.GetValue(null);
                }
                if (uiSelObj == null) { error = "UISelection instance null"; return false; }
                var tryGetMethod = uiSelObj.GetType().GetMethod("TryGetCurrentItem", BindingFlags.Public|BindingFlags.Instance); if (tryGetMethod == null) { error = "TryGetCurrentItem missing"; return false; }
                object[] args = { null }; bool ok = (bool)tryGetMethod.Invoke(uiSelObj, args);
                if (!ok || args[0] == null) { error = "No item selected"; return false; }
                current = args[0];
                CapturedItem = current; CapturedItemTitle = TryGetName(current) ?? "Item"; CapturedTime = DateTime.UtcNow;
                _txToken = null; // reset tx when capturing new item
                BuildOriginalSnapshot();
                CaptureCoreSnapshot();
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static void EnsureTransactionStarted()
        {
            if (CapturedItem == null) return;
            if (!string.IsNullOrEmpty(_txToken)) return;
            try
            {
                var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return;
                var write = duck.GetProperty("Write", BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                var m = write?.GetType().GetMethod("BeginTransaction", new Type[]{ typeof(object) });
                if (m != null)
                {
                    var tok = m.Invoke(write, new object[]{ CapturedItem }) as string;
                    if (!string.IsNullOrEmpty(tok)) _txToken = tok;
                }
            }
            catch { }
        }

        private static void CaptureCoreSnapshot()
        {
            _originalCoreFields = null;
            try
            {
                var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return;
                var read = duck.GetProperty("Read", BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                var helper = GetDuckovType("ItemModKit.Core.SnapshotHelper"); if (helper == null) return;
                var cap = helper.GetMethod("CaptureCore", BindingFlags.Public|BindingFlags.Static);
                _originalCoreFields = cap?.Invoke(null, new object[]{ read, CapturedItem });
            }
            catch { }
        }

        private static Type GetDuckovType(string fullName)
        {
            var t = Type.GetType(fullName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(fullName); if (t != null) return t; } catch { }
            }
            return null;
        }

        private static void BuildOriginalSnapshot()
        {
            _originalSnapshot.Clear(); _currentValues.Clear();
            if (CapturedItem == null) return; var t = CapturedItem.GetType();
            var members = new List<MemberInfo>(); members.AddRange(t.GetProperties(BindingFlags.Public|BindingFlags.Instance)); members.AddRange(t.GetFields(BindingFlags.Public|BindingFlags.Instance));
            foreach (var m in members)
            {
                object val = null; Type vt = null; bool readable=false;
                try
                {
                    if (m is PropertyInfo p && p.CanRead) { vt = p.PropertyType; if (IsPrimitiveSupported(vt)) { val = p.GetValue(CapturedItem); readable=true; } }
                    else if (m is FieldInfo f && f.IsPublic) { vt = f.FieldType; if (IsPrimitiveSupported(vt)) { val = f.GetValue(CapturedItem); readable=true; } }
                }
                catch { }
                if (readable) { _originalSnapshot[m.Name] = val; _currentValues[m.Name] = val; }
            }
        }

        private static string TryGetName(object item)
        {
            if (item == null) return null; var t = item.GetType();
            foreach (var nameField in new[]{"Name","RawName","DisplayName","name"})
            {
                var p = t.GetProperty(nameField, BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase); if (p!=null && p.PropertyType==typeof(string)) { var v = p.GetValue(item) as string; if (!string.IsNullOrEmpty(v)) return v; }
                var f = t.GetField(nameField, BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase); if (f!=null && f.FieldType==typeof(string)) { var v = f.GetValue(item) as string; if (!string.IsNullOrEmpty(v)) return v; }
            }
            return null;
        }

        /// <summary>Build card models for captured item (reflection of basic fields/properties).</summary>
        public static List<ICardModel> BuildItemDetailCards()
        {
            var list = new List<ICardModel>();
            if (CapturedItem == null)
            {
                list.Add(new MarkdownCardModel{ Id="imk.item.none", Title="No Item", Markdown="尚未捕获物品。请返回 Inspector 页面并点击 Capture 按钮。" });
                return list;
            }
            list.Add(new MarkdownCardModel{ Id="imk.item.header", Title=CapturedItemTitle, Markdown=$"### {CapturedItemTitle}\n捕获时间: {CapturedTime:HH:mm:ss}\n类型: {CapturedItem.GetType().Name}" });
            var t = CapturedItem.GetType();
            var members = new List<MemberInfo>(); members.AddRange(t.GetProperties(BindingFlags.Public|BindingFlags.Instance)); members.AddRange(t.GetFields(BindingFlags.Public|BindingFlags.Instance));
            int added = 0;
            foreach (var m in members)
            {
                Type valType = null; bool canRead=false; bool canWrite=false; System.Func<object> getter=null; System.Action<object> rawSetter=null; string id=m.Name;
                if (m is PropertyInfo p)
                {
                    valType = p.PropertyType; canRead = p.CanRead; canWrite = p.CanWrite && p.SetMethod!=null && p.SetMethod.IsPublic;
                    if (canRead) getter = ()=> { try { return p.GetValue(CapturedItem); } catch { return null; } };
                    if (canWrite) rawSetter = v=> { try { p.SetValue(CapturedItem, ConvertValue(v, valType)); } catch { } };
                }
                else if (m is FieldInfo f)
                {
                    valType = f.FieldType; canRead=true; canWrite=!f.IsInitOnly && !f.IsLiteral && f.IsPublic;
                    getter = ()=> { try { return f.GetValue(CapturedItem); } catch { return null; } };
                    if (canWrite) rawSetter = v=> { try { f.SetValue(CapturedItem, ConvertValue(v, valType)); } catch { } };
                }
                if (!canRead) continue; if (!IsPrimitiveSupported(valType)) continue;
                var original = _originalSnapshot.TryGetValue(id, out var ov) ? ov : getter();

                // Build setter with transaction awareness
                System.Action<object> setter = null;
                if (s_coreIds.Contains(id))
                {
                    setter = v=> { EnsureTransactionStarted(); TryWriteCoreField(id, v); };
                }
                else if (rawSetter != null)
                {
                    setter = v=> { EnsureTransactionStarted(); rawSetter(v); };
                }

                var bound = new BoundSettingCardModel
                {
                    Id = "imk.item."+id,
                    Title = id,
                    Desc = valType.Name,
                    Getter = getter,
                    Setter = setter,
                    ValueType = valType,
                    Pending = getter(),
                    OriginalValue = original,
                    Size = CardSize.Small
                };
                if (valType.IsEnum) { try { bound.Options = Enum.GetNames(valType); } catch { } }
                list.Add(bound);
                added++; if (added >= 40) break;
            }
            if (added==0) list.Add(new MarkdownCardModel{ Id="imk.item.empty", Title="No Editable Fields", Markdown="未找到可显示的基础字段。" });
            // navigation to advanced collections
            list.Add(new NavigationCardModel{ Id="ItemModKit:VariablesOnly", Title="Variables", Desc="编辑变量", OnClick = ()=> NavigateProgrammatic("ItemModKit:VariablesOnly") });
            list.Add(new NavigationCardModel{ Id="ItemModKit:Constants", Title="Constants", Desc="编辑常量", OnClick = ()=> NavigateProgrammatic("ItemModKit:Constants") });
            list.Add(new NavigationCardModel{ Id="ItemModKit:Tags", Title="Tags", Desc="编辑标签", OnClick = ()=> NavigateProgrammatic("ItemModKit:Tags") });
            list.Add(new NavigationCardModel{ Id="ItemModKit:Stats", Title="Stats", Desc="查看并编辑数值统计", OnClick = ()=> NavigateProgrammatic("ItemModKit:Stats") });
            list.Add(new NavigationCardModel{ Id="ItemModKit:Slots", Title="Slots", Desc="查看并管理插槽", OnClick = ()=> NavigateProgrammatic("ItemModKit:Slots") });
            list.Add(new NavigationCardModel{ Id="ItemModKit:Modifiers", Title="Modifiers", Desc="查看并编辑修饰符描述", OnClick = ()=> NavigateProgrammatic("ItemModKit:Modifiers") });
            list.Add(new ActionCardModel{ Id="imk.item.apply", Title="Apply / Commit", Desc="提交事务并刷新持久化", OnInvoke = ()=> { CommitTransactionAndFlush(); RebuildCurrentDetailPage(); } });
            list.Add(new ActionCardModel{ Id="imk.item.refresh", Title="Refresh Values", Desc="重新读取所有字段", OnInvoke = ()=> { RefreshCapturedValues(true); RebuildCurrentDetailPage(); } });
            list.Add(new ActionCardModel{ Id="imk.item.rollback", Title="Rollback (Prefer Tx)", Desc="优先事务回滚，否则使用快照回滚", OnInvoke = ()=> { RollbackPreferTransaction(); RebuildCurrentDetailPage(); } });
            return list;
        }

        public static void CommitTransactionAndFlush()
        {
            try
            {
                if (CapturedItem == null) return;
                bool committed = false;
                if (!string.IsNullOrEmpty(_txToken))
                {
                    var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov");
                    var write = duck?.GetProperty("Write", BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                    var m = write?.GetType().GetMethod("CommitTransaction", new Type[]{ typeof(object), typeof(string) });
                    var rr = m?.Invoke(write, new object[]{ CapturedItem, _txToken });
                    bool ok = false; try { var okProp = rr?.GetType().GetProperty("Ok"); if (okProp!=null) ok = (bool)okProp.GetValue(rr); } catch { }
                    if (ok) { committed = true; _txToken = null; }
                }
                // Flush dirty if available
                try
                {
                    var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov");
                    var flush = duck?.GetMethod("FlushDirty", BindingFlags.Public|BindingFlags.Static);
                    flush?.Invoke(null, new object[]{ CapturedItem, false });
                }
                catch { }
                if (committed) { RefreshCapturedValues(true); }
            }
            catch { }
        }

        private static bool TryWriteCoreField(string id, object value)
        {
            try
            {
                var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return false;
                var writeProp = duck.GetProperty("Write", BindingFlags.Public|BindingFlags.Static); if (writeProp == null) return false;
                var writeSvc = writeProp.GetValue(null);
                var changesType = GetDuckovType("ItemModKit.Core.CoreFieldChanges"); if (changesType == null) return false;
                var changes = Activator.CreateInstance(changesType);
                // map id -> property on CoreFieldChanges
                void Set(string prop, object v)
                {
                    var pi = changesType.GetProperty(prop, BindingFlags.Public|BindingFlags.Instance);
                    if (pi != null)
                    {
                        try
                        {
                            object vv = v;
                            var pt = pi.PropertyType;
                            if (pt == typeof(int?) || pt == typeof(int)) vv = Convert.ToInt32(v);
                            else if (pt == typeof(string)) vv = v?.ToString();
                            pi.SetValue(changes, vv);
                        }
                        catch { }
                    }
                }
                switch (id)
                {
                    case "Name": Set("Name", value); break;
                    case "RawName": Set("RawName", value); break;
                    case "TypeId": Set("TypeId", value); break;
                    case "Quality": Set("Quality", value); break;
                    case "DisplayQuality": Set("DisplayQuality", value); break;
                    case "Value": Set("Value", value); break;
                    default: return false;
                }
                var method = writeSvc.GetType().GetMethod("TryWriteCoreFields", new Type[]{ typeof(object), changesType });
                if (method == null) return false;
                var rr = method.Invoke(writeSvc, new object[]{ CapturedItem, changes });
                bool ok = false;
                try { var okProp = rr.GetType().GetProperty("Ok"); if (okProp != null) ok = (bool)okProp.GetValue(rr); } catch { }
                if (ok)
                {
                    // MarkDirty(item, DirtyKind.Core)
                    try
                    {
                        var dirtyKind = GetDuckovType("ItemModKit.Core.DirtyKind");
                        var coreVal = Enum.Parse(dirtyKind, "Core");
                        var mark = duck.GetMethod("MarkDirty", BindingFlags.Public|BindingFlags.Static);
                        mark?.Invoke(null, new object[]{ CapturedItem, coreVal, false });
                    }
                    catch { }
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public static void RefreshCapturedValues(bool rebuildSnapshot = false)
        {
            if (CapturedItem==null) return; var t = CapturedItem.GetType();
            var members = new List<MemberInfo>(); members.AddRange(t.GetProperties(BindingFlags.Public|BindingFlags.Instance)); members.AddRange(t.GetFields(BindingFlags.Public|BindingFlags.Instance));
            foreach (var m in members)
            {
                string id = m.Name; object val = null; Type vt = null; bool readable=false;
                try
                {
                    if (m is PropertyInfo p && p.CanRead) { vt = p.PropertyType; if (IsPrimitiveSupported(vt)) { val = p.GetValue(CapturedItem); readable=true; } }
                    else if (m is FieldInfo f && f.IsPublic) { vt = f.FieldType; if (IsPrimitiveSupported(vt)) { val = f.GetValue(CapturedItem); readable=true; } }
                }
                catch { }
                if (readable) _currentValues[id] = val;
            }
            if (rebuildSnapshot) { BuildOriginalSnapshot(); CaptureCoreSnapshot(); }
        }

        public static void RollbackPreferTransaction()
        {
            if (CapturedItem==null) return;
            bool usedTx = false;
            try
            {
                if (!string.IsNullOrEmpty(_txToken))
                {
                    var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov");
                    var write = duck?.GetProperty("Write", BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                    var m = write?.GetType().GetMethod("RollbackTransaction", new Type[]{ typeof(object), typeof(string) });
                    var rr = m?.Invoke(write, new object[]{ CapturedItem, _txToken });
                    _txToken = null; usedTx = true;
                }
            }
            catch { }
            if (!usedTx)
            {
                RollbackToOriginal();
            }
            else
            {
                RefreshCapturedValues(true);
            }
        }

        public static void RollbackToOriginal()
        {
            if (CapturedItem==null) return;
            // First rollback core via SnapshotHelper to honor WriteService semantics
            try
            {
                if (_originalCoreFields != null)
                {
                    var duck = GetDuckovType("ItemModKit.Adapters.Duckov.IMKDuckov");
                    var write = duck?.GetProperty("Write", BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                    var helper = GetDuckovType("ItemModKit.Core.SnapshotHelper");
                    var rb = helper?.GetMethod("RollbackCore", BindingFlags.Public|BindingFlags.Static);
                    var rr = rb?.Invoke(null, new object[]{ write, CapturedItem, _originalCoreFields });
                }
            }
            catch { }
            // Then rollback other primitive fields
            var t = CapturedItem.GetType();
            var members = new List<MemberInfo>(); members.AddRange(t.GetProperties(BindingFlags.Public|BindingFlags.Instance)); members.AddRange(t.GetFields(BindingFlags.Public|BindingFlags.Instance));
            foreach (var m in members)
            {
                string id = m.Name; if (!_originalSnapshot.TryGetValue(id, out var orig)) continue;
                if (s_coreIds.Contains(id)) continue; // already handled via write service
                try
                {
                    if (m is PropertyInfo p && p.CanRead && p.CanWrite && p.SetMethod!=null && p.SetMethod.IsPublic && IsPrimitiveSupported(p.PropertyType)) p.SetValue(CapturedItem, ConvertValue(orig, p.PropertyType));
                    else if (m is FieldInfo f && !f.IsInitOnly && !f.IsLiteral && f.IsPublic && IsPrimitiveSupported(f.FieldType)) f.SetValue(CapturedItem, ConvertValue(orig, f.FieldType));
                }
                catch { }
            }
            RefreshCapturedValues();
        }

        private static void RebuildCurrentDetailPage()
        {
            try
            {
                var presenterGo = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window/Content/ContentInset");
                var presenter = presenterGo != null ? presenterGo.GetComponent<IMK.SettingsUI.Navigation.ContentPresenter>() : null;
                if (presenter == null) return;
                var models = BuildItemDetailCards();
                presenter.SetWithTransition(models, false);
            }
            catch { }
        }

        private static bool IsPrimitiveSupported(Type t)
        {
            if (t==typeof(string) || t==typeof(int) || t==typeof(float) || t==typeof(double) || t==typeof(bool) || t==typeof(long) || t==typeof(short) || t==typeof(byte)) return true;
            if (t.IsEnum) return true; return false;
        }
        private static object ConvertValue(object v, Type target)
        {
            if (v == null) return target.IsValueType? Activator.CreateInstance(target) : null;
            try
            {
                if (target.IsEnum) return Enum.Parse(target, v.ToString(), true);
                if (target == typeof(string)) return v.ToString();
                if (target == typeof(int) || target == typeof(int?)) return Convert.ToInt32(v);
                if (target == typeof(float) || target == typeof(float?)) return Convert.ToSingle(v);
                if (target == typeof(double) || target == typeof(double?)) return Convert.ToDouble(v);
                if (target == typeof(bool) || target == typeof(bool?)) return Convert.ToBoolean(v);
                if (target == typeof(long) || target == typeof(long?)) return Convert.ToInt64(v);
                if (target == typeof(short) || target == typeof(short?)) return Convert.ToInt16(v);
                if (target == typeof(byte) || target == typeof(byte?)) return Convert.ToByte(v);
                return v;
            }
            catch { return v; }
        }

        private static void NavigateProgrammatic(string id)
        {
            try
            {
                IMK.SettingsUI.PublicApi.EnsureInitialized();
                var nav = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window")?.GetComponent<IMK.SettingsUI.Navigation.NavController>();
                if (nav != null) nav.NavigateTo(id);
            }
            catch { }
        }
    }
}
