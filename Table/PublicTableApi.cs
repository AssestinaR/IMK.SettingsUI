using System;
using System.Collections.Generic;

namespace IMK.SettingsUI.Table
{
    /// <summary>
    /// Public, provider-agnostic table UI contracts. Mods can implement these接口以复用 SettingsUI 的通用表格控件。
    /// 仅暴露接口与简单POCO，内部实现与控制器保持可替换。
    /// </summary>
    public enum TableCellKind { Text, Number, Slider, Dropdown, Toggle, Readonly }

    public sealed class TableColumn
    {
        public string Id;               // 唯一列id
        public string Title;            // 显示标题
        public TableCellKind Kind;      // 编辑器类型
        public Type ValueType;          // 值类型（string/float/int/bool...）
        public float? Min;              // 数值下限（Number/Slider）
        public float? Max;              // 数值上限（Number/Slider）
        public string[] Options;        // 下拉可选项（Dropdown）
        public bool ReadOnly;           // 只读列
        public float? WidthHint;        // 宽度建议（像素）
        public Func<object, string> Formatter;   // 显示格式化（可选）
        public Func<object, bool> Validator;     // 校验（返回true表示合法）
    }

    public interface ITableSchema
    {
        IReadOnlyList<TableColumn> Columns { get; }
    }

    /// <summary>单行适配器：按列id读写单元格。</summary>
    public interface IRowAdapter
    {
        object Get(string columnId);
        bool Set(string columnId, object value);
    }

    /// <summary>数据集：行访问与持久化。</summary>
    public interface ITableDataSet
    {
        int Count { get; }
        IRowAdapter GetRow(int index);
        bool AddNew();
        bool RemoveAt(int index);
        bool Move(int from, int to);
        bool Commit();     // 持久化保存
        bool Reload();     // 重新从存储加载（丢弃未提交）
        bool IsDirty { get; }
    }

    /// <summary>可选：自定义单元格编辑器工厂（高级扩展点）。</summary>
    public interface ICellEditorFactory
    {
        // 预留：后续 SchemaTableController 使用。当前占位以保持API稳定。
    }
}
