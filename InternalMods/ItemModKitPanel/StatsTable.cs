using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal sealed class StatsSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols;
        public StatsSchema()
        {
            _cols = new List<TableColumn>
            {
                new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=160f },
                new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Number, ValueType=typeof(float), ReadOnly=false, WidthHint=100f, Min = -999999f, Max = 999999f },
                new TableColumn{ Id="remove", Title="Remove", Kind=TableCellKind.Toggle, ValueType=typeof(bool), ReadOnly=false, WidthHint=70f },
            };
        }
        public IReadOnlyList<TableColumn> Columns => _cols;
    }

    internal sealed class StatsDataSet : ITableDataSet
    {
        private readonly List<Row> _rows = new List<Row>();
        private readonly Dictionary<string, float> _original = new Dictionary<string, float>(StringComparer.Ordinal);
        private bool _dirty;
        private object _item => ItemModKitPanelState.CapturedItem;
        public StatsDataSet(){ Reload(); }
        public int Count => _rows.Count;
        public bool IsDirty => _dirty;
        public IRowAdapter GetRow(int index) => new StatsRowAdapter(this, _rows[index]);
        public bool AddNew(){ _rows.Add(new Row{ key = UniqueKey("NewStat"), value = 0f, remove = false }); _dirty = true; return true; }
        public bool RemoveAt(int index){ if(index<0||index>=_rows.Count) return false; _rows.RemoveAt(index); _dirty = true; return true; }
        public bool Move(int from, int to){ if(from<0||from>=_rows.Count||to<0||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); _dirty=true; return true; }
        public bool Commit()
        {
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ Log("Stats.Commit IMKDuckov type not found"); return false; }
                var write = GetStaticMember(duck, "Write"); if (write == null){ Log("Stats.Commit Write is null"); return false; }
                var mSet = write.GetType().GetMethod("TrySetStatValue");
                var mEnsure = write.GetType().GetMethod("TryEnsureStat");
                var mRemove = write.GetType().GetMethod("TryRemoveStat");
                object item = _item; if (item == null){ Log("Stats.Commit item null"); return false; }
                var currentKeys = new HashSet<string>(_rows.Select(r=> r.key).Where(k=> !string.IsNullOrEmpty(k)), StringComparer.Ordinal);
                // remove keys that were in original but no longer exist or explicitly marked
                foreach (var kv in _original)
                {
                    if (!currentKeys.Contains(kv.Key))
                    {
                        if (mRemove != null) mRemove.Invoke(write, new object[]{ item, kv.Key });
                    }
                }
                foreach (var r in _rows)
                {
                    if (string.IsNullOrEmpty(r.key)) continue;
                    if (r.remove)
                    {
                        if (mRemove != null) mRemove.Invoke(write, new object[]{ item, r.key });
                        continue;
                    }
                    if (!_original.ContainsKey(r.key))
                    {
                        if (mEnsure != null) mEnsure.Invoke(write, new object[]{ item, r.key, r.value });
                    }
                    else
                    {
                        float old = _original[r.key]; if (Math.Abs(old - r.value) > 0.0001f)
                        {
                            if (mSet != null) mSet.Invoke(write, new object[]{ item, r.key, r.value });
                        }
                    }
                }
                // Mark dirty & flush
                try { var dirtyKind = FindType("ItemModKit.Core.DirtyKind"); var statsVal = Enum.Parse(dirtyKind, "Stats"); var mark = duck.GetMethod("MarkDirty", BindingFlags.Public|BindingFlags.Static); mark?.Invoke(null, new object[]{ item, statsVal, false }); } catch { }
                try { var flush = duck.GetMethod("FlushDirty", BindingFlags.Public|BindingFlags.Static); flush?.Invoke(null, new object[]{ item, false }); } catch { }
                // refresh originals
                Reload(); _dirty=false; return true;
            }
            catch(System.Exception ex){ Log("Stats.Commit exception: "+ex.Message); return false; }
        }
        public bool Reload()
        {
            try
            {
                _rows.Clear(); _original.Clear();
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ Log("Stats.Reload IMKDuckov type not found"); return false; }
                var read = GetStaticMember(duck, "Read"); if (read == null){ Log("Stats.Reload Read is null"); return false; }
                object item = _item; if (item == null){ Log("Stats.Reload item null"); return false; }
                var m = read.GetType().GetMethod("TryReadStats"); var rr = m?.Invoke(read, new object[]{ item });
                if (rr == null){ Log("Stats.Reload TryReadStats returned null"); return false; }
                bool ok = Convert.ToBoolean(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); if (!ok){ Log("Stats.Reload result.Ok=false"); return false; }
                var valueProp = rr.GetType().GetProperty("Value");
                var payload = valueProp != null ? valueProp.GetValue(rr) : null;
                if (payload == null){ Log("Stats.Reload value=null"); return false; }
                // payload is StatsSnapshot.Entries or raw array depending on impl
                System.Collections.IEnumerable entries = null;
                var snapEntries = payload.GetType().GetProperty("Entries");
                if (snapEntries != null) entries = snapEntries.GetValue(payload) as System.Collections.IEnumerable;
                else entries = payload as System.Collections.IEnumerable;
                if (entries == null){ Log("Stats.Reload entries=null"); return false; }
                int count=0;
                foreach (var e in entries)
                {
                    string key = Convert.ToString(GetMaybe(e, new[]{"Key","key","Name","name"})) ?? string.Empty;
                    float val = ConvertToFloat(GetMaybe(e, new[]{"Value","value"}));
                    _rows.Add(new Row{ key = key, value = val, remove=false });
                    if (!string.IsNullOrEmpty(key)) _original[key] = val;
                    count++;
                }
                Log("Stats.Reload rows="+count);
                _dirty=false; return true;
            }
            catch(System.Exception ex){ Log("Stats.Reload exception: "+ex.Message); return false; }
        }
        private static void Log(string msg){ if (IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled) UnityEngine.Debug.Log("[IMK.StatsTable] "+msg); }
        private static Type FindType(string fullName)
        {
            var t = System.Type.GetType(fullName); if (t != null) return t;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(fullName); if (t != null) return t; } catch { }
            }
            return null;
        }
        private static object GetStaticMember(Type t, string name)
        {
            try
            {
                var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.Static); if (p!=null) return p.GetValue(null);
                var f = t.GetField(name, BindingFlags.Public|BindingFlags.Static); if (f!=null) return f.GetValue(null);
            }
            catch { }
            return null;
        }
        private static object GetMaybe(object obj, string[] names)
        {
            if (obj == null) return null; var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if (p!=null) { try { return p.GetValue(obj); } catch { } }
                var f = t.GetField(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if (f!=null) { try { return f.GetValue(obj); } catch { } }
            }
            return null;
        }
        private static float ConvertToFloat(object v)
        {
            try { if (v == null) return 0f; if (v is float f) return f; if (v is double d) return (float)d; if (v is int i) return i; if (v is long l) return l; if (v is string s && float.TryParse(s, out var pf)) return pf; return System.Convert.ToSingle(v); } catch { return 0f; }
        }
        private string UniqueKey(string baseKey)
        {
            string k = baseKey; int i=1; var set = new HashSet<string>(_rows.Select(r=> r.key), StringComparer.Ordinal);
            while (set.Contains(k)) { k = baseKey + (++i).ToString(); }
            return k;
        }
        internal void OnChanged(){ _dirty=true; }
        internal sealed class Row
        {
            public string key;
            public float value;
            public bool remove;
        }
        private sealed class StatsRowAdapter : IRowAdapter
        {
            private readonly StatsDataSet _ds; private readonly Row _r;
            public StatsRowAdapter(StatsDataSet ds, Row r){ _ds=ds; _r=r; }
            public object Get(string columnId)
            {
                switch(columnId){ case "key": return _r.key; case "value": return _r.value; case "remove": return _r.remove; default: return null; }
            }
            public bool Set(string columnId, object value)
            {
                switch(columnId)
                {
                    case "key": _r.key = value?.ToString() ?? string.Empty; _ds.OnChanged(); return true;
                    case "value":
                        try { if (value is float f) _r.value=f; else if (value is double d) _r.value=(float)d; else if (value is int i) _r.value=i; else if (value is string s) { float pf; if(float.TryParse(s, out pf)) _r.value = pf; } else _r.value = System.Convert.ToSingle(value); } catch { }
                        _ds.OnChanged(); return true;
                    case "remove": _r.remove = value is bool b ? b : (_r.remove); _ds.OnChanged(); return true;
                    default: return false;
                }
            }
        }
    }
}
