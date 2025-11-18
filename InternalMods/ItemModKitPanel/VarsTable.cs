using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal static class VarTableCommon
    {
        internal static void Log(string msg){ if (IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled) UnityEngine.Debug.Log("[IMK.VarsTable] "+msg); }
        internal static Type FindType(string fullName)
        {
            var t = Type.GetType(fullName); if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(fullName); if (t != null) return t; } catch { }
            }
            return null;
        }
        internal static object GetStaticMember(Type t, string name)
        {
            try
            {
                var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.Static); if (p!=null) return p.GetValue(null);
                var f = t.GetField(name, BindingFlags.Public|BindingFlags.Static); if (f!=null) return f.GetValue(null);
            }
            catch { }
            return null;
        }
        internal static object GetMaybe(object obj, string[] names)
        {
            if (obj == null) return null; var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if (p!=null) { try { return p.GetValue(obj); } catch { } }
                var f = t.GetField(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if (f!=null) { try { return f.GetValue(obj); } catch { } }
            }
            return null;
        }
        internal static string InferTypeName(object v)
        {
            if (v == null) return "string";
            var t = v.GetType();
            if (t==typeof(string)) return "string";
            if (t==typeof(int) || t==typeof(uint)) return "int";
            if (t==typeof(float)) return "float";
            if (t==typeof(double)) return "double";
            if (t==typeof(bool)) return "bool";
            if (t==typeof(long) || t==typeof(ulong)) return "long";
            if (t==typeof(short) || t==typeof(ushort)) return "short";
            if (t==typeof(byte) || t==typeof(sbyte)) return "byte";
            return "string";
        }
        internal static object ParseToType(string s, string type)
        {
            try
            {
                switch(type)
                {
                    case "int": return int.TryParse(s, out var i) ? i : 0;
                    case "float": return float.TryParse(s, out var f) ? f : 0f;
                    case "double": return double.TryParse(s, out var d) ? d : 0.0;
                    case "bool": return bool.TryParse(s, out var b) ? b : false;
                    case "long": return long.TryParse(s, out var l) ? l : 0L;
                    case "short": return short.TryParse(s, out var sh) ? sh : (short)0;
                    case "byte": return byte.TryParse(s, out var by) ? by : (byte)0;
                    default: return s ?? string.Empty;
                }
            }
            catch { return s; }
        }
    }

    internal sealed class VarsSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols;
        public VarsSchema()
        {
            _cols = new List<TableColumn>
            {
                new TableColumn{ Id="scope", Title="Scope", Kind=TableCellKind.Dropdown, Options=new[]{"Var","Const","Tag"}, ReadOnly=false, WidthHint=70f },
                new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=140f },
                new TableColumn{ Id="type", Title="Type", Kind=TableCellKind.Dropdown, Options=new[]{"string","int","float","double","bool","long","short","byte"}, ReadOnly=false, WidthHint=90f },
                new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=200f },
            };
        }
        public IReadOnlyList<TableColumn> Columns => _cols;
    }

    // New: Variables-only schema
    internal sealed class VariablesOnlySchema : ITableSchema
    {
        private readonly List<TableColumn> _cols = new List<TableColumn>
        {
            new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=160f },
            new TableColumn{ Id="type", Title="Type", Kind=TableCellKind.Dropdown, Options=new[]{"string","int","float","double","bool","long","short","byte"}, ReadOnly=false, WidthHint=100f },
            new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=220f },
        };
        public IReadOnlyList<TableColumn> Columns => _cols;
    }
    // New: Constants-only schema
    internal sealed class ConstantsSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols = new List<TableColumn>
        {
            new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=160f },
            new TableColumn{ Id="type", Title="Type", Kind=TableCellKind.Dropdown, Options=new[]{"string","int","float","double","bool","long","short","byte"}, ReadOnly=false, WidthHint=100f },
            new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=220f },
        };
        public IReadOnlyList<TableColumn> Columns => _cols;
    }
    // New: Tags-only schema
    internal sealed class TagsSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols = new List<TableColumn>
        {
            new TableColumn{ Id="key", Title="Tag", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=220f },
        };
        public IReadOnlyList<TableColumn> Columns => _cols;
    }

    internal sealed class VarsDataSet : ITableDataSet
    {
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<Row> _original = new List<Row>();
        private bool _dirty;
        private object _item => ItemModKitPanelState.CapturedItem;
        public VarsDataSet(){ Reload(); }
        public int Count => _rows.Count;
        public bool IsDirty => _dirty;
        public IRowAdapter GetRow(int index) => new VarsRowAdapter(this, _rows[index]);
        public bool AddNew(){ var key = UniqueKey("NewKey"); _rows.Add(new Row{ scope="Var", key=key, type="string", value="" }); _dirty=true; return true; }
        private string UniqueKey(string baseKey){ string k=baseKey; int i=1; var set = new HashSet<string>(_rows.Select(r=> r.key), StringComparer.Ordinal); while (set.Contains(k)) k = baseKey + (++i).ToString(); return k; }
        public bool RemoveAt(int index)
        {
            if (index<0 || index>=_rows.Count) return false;
            _rows.RemoveAt(index); _dirty = true; return true;
        }
        public bool Move(int from, int to){ if (from<0||from>=_rows.Count||to<0||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); _dirty=true; return true; }
        public bool Commit()
        {
            try
            {
                var duck = VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ VarTableCommon.Log("Vars.Commit IMKDuckov type not found"); return false; }
                var write = VarTableCommon.GetStaticMember(duck, "Write"); if (write == null){ VarTableCommon.Log("Vars.Commit Write is null"); return false; }
                var vars = new Dictionary<string, object>();
                var consts = new Dictionary<string, object>();
                var tags = new List<string>();
                var originalKeys = new HashSet<string>(_original.Select(o=> o.key), StringComparer.Ordinal);
                var newVarKeys = new List<string>();
                foreach (var r in _rows)
                {
                    if (string.IsNullOrEmpty(r.key)) continue; // skip empty key rows
                    if (r.scope=="Tag") { tags.Add(r.key); continue; }
                    var parsed = VarTableCommon.ParseToType(r.value, r.type);
                    if (r.scope=="Const") consts[r.key] = parsed; else { vars[r.key] = parsed; if (!originalKeys.Contains(r.key)) newVarKeys.Add(r.key); }
                }
                object item = _item; if (item == null){ VarTableCommon.Log("Vars.Commit item null"); return false; }
                var mVars = write.GetType().GetMethod("TryWriteVariables");
                var mConsts = write.GetType().GetMethod("TryWriteConstants");
                var mTags = write.GetType().GetMethod("TryWriteTags");
                var mMetaVar = write.GetType().GetMethod("TrySetVariableMeta");
                var mMetaConst = write.GetType().GetMethod("TrySetConstantMeta");
                if (vars.Count>0 && mVars!=null) mVars.Invoke(write, new object[]{ item, vars, true });
                if (consts.Count>0 && mConsts!=null) mConsts.Invoke(write, new object[]{ item, consts, true });
                if (mTags!=null) mTags.Invoke(write, new object[]{ item, tags.ToArray(), false });
                // ensure new variables are marked display=true so they appear in read snapshots
                foreach (var k in newVarKeys)
                {
                    try { mMetaVar?.Invoke(write, new object[]{ item, k, true, null, null }); } catch { }
                }
                // dirty + flush
                try { var dirtyKind = VarTableCommon.FindType("ItemModKit.Core.DirtyKind"); var coreVal = Enum.Parse(dirtyKind, "Variables"); var mark = duck.GetMethod("MarkDirty", BindingFlags.Public|BindingFlags.Static); mark?.Invoke(null, new object[]{ item, coreVal, false }); } catch { }
                try { var flush = duck.GetMethod("FlushDirty", BindingFlags.Public|BindingFlags.Static); flush?.Invoke(null, new object[]{ item, false }); } catch { }
                _dirty=false; _original.Clear(); _original.AddRange(_rows.Select(x=> x.Clone()));
                VarTableCommon.Log($"Vars.Commit ok rows={_rows.Count} newVars={newVarKeys.Count}");
                return true;
            }
            catch(System.Exception ex){ VarTableCommon.Log("Vars.Commit exception: "+ex.Message); return false; }
        }
        public bool Reload()
        {
            try
            {
                _rows.Clear();
                var duck = VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ VarTableCommon.Log("Vars.Reload IMKDuckov type not found"); return false; }
                var read = VarTableCommon.GetStaticMember(duck, "Read"); if (read == null){ VarTableCommon.Log("Vars.Reload Read is null"); return false; }
                object item = _item; if (item == null){ VarTableCommon.Log("Vars.Reload item null"); return false; }
                var rv = read.GetType().GetMethod("TryReadVariables"); var rr = rv?.Invoke(read, new object[]{ item }); AppendVarEntries(rr, "Var");
                var rc = read.GetType().GetMethod("TryReadConstants"); var rcRes = rc?.Invoke(read, new object[]{ item }); AppendVarEntries(rcRes, "Const");
                var rt = read.GetType().GetMethod("TryReadTags"); var rtRes = rt?.Invoke(read, new object[]{ item }); AppendTagEntries(rtRes);
                _original.Clear(); _original.AddRange(_rows.Select(x=> x.Clone())); _dirty=false; VarTableCommon.Log("Vars.Reload rows="+_rows.Count); return true;
            }
            catch(System.Exception ex){ VarTableCommon.Log("Vars.Reload exception: "+ex.Message); return false; }
        }
        private void AppendVarEntries(object richResult, string scope)
        {
            if (richResult == null){ VarTableCommon.Log($"Vars.AppendVarEntries {scope}: result null"); return; }
            try
            {
                var ok = Convert.ToBoolean(richResult.GetType().GetProperty("Ok")?.GetValue(richResult) ?? false);
                if (!ok){ VarTableCommon.Log($"Vars.AppendVarEntries {scope}: Ok=false"); return; }
                var valueProp = richResult.GetType().GetProperty("Value");
                var payload = valueProp!=null ? valueProp.GetValue(richResult) as System.Collections.IEnumerable : null;
                if (payload == null){ VarTableCommon.Log($"Vars.AppendVarEntries {scope}: value null"); return; }
                int count=0;
                foreach (var e in payload)
                {
                    string key = Convert.ToString(VarTableCommon.GetMaybe(e, new[]{"Key","key","Name","name"}));
                    var valObj = VarTableCommon.GetMaybe(e, new[]{"Value","value"});
                    string type = VarTableCommon.InferTypeName(valObj);
                    string valueStr = valObj?.ToString() ?? string.Empty;
                    _rows.Add(new Row{ scope=scope, key=key, type=type, value=valueStr }); count++;
                }
                VarTableCommon.Log($"Vars.AppendVarEntries {scope}: +{count}");
            }
            catch(System.Exception ex){ VarTableCommon.Log($"Vars.AppendVarEntries {scope}: exception {ex.Message}"); }
        }
        private void AppendTagEntries(object richResult)
        {
            if (richResult == null){ VarTableCommon.Log("Vars.AppendTagEntries result null"); return; }
            try
            {
                var ok = Convert.ToBoolean(richResult.GetType().GetProperty("Ok")?.GetValue(richResult) ?? false);
                if (!ok){ VarTableCommon.Log("Vars.AppendTagEntries Ok=false"); return; }
                var valueProp = richResult.GetType().GetProperty("Value");
                var payload = valueProp!=null ? valueProp.GetValue(richResult) as System.Collections.IEnumerable : null;
                if (payload == null){ VarTableCommon.Log("Vars.AppendTagEntries value null"); return; }
                int count=0; foreach (var s in payload){ string tag = s?.ToString(); if (string.IsNullOrEmpty(tag)) continue; _rows.Add(new Row{ scope="Tag", key=tag, type="string", value=string.Empty }); count++; }
                VarTableCommon.Log("Vars.AppendTagEntries +"+count);
            }
            catch(System.Exception ex){ VarTableCommon.Log("Vars.AppendTagEntries exception: "+ex.Message); }
        }
        internal void OnChanged(){ _dirty=true; }
        internal sealed class Row
        {
            public string scope;
            public string key;
            public string type;
            public string value;
            public Row Clone() => new Row{ scope=this.scope, key=this.key, type=this.type, value=this.value };
        }
        private sealed class VarsRowAdapter : IRowAdapter
        {
            private readonly VarsDataSet _ds; private readonly Row _r;
            public VarsRowAdapter(VarsDataSet ds, Row r){ _ds=ds; _r=r; }
            public object Get(string columnId)
            {
                switch(columnId){ case "scope": return _r.scope; case "key": return _r.key; case "type": return _r.type; case "value": return _r.value; default: return null; }
            }
            public bool Set(string columnId, object value)
            {
                switch(columnId)
                {
                    case "scope": _r.scope = value?.ToString() ?? "Var"; _ds.OnChanged(); return true;
                    case "key": _r.key = value?.ToString() ?? string.Empty; _ds.OnChanged(); return true;
                    case "type": _r.type = value?.ToString() ?? "string"; _ds.OnChanged(); return true;
                    case "value": _r.value = value?.ToString() ?? string.Empty; _ds.OnChanged(); return true;
                    default: return false;
                }
            }
        }
    }

    // Variables-only dataset
    internal sealed class VariablesOnlyDataSet : ITableDataSet
    {
        private readonly List<(string key,string type,string value)> _rows = new List<(string,string,string)>();
        private List<(string key,string type,string value)> _original = new List<(string,string,string)>();
        public int Count => _rows.Count; public bool IsDirty { get; private set; }
        private object _item => ItemModKitPanelState.CapturedItem;
        public VariablesOnlyDataSet(){ Reload(); }
        public IRowAdapter GetRow(int index) => new RowAdapter(this, index);
        public bool AddNew(){ var key=UniqueKey("VarKey"); _rows.Add((key,"string","")); IsDirty=true; return true; }
        public bool RemoveAt(int index){ if(index<0||index>=_rows.Count) return false; _rows.RemoveAt(index); IsDirty=true; return true; }
        public bool Move(int from,int to){ if(from<0||to<0||from>=_rows.Count||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); IsDirty=true; return true; }
        public bool Commit(){ try{ var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var write=VarTableCommon.GetStaticMember(duck,"Write"); var dict=new Dictionary<string,object>(); foreach(var r in _rows){ dict[r.key]=VarTableCommon.ParseToType(r.value,r.type);} var m=write.GetType().GetMethod("TryWriteVariables"); m?.Invoke(write,new object[]{ _item, dict, true }); MarkDirty(); return true; }catch{ return false; } }
        public bool Reload(){ try{ _rows.Clear(); var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var read=VarTableCommon.GetStaticMember(duck,"Read"); var rr=read.GetType().GetMethod("TryReadVariables")?.Invoke(read,new object[]{ _item }); Append(rr); _original=_rows.ToList(); IsDirty=false; return true; }catch{ return false; } }
        private void Append(object rich){ if(rich==null) return; bool ok=Convert.ToBoolean(rich.GetType().GetProperty("Ok")?.GetValue(rich) ?? false); if(!ok) return; var payload=rich.GetType().GetProperty("Value")?.GetValue(rich) as System.Collections.IEnumerable; if(payload==null) return; foreach(var e in payload){ string key=Convert.ToString(VarTableCommon.GetMaybe(e,new[]{"Key","key","Name","name"})); var valObj=VarTableCommon.GetMaybe(e,new[]{"Value","value"}); string type=VarTableCommon.InferTypeName(valObj); _rows.Add((key,type,valObj?.ToString()??string.Empty)); } }
        private void MarkDirty(){ IsDirty=false; _original=_rows.ToList(); }
        private string UniqueKey(string baseKey){ string k=baseKey; int i=1; var set=new HashSet<string>(_rows.Select(r=>r.key), StringComparer.Ordinal); while(set.Contains(k)) k=baseKey+(++i).ToString(); return k; }
        private sealed class RowAdapter : IRowAdapter
        {
            private readonly VariablesOnlyDataSet _ds; private int _i; public RowAdapter(VariablesOnlyDataSet ds,int i){_ds=ds;_i=i;}
            public object Get(string id){ var r=_ds._rows[_i]; switch(id){ case "key": return r.key; case "type": return r.type; case "value": return r.value; default: return null; } }
            public bool Set(string id, object v){ var r=_ds._rows[_i]; switch(id){ case "key": r.key=v?.ToString()??""; break; case "type": r.type=v?.ToString()??"string"; break; case "value": r.value=v?.ToString()??""; break; default: return false; } _ds._rows[_i]=r; _ds.IsDirty=true; return true; }
        }
    }

    // Constants-only dataset
    internal sealed class ConstantsOnlyDataSet : ITableDataSet
    {
        private readonly List<(string key,string type,string value)> _rows = new List<(string,string,string)>();
        private List<(string key,string type,string value)> _original = new List<(string,string,string)>();
        public int Count => _rows.Count; public bool IsDirty { get; private set; }
        private object _item => ItemModKitPanelState.CapturedItem;
        public ConstantsOnlyDataSet(){ Reload(); }
        public IRowAdapter GetRow(int index) => new RowAdapter(this, index);
        public bool AddNew(){ var key=UniqueKey("ConstKey"); _rows.Add((key,"string","")); IsDirty=true; return true; }
        public bool RemoveAt(int index){ if(index<0||index>=_rows.Count) return false; _rows.RemoveAt(index); IsDirty=true; return true; }
        public bool Move(int from,int to){ if(from<0||to<0||from>=_rows.Count||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); IsDirty=true; return true; }
        public bool Commit(){ try{ var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var write=VarTableCommon.GetStaticMember(duck,"Write"); var dict=new Dictionary<string,object>(); foreach(var r in _rows){ dict[r.key]=VarTableCommon.ParseToType(r.value,r.type);} var m=write.GetType().GetMethod("TryWriteConstants"); m?.Invoke(write,new object[]{ _item, dict, true }); MarkDirty(); return true; }catch{ return false; } }
        public bool Reload(){ try{ _rows.Clear(); var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var read=VarTableCommon.GetStaticMember(duck,"Read"); var rr=read.GetType().GetMethod("TryReadConstants")?.Invoke(read,new object[]{ _item }); Append(rr); _original=_rows.ToList(); IsDirty=false; return true; }catch{ return false; } }
        private void Append(object rich){ if(rich==null) return; bool ok=Convert.ToBoolean(rich.GetType().GetProperty("Ok")?.GetValue(rich) ?? false); if(!ok) return; var payload=rich.GetType().GetProperty("Value")?.GetValue(rich) as System.Collections.IEnumerable; if(payload==null) return; foreach(var e in payload){ string key=Convert.ToString(VarTableCommon.GetMaybe(e,new[]{"Key","key","Name","name"})); var valObj=VarTableCommon.GetMaybe(e,new[]{"Value","value"}); string type=VarTableCommon.InferTypeName(valObj); _rows.Add((key,type,valObj?.ToString()??string.Empty)); } }
        private void MarkDirty(){ IsDirty=false; _original=_rows.ToList(); }
        private string UniqueKey(string baseKey){ string k=baseKey; int i=1; var set=new HashSet<string>(_rows.Select(r=>r.key), StringComparer.Ordinal); while(set.Contains(k)) k=baseKey+(++i).ToString(); return k; }
        private sealed class RowAdapter : IRowAdapter
        {
            private readonly ConstantsOnlyDataSet _ds; private int _i; public RowAdapter(ConstantsOnlyDataSet ds,int i){_ds=ds;_i=i;}
            public object Get(string id){ var r=_ds._rows[_i]; switch(id){ case "key": return r.key; case "type": return r.type; case "value": return r.value; default: return null; } }
            public bool Set(string id, object v){ var r=_ds._rows[_i]; switch(id){ case "key": r.key=v?.ToString()??""; break; case "type": r.type=v?.ToString()??"string"; break; case "value": r.value=v?.ToString()??""; break; default: return false; } _ds._rows[_i]=r; _ds.IsDirty=true; return true; }
        }
    }

    // Tags-only dataset
    internal sealed class TagsOnlyDataSet : ITableDataSet
    {
        private readonly List<string> _rows = new List<string>();
        private List<string> _original = new List<string>();
        public int Count => _rows.Count; public bool IsDirty { get; private set; }
        private object _item => ItemModKitPanelState.CapturedItem;
        public TagsOnlyDataSet(){ Reload(); }
        public IRowAdapter GetRow(int index) => new RowAdapter(this, index);
        public bool AddNew(){ var key=UniqueKey("Tag"); _rows.Add(key); IsDirty=true; return true; }
        public bool RemoveAt(int index){ if(index<0||index>=_rows.Count) return false; _rows.RemoveAt(index); IsDirty=true; return true; }
        public bool Move(int from,int to){ if(from<0||to<0||from>=_rows.Count||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); IsDirty=true; return true; }
        public bool Commit(){ try{ var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var write=VarTableCommon.GetStaticMember(duck,"Write"); var m=write.GetType().GetMethod("TryWriteTags"); m?.Invoke(write,new object[]{ _item, _rows.ToArray(), false }); MarkDirty(); return true; }catch{ return false; } }
        public bool Reload(){ try{ _rows.Clear(); var duck=VarTableCommon.FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); var read=VarTableCommon.GetStaticMember(duck,"Read"); var rr=read.GetType().GetMethod("TryReadTags")?.Invoke(read,new object[]{ _item }); Append(rr); _original=_rows.ToList(); IsDirty=false; return true; }catch{ return false; } }
        private void Append(object rich){ if(rich==null) return; bool ok=Convert.ToBoolean(rich.GetType().GetProperty("Ok")?.GetValue(rich) ?? false); if(!ok) return; var payload=rich.GetType().GetProperty("Value")?.GetValue(rich) as System.Collections.IEnumerable; if(payload==null) return; foreach(var s in payload){ var tag=s?.ToString(); if(string.IsNullOrEmpty(tag)) continue; _rows.Add(tag); } }
        private void MarkDirty(){ IsDirty=false; _original=_rows.ToList(); }
        private string UniqueKey(string baseKey){ string k=baseKey; int i=1; var set=new HashSet<string>(_rows, StringComparer.Ordinal); while(set.Contains(k)) k=baseKey+(++i).ToString(); return k; }
        private sealed class RowAdapter : IRowAdapter
        {
            private readonly TagsOnlyDataSet _ds; private int _i; public RowAdapter(TagsOnlyDataSet ds,int i){_ds=ds;_i=i;}
            public object Get(string id){ var tag=_ds._rows[_i]; return id=="key"? tag : null; }
            public bool Set(string id, object v){ if(id!="key") return false; _ds._rows[_i]=v?.ToString()??string.Empty; _ds.IsDirty=true; return true; }
        }
    }
}
