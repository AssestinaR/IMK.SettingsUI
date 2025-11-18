using System;
using System.Collections.Generic;
using System.Reflection;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Table;
using UnityEngine;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal sealed class SlotsSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols;
        public SlotsSchema()
        {
            _cols = new List<TableColumn>
            {
                new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=true, WidthHint=160f },
                new TableColumn{ Id="occupied", Title="Occupied", Kind=TableCellKind.Toggle, ValueType=typeof(bool), ReadOnly=true, WidthHint=80f },
                new TableColumn{ Id="type", Title="PlugType", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=true, WidthHint=200f },
            };
        }
        public System.Collections.Generic.IReadOnlyList<TableColumn> Columns => _cols;
    }

    internal sealed class SlotsDataSet : ITableDataSet
    {
        private readonly List<Row> _rows = new();
        private bool _dirty;
        private object _item => ItemModKitPanelState.CapturedItem;
        public SlotsDataSet() { Reload(); }
        public int Count => _rows.Count;
        public bool IsDirty => _dirty;
        public IRowAdapter GetRow(int index) => new SlotsRowAdapter(_rows[index]);
        public bool AddNew()
        {
            try
            {
                if (_item == null) { Debug.LogWarning("[IMK.Slots] AddNew aborted: captured item null"); return false; }
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) { Debug.LogWarning("[IMK.Slots] AddNew IMKDuckov type not found"); return false; }
                var write = GetStaticMember(duck, "Write"); if (write == null) { Debug.LogWarning("[IMK.Slots] AddNew Write service null"); return false; }
                var optionsType = FindType("ItemModKit.Core.SlotCreateOptions"); if (optionsType == null) { Debug.LogWarning("[IMK.Slots] AddNew SlotCreateOptions type not found"); return false; }
                // build unique base key
                string baseKey = "Socket"; int suffix = 1; var existing = new HashSet<string>(_rows.ConvertAll(r => r.key)); string candidate = baseKey;
                while (existing.Contains(candidate)) candidate = baseKey + (++suffix).ToString();
                var opt = Activator.CreateInstance(optionsType);
                optionsType.GetProperty("Key")?.SetValue(opt, candidate);
                optionsType.GetProperty("DisplayName")?.SetValue(opt, candidate);
                var m = write.GetType().GetMethod("TryAddSlot", new[] { typeof(object), optionsType });
                object rr = null; bool ok = false; string err = null;
                if (m != null)
                {
                    rr = m.Invoke(write, new object[] { _item, opt });
                    ok = Convert.ToBoolean(rr?.GetType().GetProperty("Ok")?.GetValue(rr) ?? false);
                    if (!ok) err = rr?.GetType().GetProperty("Error")?.GetValue(rr)?.ToString();
                }
                if (!ok && err == "Slot type missing")
                {
                    Debug.LogWarning("[IMK.Slots] TryAddSlot failed with 'Slot type missing'. Attempting fallback manual creation.");
                    if (FallbackManualAddSlot(candidate)) { Reload(); Debug.Log("[IMK.Slots] Fallback manual slot add success key=" + candidate + " total=" + _rows.Count); return true; }
                    Debug.LogWarning("[IMK.Slots] Fallback manual slot add failed"); return false;
                }
                if (!ok)
                {
                    Debug.LogWarning("[IMK.Slots] AddNew failed Ok=false err=" + err);
                    return false;
                }
                // flush
                try
                {
                    var flush = duck.GetMethod("FlushDirty", BindingFlags.Public | BindingFlags.Static);
                    flush?.Invoke(null, new object[] { _item, false });
                }
                catch { }
                Reload();
                Debug.Log("[IMK.Slots] AddNew success key=" + candidate + " total=" + _rows.Count);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[IMK.Slots] AddNew exception: " + ex.Message);
                return false;
            }
        }
        private bool FallbackManualAddSlot(string keyDesired)
        {
            try
            {
                var item = _item; if (item == null) return false;
                // get slots collection via reflection
                var slotsObj = GetSlotsCollection(item); if (slotsObj == null) { Debug.LogWarning("[IMK.Slots.Fallback] owner has no Slots collection"); return false; }
                // discover slot type: prefer existing element type if any
                Type slotType = null; foreach (var existing in EnumerateEnumerable(slotsObj)) { if (existing != null) { slotType = existing.GetType(); break; } }
                if (slotType == null)
                {
                    slotType = FindType("ItemStatsSystem.Slot") ?? FindType("Slot");
                    if (slotType == null)
                    {
                        // brute force search by name suffix and Key property
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            Type[] types;
                            try { types = asm.GetTypes(); } catch { continue; }
                            foreach (var t in types)
                            {
                                if (!t.Name.EndsWith("Slot", StringComparison.Ordinal)) continue;
                                if (t.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance) != null) { slotType = t; break; }
                            }
                            if (slotType != null) break;
                        }
                    }
                }
                if (slotType == null) { Debug.LogWarning("[IMK.Slots.Fallback] slotType discovery failed"); return false; }
                // ensure unique key using existing collection
                string finalKey = EnsureUniqueKey(slotsObj, keyDesired);
                var slot = Activator.CreateInstance(slotType); if (slot == null) { Debug.LogWarning("[IMK.Slots.Fallback] create instance failed"); return false; }
                SetProp(slot, "Key", finalKey);
                SetProp(slot, "DisplayName", finalKey);
                // Initialize(slotsCollection)
                var init = slotType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); try { init?.Invoke(slot, new[] { slotsObj }); } catch { }
                // Add to collection
                var add = slotsObj.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (add == null) { Debug.LogWarning("[IMK.Slots.Fallback] collection Add not found"); return false; }
                add.Invoke(slotsObj, new[] { slot });
                // Mark dirty
                try
                {
                    var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var dirtyKind = FindType("ItemModKit.Core.DirtyKind"); var kindVal = Enum.Parse(dirtyKind, "Slots"); var mark = duck.GetMethod("MarkDirty", BindingFlags.Public | BindingFlags.Static); mark?.Invoke(null, new object[] { item, kindVal, false });
                    var flush = duck.GetMethod("FlushDirty", BindingFlags.Public | BindingFlags.Static); flush?.Invoke(null, new object[] { item, false });
                }
                catch { }
                return true;
            }
            catch (System.Exception ex) { Debug.LogWarning("[IMK.Slots.Fallback] exception: " + ex.Message); return false; }
        }
        private static object GetSlotsCollection(object item)
        {
            if (item == null) return null; var t = item.GetType();
            // search property first
            var p = t.GetProperty("Slots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) { try { return p.GetValue(item); } catch { } }
            // then field
            var f = t.GetField("Slots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { try { return f.GetValue(item); } catch { } }
            return null;
        }
        private static object DuckovReflectionCacheGet(Type ownerType, string name)
        {
            try
            {
                var instField = ownerType.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (instField != null) return instField.GetValue(null); // unlikely for instance field; will be adjusted later
                var instProp = ownerType.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // need instance to read; create dummy not possible; will resolve via captured item instead
                return null;
            }
            catch { return null; }
        }
        private static IEnumerable<object> EnumerateEnumerable(object collection) { if (collection == null) yield break; var en = collection as System.Collections.IEnumerable; if (en == null) yield break; foreach (var o in en) yield return o; }
        private static string EnsureUniqueKey(object slotsObj, string desired)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in EnumerateEnumerable(slotsObj))
            {
                if (s == null) continue; var k = s.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance)?.GetValue(s)?.ToString(); if (!string.IsNullOrEmpty(k)) set.Add(k);
            }
            if (!set.Contains(desired)) return desired;
            int i = 2; string cand = desired + "2"; while (set.Contains(cand)) { cand = desired + (++i).ToString(); }
            return cand;
        }
        private static void SetProp(object obj, string prop, object val) { if (obj == null || prop == null) return; try { var pi = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); pi?.SetValue(obj, val); } catch { } }
        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _rows.Count) return false;
            var row = _rows[index];
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return false;
                var write = GetStaticMember(duck, "Write"); if (write == null) return false;
                var m = write.GetType().GetMethod("TryRemoveSlot");
                if (m != null && _item != null && !string.IsNullOrEmpty(row.key))
                {
                    var rr = m.Invoke(write, new object[] { _item, row.key });
                    bool ok = Convert.ToBoolean(rr?.GetType().GetProperty("Ok")?.GetValue(rr) ?? false);
                    if (ok) { Reload(); return true; }
                }
            }
            catch { }
            return false;
        }
        public bool Move(int from, int to)
        {
            if (from < 0 || to < 0 || from >= _rows.Count || to >= _rows.Count) return false;
            if (from == to) return true;
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return false;
                var write = GetStaticMember(duck, "Write"); if (write == null) return false;
                var fromRow = _rows[from]; var toRow = _rows[to];
                var mMove = write.GetType().GetMethod("TryMoveBetweenSlots");
                if (mMove != null && _item != null && !string.IsNullOrEmpty(fromRow.key) && !string.IsNullOrEmpty(toRow.key))
                {
                    var rr = mMove.Invoke(write, new object[] { _item, fromRow.key, toRow.key });
                    bool ok = Convert.ToBoolean(rr?.GetType().GetProperty("Ok")?.GetValue(rr) ?? false);
                    if (ok) { Reload(); return true; }
                }
            }
            catch { }
            return false;
        }
        public bool Commit() { return false; }
        public bool Reload()
        {
            try
            {
                _rows.Clear();
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null) return false;
                var read = GetStaticMember(duck, "Read"); if (read == null) return false;
                var item = _item; if (item == null) return false;
                var m = read.GetType().GetMethod("TryReadSlots"); var rr = m?.Invoke(read, new object[] { item });
                if (rr == null) return false;
                bool ok = Convert.ToBoolean(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); if (!ok) return false;
                var payload = rr.GetType().GetProperty("Value")?.GetValue(rr) as System.Collections.IEnumerable; if (payload == null) return false;
                foreach (var e in payload)
                {
                    string key = Convert.ToString(GetMaybe(e, new[] { "Key", "key", "Name", "name" })) ?? string.Empty;
                    bool occ = Convert.ToBoolean(GetMaybe(e, new[] { "Occupied", "occupied" }) ?? false);
                    string type = Convert.ToString(GetMaybe(e, new[] { "PlugType", "Type", "ExpectedType" })) ?? string.Empty;
                    _rows.Add(new Row { key = key, occupied = occ, type = type });
                }
                _dirty = false; return true;
            }
            catch { return false; }
        }
        private static object GetMaybe(object obj, string[] names)
        {
            if (obj == null) return null; var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) { try { return p.GetValue(obj); } catch { } }
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { try { return f.GetValue(obj); } catch { } }
            }
            return null;
        }
        private static Type FindType(string fullName)
        {
            var t = Type.GetType(fullName); if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(fullName); if (t != null) return t; } catch { }
            }
            return null;
        }
        private static object GetStaticMember(Type t, string name)
        {
            try
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Static); if (p != null) return p.GetValue(null);
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.Static); if (f != null) return f.GetValue(null);
            }
            catch { }
            return null;
        }
        internal sealed class Row { public string key; public bool occupied; public string type; }
        private sealed class SlotsRowAdapter : IRowAdapter
        {
            private readonly Row _r; public SlotsRowAdapter(Row r) { _r = r; }
            public object Get(string columnId) => columnId switch
            {
                "key" => _r.key,
                "occupied" => _r.occupied,
                "type" => _r.type,
                _ => null
            };
            public bool Set(string columnId, object value) { return false; }
        }
    }

    internal static class SlotsPageAugment
    {
        public static void InjectActions(List<ICardModel> list, SlotsDataSet data)
        {
            list.Add(new ActionCardModel { Id = "imk.slots.add", Title = "Add Slot", Desc = "新增插槽", OnInvoke = () => data.AddNew() });
            list.Add(new ActionCardModel { Id = "imk.slots.reload", Title = "Reload Slots", Desc = "重新读取", OnInvoke = () => data.Reload() });
        }
    }
}
