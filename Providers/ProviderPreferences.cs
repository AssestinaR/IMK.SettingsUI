using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace IMK.SettingsUI.Providers
{
    /// <summary>Simple persistence for provider visibility and ordering.</summary>
    internal static class ProviderPreferences
    {
        private class Model
        {
            public Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        }
        public sealed class Entry
        {
            public bool Enabled = true;
            public int Order = 0;
        }
        private static Model _model;
        private static string NewDir => Path.Combine(Application.persistentDataPath, "Mods", "IMK.SettingsUI");
        private static string NewFile => Path.Combine(NewDir, "providers.json");

        public static void EnsureLoaded()
        {
            if (_model != null) return;
            try
            {
                if (File.Exists(NewFile))
                {
                    var json = File.ReadAllText(NewFile);
                    _model = JsonConvert.DeserializeObject<Model>(json) ?? new Model();
                }
                else _model = new Model();
            }
            catch { _model = new Model(); }
        }
        public static void Save()
        {
            try
            {
                var dir = NewDir; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(_model, Formatting.Indented);
                File.WriteAllText(NewFile, json);
            }
            catch (System.Exception ex){ Debug.LogWarning("[SettingsUI.ProviderPreferences] Save failed: "+ex.Message); }
        }
        public static Entry GetOrCreate(string id, string title)
        {
            EnsureLoaded();
            if (!_model.Entries.TryGetValue(id, out var e))
            {
                e = new Entry();
                // defaults: CoreShell, Sample disabled; others enabled
                if (string.Equals(id, "CoreShell", StringComparison.OrdinalIgnoreCase) || string.Equals(id, "Sample", StringComparison.OrdinalIgnoreCase)) e.Enabled = false; else e.Enabled = true;
                // default order by title alpha; compute hash-based baseline to avoid collisions
                e.Order = title == null ? 0 : title.ToLowerInvariant().GetHashCode();
                _model.Entries[id] = e;
            }
            // Hard rule: SettingsUI must not be disabled
            if (string.Equals(id, "SettingsUI", StringComparison.OrdinalIgnoreCase)) e.Enabled = true;
            return e;
        }
        public static void Set(string id, bool enabled, int order, string title)
        {
            var e = GetOrCreate(id, title);
            // Hard rule: SettingsUI cannot be disabled
            if (string.Equals(id, "SettingsUI", StringComparison.OrdinalIgnoreCase)) enabled = true;
            e.Enabled = enabled; e.Order = order;
        }
        public static void Remove(string id)
        {
            EnsureLoaded(); _model.Entries.Remove(id);
        }
        public static IReadOnlyList<(string id, string title, Entry pref)> BuildOrderedList(IReadOnlyDictionary<string, ISettingsProvider> providers)
        {
            EnsureLoaded();
            var list = new List<(string id, string title, Entry pref)>();
            foreach (var kv in providers)
            {
                var id = kv.Key; var title = kv.Value?.Title ?? id; var pref = GetOrCreate(id, title);
                // enforce hard rule again when building list
                if (string.Equals(id, "SettingsUI", StringComparison.OrdinalIgnoreCase)) pref.Enabled = true;
                list.Add((id, title, pref));
            }
            // sort by Order ascending, then title
            list.Sort((a,b)=> {
                int c = a.pref.Order.CompareTo(b.pref.Order); if (c!=0) return c; return string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }
        public static Dictionary<string, Entry> Snapshot()
        {
            EnsureLoaded();
            return new Dictionary<string, Entry>(_model.Entries, StringComparer.OrdinalIgnoreCase);
        }
    }
}
