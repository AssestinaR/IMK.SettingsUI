using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IMK.SettingsUI.Table.Adapters
{
    /// <summary>
    /// 通用 JSON 文件数据集。将 List<T> 作为行数据，行通过 ReflectionRowAdapter 访问。
    /// </summary>
    public sealed class JsonFileDataSet<T> : ITableDataSet where T : new()
    {
        private readonly string _dir;
        private readonly string _fileName;
        private readonly Func<List<T>> _buildDefault;
        private readonly List<T> _items = new List<T>();
        private bool _dirty;
        public JsonFileDataSet(string dir, string fileName, Func<List<T>> buildDefault)
        {
            _dir = dir; _fileName = fileName; _buildDefault = buildDefault ?? (()=> new List<T>());
            Reload();
        }
        public int Count => _items.Count;
        public IRowAdapter GetRow(int index){ if (index<0 || index>=_items.Count) return null; return new ReflectionRowAdapter<T>(_items[index]); }
        public bool AddNew(){ _items.Add(new T()); _dirty=true; return true; }
        public bool RemoveAt(int index){ if (index<0 || index>=_items.Count) return false; _items.RemoveAt(index); _dirty=true; return true; }
        public bool Move(int from, int to){ if (from<0||to<0||from>=_items.Count||to>=_items.Count) return false; var it=_items[from]; _items.RemoveAt(from); _items.Insert(to,it); _dirty=true; return true; }
        public bool Commit(){ try{ Directory.CreateDirectory(_dir); var path=Path.Combine(_dir,_fileName); var json = JsonUtility.ToJson(new Wrapper{ items=_items }, true); File.WriteAllText(path, json); _dirty=false; return true; } catch (Exception ex){ Debug.LogWarning($"[Table.JsonFileDataSet] Commit failed: {ex.Message}"); return false; } }
        public bool Reload(){ try{ Directory.CreateDirectory(_dir); var path=Path.Combine(_dir,_fileName); if (!File.Exists(path)){ _items.Clear(); _items.AddRange(_buildDefault()); Commit(); return true; } var text=File.ReadAllText(path); var wrap = JsonUtility.FromJson<Wrapper>(text); _items.Clear(); if (wrap?.items!=null) _items.AddRange(wrap.items); _dirty=false; return true; } catch (Exception ex){ Debug.LogWarning($"[Table.JsonFileDataSet] Reload failed: {ex.Message}"); return false; } }
        public bool IsDirty => _dirty;
        [Serializable] private class Wrapper{ public List<T> items; }
    }
}
