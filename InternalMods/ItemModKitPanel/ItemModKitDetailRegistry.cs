using System;
using System.Collections.Generic;
using IMK.SettingsUI.InternalMods.ItemModKitPanel;
using IMK.SettingsUI.Table;

namespace IMK.SettingsUI.InternalMods.ItemModKitPanel
{
    /// <summary>Descriptor registry for item mod kit detail edit pages. Allows specialized field sets per kind.</summary>
    internal static class ItemModKitDetailRegistry
    {
        internal sealed class FieldDescriptor
        {
            public string Id;
            public string Title;
            public string Desc;
            public bool Editable;
            public string[] Options;
            public float? Min;
            public float? Max;
            public Func<object> Get;
            public Action<object> Set;
        }
        private static readonly Dictionary<string, Func<int,List<FieldDescriptor>>> _builders = new Dictionary<string, Func<int,List<FieldDescriptor>>>(StringComparer.OrdinalIgnoreCase);
        static ItemModKitDetailRegistry()
        {
            // variables (vars) use dataset adapter columns; fallback handles it, so no custom builder yet
            // constants (consts) fallback
            // tags fallback
            // stats custom minimal builder (key, value, remove)
            Register("stats", index => BuildFromActive("stats", index));
            Register("modifiers", index => BuildFromActive("modifiers", index));
            Register("slots", index => BuildFromActive("slots", index));
        }
        internal static void Register(string kind, Func<int,List<FieldDescriptor>> builder){ if(string.IsNullOrEmpty(kind)||builder==null) return; _builders[kind]=builder; }
        internal static List<FieldDescriptor> BuildDescriptors(string kind, int index)
        {
            if (_builders.TryGetValue(kind, out var b))
            {
                try { return b(index); } catch { return null; }
            }
            return null; // fallback to schema columns
        }
        // Generic builder using active dataset row
        private static List<FieldDescriptor> BuildFromActive(string kind, int index)
        {
            if (!ItemModKitPanelPages.TryGetActive(kind, out var schema, out var data)) return null;
            if (index < 0 || index >= data.Count) return null;
            var adapter = data.GetRow(index);
            var list = new List<FieldDescriptor>();
            foreach (var col in schema.Columns)
            {
                var val = adapter.Get(col.Id);
                list.Add(new FieldDescriptor
                {
                    Id = col.Id,
                    Title = col.Title,
                    Desc = col.Id,
                    Editable = !col.ReadOnly,
                    Options = col.Options,
                    Min = col.Min,
                    Max = col.Max,
                    Get = ()=> adapter.Get(col.Id),
                    Set = v=> { if(!col.ReadOnly) adapter.Set(col.Id, v); }
                });
            }
            return list;
        }
    }
}
