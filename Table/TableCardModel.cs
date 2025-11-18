using System.Collections.Generic;
using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.Table
{
    /// <summary>
    /// 通用表格卡片模型（公共入口）。任何 Mod 提供 Schema + DataSet 即可使用。
    /// 不依赖外部库，渲染由内部控制器负责。
    /// </summary>
    public sealed class TableCardModel : CardModelBase
    {
        public ITableSchema Schema { get; set; }
        public ITableDataSet DataSet { get; set; }
        public bool ShowAddButton { get; set; } = true;
        public bool ShowImportExport { get; set; } = true;
        public bool ShowMoveButtons { get; set; } = true;
        public TableCardModel(){ Kind = CardKind.Setting; Size = CardSize.XLarge; }
    }
}
