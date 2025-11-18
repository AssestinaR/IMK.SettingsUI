using System;
using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Theme;
using UnityEngine;

namespace IMK.SettingsUI.Settings
{
    public static class SettingsApplyService
    {
        public static bool Apply(IEnumerable<ICardModel> models)
        {
            bool changed = false;
            // collect persistence changes per group
            var persistGroups = new Dictionary<string, Dictionary<string, object>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var m in models)
            {
                if (m is BoundSettingCardModel b)
                {
                    var newVal = b.Pending ?? b.Getter?.Invoke();
                    if (!Equals(newVal, b.OriginalValue))
                    {
                        try { b.Setter?.Invoke(newVal); b.OriginalValue = newVal; changed = true; } catch (Exception ex) { Debug.LogWarning("[SettingsUI.Apply] Bound setting apply failed: " + ex.Message); }
                        if (b.Persist)
                        {
                            var group = ResolveGroup(m, b.PersistGroup);
                            var key = string.IsNullOrEmpty(b.PersistKey) ? (m.Id ?? m.Title ?? "setting") : b.PersistKey;
                            AddPersist(persistGroups, group, key, newVal);
                        }
                    }
                    continue;
                }
                if (m is ListSettingCardModel ls)
                {
                    var vals = ls.PendingValues ?? ls.InitialValues;
                    if (!SequenceEqual(vals, ls.InitialValues))
                    {
                        try { ls.Setter?.Invoke(vals ?? System.Array.Empty<string>()); ls.InitialValues = vals; changed = true; } catch (Exception ex) { Debug.LogWarning("[SettingsUI.Apply] List setting apply failed: " + ex.Message); }
                        if (ls.Persist)
                        {
                            var group = ResolveGroup(m, ls.PersistGroup);
                            var key = string.IsNullOrEmpty(ls.PersistKey) ? (m.Id ?? m.Title ?? "list") : ls.PersistKey;
                            AddPersist(persistGroups, group, key, vals);
                        }
                    }
                    continue;
                }
                if (m is ToggleSliderSettingCardModel ts)
                {
                    if (ts.Pending != ts.Initial)
                    {
                        try { ts.Setter?.Invoke(ts.Pending); ts.Initial = ts.Pending; changed = true; } catch (Exception ex) { Debug.LogWarning("[SettingsUI.Apply] Toggle slider apply failed: " + ex.Message); }
                        if (ts.Persist)
                        {
                            var group = ResolveGroup(m, ts.PersistGroup);
                            var key = string.IsNullOrEmpty(ts.PersistKey) ? (m.Id ?? m.Title ?? "toggle") : ts.PersistKey;
                            AddPersist(persistGroups, group, key, ts.Pending);
                        }
                    }
                    continue;
                }
                if (m is SettingCardModel s)
                {
                    if (s.Pending != null && !Equals(s.Pending, s.Initial))
                    {
                        bool mapped = SettingsApplyRegistry.TryApply(s.Id, s.Pending);
                        if (!mapped) Debug.LogWarning("[SettingsUI.Apply] Unmapped SettingCardModel id=" + s.Id);
                        s.Initial = s.Pending; if (mapped) changed = true;
                        if (s.Persist)
                        {
                            var group = ResolveGroup(m, s.PersistGroup);
                            var key = string.IsNullOrEmpty(s.PersistKey) ? (m.Id ?? m.Title ?? "setting") : s.PersistKey;
                            AddPersist(persistGroups, group, key, s.Pending);
                        }
                    }
                }
            }
            // flush persistence groups
            foreach (var kv in persistGroups)
            {
                try { SettingsPersistence.Save(kv.Key, kv.Value); } catch { }
            }
            if (changed)
            {
                SettingsStore.Current.window.width = ThemeMetrics.WindowWidth;
                SettingsStore.Current.window.height = ThemeMetrics.WindowHeight;
                SettingsStore.Current.nav.width = ThemeMetrics.NavWidth;
                SettingsStore.Save();
                SettingsRefreshUtil.RefreshLayout();
                Debug.Log("[IMK.SettingsUI] Applied settings.");
            }
            return changed;
        }
        private static void AddPersist(Dictionary<string, Dictionary<string, object>> map, string group, string key, object value)
        {
            if (!map.TryGetValue(group, out var dict)) { dict = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase); map[group] = dict; }
            dict[key] = value;
        }
        private static string ResolveGroup(ICardModel model, string explicitGroup)
        {
            if (!string.IsNullOrEmpty(explicitGroup)) return explicitGroup;
            // default to provider id inferred from model.Id prefix "Provider:...", otherwise fallback to "IMK.SettingsUI"
            var id = model?.Id; if (!string.IsNullOrEmpty(id))
            {
                int sep = id.IndexOf(':'); if (sep > 0) return id[..sep];
            }
            return "IMK.SettingsUI";
        }
        private static bool SequenceEqual(string[] a, string[] b)
        {
            if (a == b) return true; if (a == null || b == null) return false; if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (!System.String.Equals(a[i], b[i])) return false; return true;
        }
    }
}
