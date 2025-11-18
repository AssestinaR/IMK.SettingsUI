using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    internal sealed class ModifiersSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols;
        public ModifiersSchema()
        {
            _cols = new List<TableColumn>
            {
                new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=160f },
                new TableColumn{ Id="type", Title="Type", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=100f },
                new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Number, ValueType=typeof(float), ReadOnly=false, WidthHint=100f, Min=-999999f, Max=999999f },
                new TableColumn{ Id="display", Title="Display", Kind=TableCellKind.Toggle, ValueType=typeof(bool), ReadOnly=false, WidthHint=70f },
                new TableColumn{ Id="order", Title="Order", Kind=TableCellKind.Number, ValueType=typeof(int), ReadOnly=false, WidthHint=70f },
                new TableColumn{ Id="target", Title="Target", Kind=TableCellKind.Text, ValueType=typeof(string), ReadOnly=false, WidthHint=100f },
                new TableColumn{ Id="remove", Title="Remove", Kind=TableCellKind.Toggle, ValueType=typeof(bool), ReadOnly=false, WidthHint=70f },
            };
        }
        public IReadOnlyList<TableColumn> Columns => _cols;
    }

    internal sealed class ModifiersDataSet : ITableDataSet
    {
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<Row> _original = new List<Row>();
        private bool _dirty;
        private object _item => ItemModKitPanelState.CapturedItem;
        public ModifiersDataSet(){ Reload(); }
        public int Count => _rows.Count;
        public bool IsDirty => _dirty;
        public IRowAdapter GetRow(int index) => new RowAdapter(this, _rows[index]);
        public bool AddNew(){ _rows.Add(new Row{ key=UniqueKey("NewMod"), type="Add", value=0f, display=true, order=0, target=null, remove=false }); _dirty=true; return true; }
        public bool RemoveAt(int index){ if(index<0||index>=_rows.Count) return false; _rows.RemoveAt(index); _dirty=true; return true; }
        public bool Move(int from, int to){ if(from<0||from>=_rows.Count||to<0||to>=_rows.Count) return false; var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); _dirty=true; return true; }
        private static void Log(string msg){ if (IMK.SettingsUI.Diagnostics.DebugFlags.TableDiagEnabled) UnityEngine.Debug.Log("[IMK.ModifiersTable] "+msg); }
        private static (bool ok, string err) InvokeRich(object svc, MethodInfo m, params object[] args)
        {
            if (svc==null || m==null) return (false, "method null");
            try
            {
                var rr = m.Invoke(svc, args);
                if (rr == null) return (false, "null result");
                var okObj = rr.GetType().GetProperty("Ok")?.GetValue(rr);
                bool ok = okObj is bool b ? b : false;
                string err = rr.GetType().GetProperty("Error")?.GetValue(rr)?.ToString();
                return (ok, err);
            }
            catch(Exception ex){ return (false, ex.Message); }
        }
        public bool Commit()
        {
            try
            {
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Commit abort: IMKDuckov not found"); return false; }
                var write = GetStaticMember(duck, "Write"); if (write == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Commit abort: Write service null"); return false; }
                object item = _item; if (item == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Commit abort: item null"); return false; }
                // reflect methods (description only)
                var mAddDesc = write.GetType().GetMethod("TryAddModifierDescription");
                var mRemDesc = write.GetType().GetMethod("TryRemoveModifierDescription");
                var mSetVal = write.GetType().GetMethod("TrySetModifierDescriptionValue");
                var mSetType = write.GetType().GetMethod("TrySetModifierDescriptionType");
                var mSetDisplay = write.GetType().GetMethod("TrySetModifierDescriptionDisplay");
                var mSetOrder = write.GetType().GetMethod("TrySetModifierDescriptionOrder");
                var mDedup = write.GetType().GetMethod("TrySanitizeModifierDescriptions");

                // originals map
                var originalMap = new Dictionary<string, Row>(StringComparer.Ordinal);
                foreach (var o in _original) if (!string.IsNullOrEmpty(o.key) && !originalMap.ContainsKey(o.key)) originalMap[o.key] = o;

                // handle deletions
                foreach (var o in _original)
                {
                    bool still = _rows.Any(r => string.Equals(r.key, o.key, StringComparison.Ordinal));
                    if (!still && !string.IsNullOrEmpty(o.key))
                    {
                        var rr = mRemDesc?.Invoke(write, new object[]{ item, o.key });
                        UnityEngine.Debug.Log("[IMK.ModifiersTable] RemoveMissing key="+o.key+" ok="+GetOk(rr)+" err="+GetErr(rr));
                    }
                }

                foreach (var r in _rows)
                {
                    if (string.IsNullOrEmpty(r.key)) continue;
                    if (r.remove)
                    {
                        var rr = mRemDesc?.Invoke(write, new object[]{ item, r.key });
                        UnityEngine.Debug.Log("[IMK.ModifiersTable] RemoveFlagged key="+r.key+" ok="+GetOk(rr)+" err="+GetErr(rr));
                        continue;
                    }
                    bool isNew = !originalMap.ContainsKey(r.key);
                    var o = isNew ? null : originalMap[r.key];
                    bool changed = isNew || Math.Abs(o.value - r.value) > 0.0001f || !string.Equals(o.type, r.type, StringComparison.OrdinalIgnoreCase) || o.order != r.order || o.display != r.display || !string.Equals(o.target, r.target, StringComparison.Ordinal);
                    if (!changed) continue;

                    if (isNew)
                    {
                        // add description only
                        var addRes = mAddDesc?.Invoke(write, new object[]{ item, r.key, r.type, r.value, r.display, r.order, r.target });
                        UnityEngine.Debug.Log("[IMK.ModifiersTable] AddDesc key="+r.key+" ok="+GetOk(addRes)+" err="+GetErr(addRes));
                        if (!GetOk(addRes)) ManualFallbackAdd(item, r);
                    }
                    else
                    {
                        // update individual fields
                        if (Math.Abs(o.value - r.value) > 0.0001f)
                        {
                            var vr = mSetVal?.Invoke(write, new object[]{ item, r.key, r.value }); UnityEngine.Debug.Log("[IMK.ModifiersTable] SetVal key="+r.key+" ok="+GetOk(vr)+" err="+GetErr(vr));
                        }
                        if (!string.Equals(o.type, r.type, StringComparison.OrdinalIgnoreCase))
                        {
                            var tr = mSetType?.Invoke(write, new object[]{ item, r.key, r.type }); UnityEngine.Debug.Log("[IMK.ModifiersTable] SetType key="+r.key+" ok="+GetOk(tr)+" err="+GetErr(tr));
                            if (!GetOk(tr))
                            {
                                // fallback: try direct write (including backing field)
                                DirectSetDescriptor(item, r.key, "Type", r.type);
                            }
                        }
                        if (o.display != r.display)
                        {
                            var dr = mSetDisplay?.Invoke(write, new object[]{ item, r.key, r.display }); UnityEngine.Debug.Log("[IMK.ModifiersTable] SetDisplay key="+r.key+" ok="+GetOk(dr)+" err="+GetErr(dr));
                        }
                        if (o.order != r.order)
                        {
                            var orr = mSetOrder?.Invoke(write, new object[]{ item, r.key, r.order }); UnityEngine.Debug.Log("[IMK.ModifiersTable] SetOrder key="+r.key+" ok="+GetOk(orr)+" err="+GetErr(orr));
                        }
                        // target change (no dedicated setter; direct reflect assign)
                        if (!string.Equals(o.target, r.target, StringComparison.Ordinal)) DirectSetDescriptor(item, r.key, "Target", r.target);
                    }
                }

                // deduplicate/sanitize after changes
                var dedRes = mDedup?.Invoke(write, new object[]{ item }); UnityEngine.Debug.Log("[IMK.ModifiersTable] Dedup ok="+GetOk(dedRes)+" err="+GetErr(dedRes));
                DumpUnderlyingCollection(item);
                var reloaded = Reload(); UnityEngine.Debug.Log("[IMK.ModifiersTable] Commit(desc-only) reload="+reloaded+" rows="+_rows.Count);
                _dirty = false; return true;
            }
            catch (Exception ex){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Commit exception: "+ex.Message); return false; }
        }
        // Helper methods (restored)
        private string UniqueKey(string baseKey){ string k=baseKey; int i=1; var set=new HashSet<string>(_rows.Select(r=> r.key), StringComparer.Ordinal); while(set.Contains(k)) k=baseKey+(++i).ToString(); return k; }
        private static Type FindType(string fullName){ var t=Type.GetType(fullName); if(t!=null) return t; foreach(var asm in AppDomain.CurrentDomain.GetAssemblies()){ try { t=asm.GetType(fullName); if(t!=null) return t; } catch { } } return null; }
        private static object GetStaticMember(Type t, string name){ try { var p=t.GetProperty(name, BindingFlags.Public|BindingFlags.Static); if(p!=null) return p.GetValue(null); var f=t.GetField(name, BindingFlags.Public|BindingFlags.Static); if(f!=null) return f.GetValue(null); } catch { } return null; }
        private static object GetMaybe(object obj, string[] names){ if(obj==null) return null; var tt=obj.GetType(); foreach(var n in names){ var p=tt.GetProperty(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if(p!=null){ try { return p.GetValue(obj); } catch { } } var f=tt.GetField(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if(f!=null){ try { return f.GetValue(obj); } catch { } } } return null; }
        private static float ConvertToFloat(object v){ try { if(v==null) return 0f; if(v is float f) return f; if(v is double d) return (float)d; if(v is int i) return i; if(v is long l) return l; if(v is string s && float.TryParse(s, out var pf)) return pf; return Convert.ToSingle(v); } catch { return 0f; } }
        private static bool GetOk(object rr){ try { return rr!=null && (bool)(rr.GetType().GetProperty("Ok")?.GetValue(rr) ?? false); } catch { return false; } }
        private static string GetErr(object rr){ try { return rr?.GetType().GetProperty("Error")?.GetValue(rr)?.ToString(); } catch { return null; } }
        private static void LogRich(string action, string key, object rr){ UnityEngine.Debug.Log($"[IMK.ModifiersTable] {action} key={key} ok={GetOk(rr)} err={GetErr(rr)}"); }
        private void ManualFallbackAdd(object item, Row r)
        {
            try
            {
                var modsGetter = item.GetType().GetProperty("Modifiers", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                var modsCol = modsGetter?.GetValue(item); if (modsCol == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Fallback add aborted: Mods collection null"); return; }
                var descType = modsCol.GetType().GetGenericArguments().FirstOrDefault() ?? FindType("ItemStatsSystem.ModifierDescription"); if (descType == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Fallback add: description type missing"); return; }
                var inst = Activator.CreateInstance(descType);
                DirectAssign(descType, inst, "Key", r.key);
                DirectAssignEnum(descType, inst, "Type", r.type);
                DirectAssign(descType, inst, "Value", r.value);
                DirectAssign(descType, inst, "Display", r.display);
                DirectAssign(descType, inst, "Order", r.order);
                if (!string.IsNullOrEmpty(r.target)) DirectAssign(descType, inst, "Target", r.target);
                var addM = modsCol.GetType().GetMethod("Add", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance, null, new Type[]{ descType }, null)
                            ?? modsCol.GetType().GetMethod("Add", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                addM?.Invoke(modsCol, new[]{ inst });
                UnityEngine.Debug.Log("[IMK.ModifiersTable] Fallback add success key="+r.key);
            }
            catch(Exception ex){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Fallback add exception: "+ex.Message); }
        }
        private static void DirectAssign(Type t, object obj, string name, object val){ try { var p=t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if(p!=null){ p.SetValue(obj, val); return; } var f=t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance) ?? t.GetField(name.ToLowerInvariant(), BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); if(f!=null) f.SetValue(obj,val); } catch { } }
        private static void DirectAssignEnum(Type t, object obj, string name, string raw){ try { var p=t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); var et=p?.PropertyType; if(et==null){ var f=t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); et=f?.FieldType; } if(et!=null && et.IsEnum){ object val; try{ val=Enum.Parse(et, raw, true);} catch { val=Enum.GetValues(et).GetValue(0);} if(p!=null) p.SetValue(obj,val); else { var f2=t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); f2?.SetValue(obj,val);} } } catch { } }
        private void DirectSetDescriptor(object item, string key, string fieldName, object value)
        {
            try
            {
                var modsGetter = item.GetType().GetProperty("Modifiers", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                var modsCol = modsGetter?.GetValue(item) as System.Collections.IEnumerable; if (modsCol == null) return;
                foreach (var d in modsCol)
                {
                    if (d == null) continue; var k = Convert.ToString(GetMaybe(d, new[]{"Key","key"})); if (!string.Equals(k, key, StringComparison.Ordinal)) continue;
                    var dt = d.GetType(); object assignVal = value;
                    // property first (allow non-public)
                    var prop = dt.GetProperty(fieldName, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (prop != null)
                    {
                        try { if (prop.PropertyType.IsEnum && value is string s){ assignVal = Enum.Parse(prop.PropertyType, s, true); } prop.SetValue(d, assignVal); UnityEngine.Debug.Log("[IMK.ModifiersTable] DirectSetDescriptor prop "+fieldName+" key="+key); }
                        catch { /* fallthrough to field */ }
                    }
                    // standard field names
                    var fld = dt.GetField(fieldName.ToLowerInvariant(), BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance) ?? dt.GetField(fieldName, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (fld != null)
                    {
                        try { if (fld.FieldType.IsEnum && value is string s){ assignVal = Enum.Parse(fld.FieldType, s, true); } fld.SetValue(d, assignVal); UnityEngine.Debug.Log("[IMK.ModifiersTable] DirectSetDescriptor field "+fieldName+" key="+key); }
                        catch { /* fallthrough to backing field */ }
                    }
                    // auto-property backing field fallback: <Name>k__BackingField
                    var fields = dt.GetFields(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
                    foreach (var f in fields)
                    {
                        var fn = f.Name;
                        if (fn.IndexOf("k__BackingField", StringComparison.Ordinal) >= 0 && fn.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            try { object v = assignVal; if (f.FieldType.IsEnum && value is string ss) v = Enum.Parse(f.FieldType, ss, true); f.SetValue(d, v); UnityEngine.Debug.Log("[IMK.ModifiersTable] DirectSetDescriptor backing "+fieldName+" key="+key); }
                            catch { }
                            break;
                        }
                    }
                    break;
                }
                // try reapply after direct write
                try
                {
                    var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov");
                    var write = GetStaticMember(duck, "Write");
                    var mRe = write?.GetType().GetMethod("TryReapplyModifiers");
                    mRe?.Invoke(write, new object[]{ item });
                }
                catch { }
            }
            catch { }
        }
        private void DumpUnderlyingCollection(object item)
        {
            try
            {
                var modsGetter = item.GetType().GetProperty("Modifiers", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                var modsCol = modsGetter?.GetValue(item) as System.Collections.IEnumerable; if (modsCol == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Dump underlying: collection null"); return; }
                int i=0; foreach(var d in modsCol){ if(d==null) continue; string key = Convert.ToString(GetMaybe(d,new[]{"Key","key"})); float val = ConvertToFloat(GetMaybe(d,new[]{"Value","value"})); string type = Convert.ToString(GetMaybe(d,new[]{"Type","type"})); bool disp = Convert.ToBoolean(GetMaybe(d,new[]{"Display","display"}) ?? true); int order=0; try{ var o=GetMaybe(d,new[]{"Order","order"}); if(o!=null) order=Convert.ToInt32(o);}catch{} UnityEngine.Debug.Log($"[IMK.ModifiersTable] Underlying[{i}] key={key} val={val} type={type} disp={disp} order={order}"); i++; }
            }
            catch(Exception ex){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Dump underlying exception: "+ex.Message); }
        }
        // Enhanced Reload: merge descriptions + raw modifiers fallback
        public bool Reload()
        {
            try
            {
                _rows.Clear(); _original.Clear();
                var duck = FindType("ItemModKit.Adapters.Duckov.IMKDuckov"); if (duck == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload abort: IMKDuckov not found"); return false; }
                var read = GetStaticMember(duck, "Read"); if (read == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload abort: Read null"); return false; }
                var item = _item; if (item == null){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload abort: item null"); return false; }
                var mDesc = read.GetType().GetMethod("TryReadModifierDescriptions");
                var rrDesc = mDesc?.Invoke(read, new object[]{ item });
                bool okDesc = Convert.ToBoolean(rrDesc?.GetType().GetProperty("Ok")?.GetValue(rrDesc) ?? false);
                var payloadDesc = okDesc ? rrDesc?.GetType().GetProperty("Value")?.GetValue(rrDesc) as System.Collections.IEnumerable : null;
                var keysSeen = new HashSet<string>(StringComparer.Ordinal);
                int count=0;
                if (payloadDesc != null)
                {
                    foreach (var e in payloadDesc)
                    {
                        string key = Convert.ToString(GetMaybe(e, new[]{"Key","key"})) ?? string.Empty;
                        float value = ConvertToFloat(GetMaybe(e, new[]{"Value","value"}));
                        string type = Convert.ToString(GetMaybe(e, new[]{"Type","type"})) ?? "Add";
                        bool display = Convert.ToBoolean(GetMaybe(e, new[]{"Display","display"}) ?? true);
                        int order = 0; try { var o = GetMaybe(e, new[]{"Order","order"}); if (o!=null) order = Convert.ToInt32(o); } catch { }
                        string target = Convert.ToString(GetMaybe(e, new[]{"Target","target"}));
                        _rows.Add(new Row{ key=key, type=type, value=value, display=display, order=order, target=target, remove=false });
                        keysSeen.Add(key); UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload(desc) key="+key+" val="+value+" type="+type+" disp="+display+" order="+order);
                        count++;
                    }
                }
                // Fallback: raw modifiers if any new not appearing in descriptions
                var mRaw = read.GetType().GetMethod("TryReadModifiers"); var rrRaw = mRaw?.Invoke(read, new object[]{ item });
                bool okRaw = Convert.ToBoolean(rrRaw?.GetType().GetProperty("Ok")?.GetValue(rrRaw) ?? false);
                var payloadRaw = okRaw ? rrRaw?.GetType().GetProperty("Value")?.GetValue(rrRaw) as System.Collections.IEnumerable : null;
                if (payloadRaw != null)
                {
                    foreach (var e in payloadRaw)
                    {
                        string key = Convert.ToString(GetMaybe(e, new[]{"Key","key"})) ?? string.Empty;
                        if (string.IsNullOrEmpty(key) || keysSeen.Contains(key)) continue;
                        float value = ConvertToFloat(GetMaybe(e, new[]{"Value","value"}));
                        string type = Convert.ToString(GetMaybe(e, new[]{"Type","type"})) ?? "Add";
                        _rows.Add(new Row{ key=key, type=type, value=value, display=true, order=0, target=null, remove=false });
                        UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload(raw) key="+key+" val="+value+" type="+type);
                        count++;
                    }
                }
                _original.AddRange(_rows.Select(x=> x.Clone())); _dirty=false; UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload done total="+count);
                return true;
            }
            catch(Exception ex){ UnityEngine.Debug.Log("[IMK.ModifiersTable] Reload exception: "+ex.Message); return false; }
        }
        internal sealed class Row { public string key; public string type; public float value; public bool display; public int order; public string target; public bool remove; public Row Clone()=> (Row)this.MemberwiseClone(); }
        private sealed class RowAdapter : IRowAdapter
        {
            private readonly ModifiersDataSet _ds; private readonly Row _r; public RowAdapter(ModifiersDataSet ds, Row r){ _ds=ds; _r=r; }
            public object Get(string columnId){ switch(columnId){ case "key": return _r.key; case "type": return _r.type; case "value": return _r.value; case "display": return _r.display; case "order": return _r.order; case "target": return _r.target; case "remove": return _r.remove; default: return null; } }
            public bool Set(string columnId, object value)
            {
                switch(columnId)
                {
                    case "key": _r.key = value?.ToString() ?? string.Empty; _ds._dirty=true; return true;
                    case "type": _r.type = value?.ToString() ?? "Add"; _ds._dirty=true; return true;
                    case "value": try { if (value is float f) _r.value=f; else if (value is double d) _r.value=(float)d; else if (value is int i) _r.value=i; else if (value is string s) { float pf; if(float.TryParse(s, out pf)) _r.value = pf; } else _r.value = System.Convert.ToSingle(value); } catch { } _ds._dirty=true; return true;
                    case "display": _r.display = value is bool b ? b : _r.display; _ds._dirty=true; return true;
                    case "order": try { if (value is int ii) _r.order=ii; else if (value is string ss) { int pi; if(int.TryParse(ss, out pi)) _r.order=pi; } else _r.order = Convert.ToInt32(value); } catch { } _ds._dirty=true; return true;
                    case "target": _r.target = value?.ToString(); _ds._dirty=true; return true;
                    case "remove": _r.remove = value is bool bb ? bb : _r.remove; _ds._dirty=true; return true;
                    default: return false;
                }
            }
        }
    }
}
