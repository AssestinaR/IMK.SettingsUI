using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace IMK.SettingsUI.Settings
{
    /// <summary>Lightweight key-value persistence for setting cards. Files are per-group under Mods/&lt;group&gt;/config.json</summary>
    public static class SettingsPersistence
    {
        private static readonly Dictionary<string, Dictionary<string, object>> _cache = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        private static string ModsDir => Path.Combine(Application.persistentDataPath, "Mods");
        private static string GetGroupDir(string group) => Path.Combine(ModsDir, Sanitize(group));
        private static string GetFile(string group) => Path.Combine(GetGroupDir(group), "config.json");
        private static string Sanitize(string s) { if (string.IsNullOrEmpty(s)) return "Default"; foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }
        public static IDictionary<string, object> GetGroup(string group)
        {
            if (_cache.TryGetValue(group, out var map)) return map;
            map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var file = GetFile(group);
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        foreach (var kv in dict) map[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[SettingsUI.Persistence] Load group '{group}' failed: {ex.Message}"); }
            _cache[group] = map; return map;
        }
        public static T Get<T>(string group, string key, T def = default(T))
        {
            var g = GetGroup(group); if (g.TryGetValue(key, out var v))
            {
                try { if (v is T tv) return tv; return (T)Convert.ChangeType(v, typeof(T)); } catch { }
            }
            return def;
        }
        public static void Save(string group, IDictionary<string, object> changes)
        {
            if (changes == null || changes.Count == 0) return;
            var g = GetGroup(group);
            foreach (var kv in changes) g[kv.Key] = kv.Value;
            try
            {
                var dir = GetGroupDir(group); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(g, Formatting.Indented);
                File.WriteAllText(GetFile(group), json);
            }
            catch (Exception ex) { Debug.LogWarning($"[SettingsUI.Persistence] Save group '{group}' failed: {ex.Message}"); }
        }
    }
}
