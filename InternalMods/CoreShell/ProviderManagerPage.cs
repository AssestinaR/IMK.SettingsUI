using System;
using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Table;
using IMK.SettingsUI.Providers;

namespace IMK.SettingsUI.InternalMods.CoreShell
{
    internal sealed class ProviderManagerSchema : ITableSchema
    {
        private readonly List<TableColumn> _cols = new List<TableColumn>
        {
            new TableColumn{ Id="id", Title="Id", Kind=TableCellKind.Readonly, ReadOnly=true, WidthHint=180f },
            new TableColumn{ Id="title", Title="Title", Kind=TableCellKind.Readonly, ReadOnly=true, WidthHint=180f },
            new TableColumn{ Id="enabled", Title="Enabled", Kind=TableCellKind.Toggle, ValueType=typeof(bool), ReadOnly=false, WidthHint=80f },
            new TableColumn{ Id="order", Title="Order", Kind=TableCellKind.Number, ValueType=typeof(int), ReadOnly=false, WidthHint=80f },
        };
        public IReadOnlyList<TableColumn> Columns => _cols;
    }
    internal sealed class ProviderManagerDataSet : ITableDataSet
    {
        private readonly List<Row> _rows = new List<Row>();
        private bool _dirty;
        private Dictionary<string, ProviderPreferences.Entry> _snapshot;
        public int Count => _rows.Count;
        public bool IsDirty => _dirty;
        public ProviderManagerDataSet(){ Reload(); }
        public IRowAdapter GetRow(int index) => new RowAdapter(this, _rows[index]);
        public bool AddNew() => false; // not supported
        public bool RemoveAt(int index) => false;
        public bool Move(int from, int to)
        {
            if (from<0||to<0||from>=_rows.Count||to>=_rows.Count) return false; if (from==to) return true;
            var r=_rows[from]; _rows.RemoveAt(from); _rows.Insert(to,r); _dirty=true; RenumberOrders(true); return true;
        }
        public bool Commit()
        {
            try
            {
                // ensure sequential order before save
                RenumberOrders(false);
                foreach (var r in _rows)
                {
                    ProviderPreferences.Set(r.id, r.enabled, r.order, r.title);
                }
                ProviderPreferences.Save();
                // rebuild nav pane
                try
                {
                    var nav = UnityEngine.GameObject.Find("IMK.SettingsUI.Canvas/Window")?.GetComponent<IMK.SettingsUI.Navigation.NavController>();
                    var pane = nav?.transform.Find("Nav")?.GetComponent<IMK.SettingsUI.Navigation.NavPane>();
                    pane?.Refresh();
                }
                catch { }
                _dirty=false; return true;
            }
            catch { return false; }
        }
        public bool Reload()
        {
            try
            {
                _rows.Clear(); _snapshot = ProviderPreferences.Snapshot();
                var list = ProviderPreferences.BuildOrderedList(ProviderRegistry.All);
                foreach (var it in list)
                {
                    _rows.Add(new Row{ id=it.id, title=it.title, enabled=it.pref.Enabled, order=it.pref.Order });
                }
                // normalize order to 1..N without marking dirty
                RenumberOrders(false);
                _dirty=false; return true;
            }
            catch { return false; }
        }
        private void RenumberOrders(bool markDirty)
        {
            for (int i=0;i<_rows.Count;i++) _rows[i].order = i+1;
            if (markDirty) _dirty = true;
        }
        private sealed class Row
        {
            public string id; public string title; public bool enabled; public int order;
        }
        private sealed class RowAdapter : IRowAdapter
        {
            private readonly ProviderManagerDataSet _ds; private Row _r;
            public RowAdapter(ProviderManagerDataSet ds, Row r){ _ds=ds; _r=r; }
            public object Get(string columnId){ switch(columnId){ case "id": return _r.id; case "title": return _r.title; case "enabled": return _r.enabled; case "order": return _r.order; default: return null; } }
            public bool Set(string columnId, object value)
            {
                switch(columnId)
                {
                    case "enabled": _r.enabled = value is bool b ? b : _r.enabled; _ds._dirty=true; return true;
                    case "order":
                        try {
                            int val;
                            if (value is int ii) val=ii; else if (value is string ss) { int.TryParse(ss, out val); } else val = System.Convert.ToInt32(value);
                            _r.order = val; _ds._dirty=true; return true;
                        } catch { return false; }
                    default: return false;
                }
            }
        }
    }
}
