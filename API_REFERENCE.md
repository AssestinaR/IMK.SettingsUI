# IMK.SettingsUI API 参考

面向后置模组 (Provider) 作者的核心可用类型、方法、事件与约束。仅列运行期稳定入口；内部实现细节与暂未稳定成员不在此清单中。

最后更新: 2025-11-18

---
## 1. Provider 体系
### 接口
- `ISettingsProvider`
  - `string Id` 唯一标识 (禁止包含冒号 ':').
  - `string Title` 导航显示标题.
  - `IEnumerable<NavItem> GetNavItems()` 可选：返回直接可导航项 (Id 形如 `ProviderId:PageId`).
  - `void BuildPage(string pageId, Transform parent)` 旧式直接构建（推荐使用模型接口替代）。
- `INavPageModelProvider`
  - `IEnumerable<ICardModel> BuildPageModels(string pageId)` 通过卡片模型构建页面；`pageId=="Root"` 为根页面.
- `IBreadcrumbProvider` (可选)
  - `bool TryGetParent(string logicalId, out string parentLogicalId)`
  - `bool TryGetTitle(string logicalId, out string title)`
  - `bool TryGetChain(string logicalId, List<string> chainOut)`

### 导航项模型
- `NavItem { string Id; string Title; }` Id 逻辑格式 `ProviderId:PageName`.

### 注册 / 事件
```csharp
ProviderRegistry.Register(new MyProvider());
ProviderRegistry.Unregister("MyProviderId");
bool ok = ProviderRegistry.TryGet("MyProviderId", out var provider);
IReadOnlyDictionary<string,ISettingsProvider> all = ProviderRegistry.All;
ProviderRegistry.OnRegister += p => { /* 回调 */ };
ProviderRegistry.OnUnregister += (id, old) => { /* 回调 */ };
```

---
## 2. 导航
- `NavController.NavigateTo(string logicalId)` 跳转到逻辑页面 (格式 `ProviderId:PageId`).
- Root 访问时自动补全 `:Root`。
- 错误回退：`ErrorCardFactory.MakeErrorCard(reason)` 生成 Markdown 错误卡片。

---
## 3. 卡片模型 (ICardModel)
### 公共属性
- `string Id` (建议唯一) 允许冒号分层，但 Persist 默认以第一段为组。
- `string Title`
- `string Desc`
- `CardKind Kind` 枚举: Navigation / Setting / Action / Markdown
- `CardSize Size` 枚举: Small / Medium / Large / XLarge
- `float HeightOverride` (>0 固定高度, <0 使用 MarkdownHeight, 0 使用 Size 默认)

### 类型速览
| 类型 | 关键属性 | 用途 |
|------|----------|------|
| NavigationCardModel | `Action OnClick` | 跳转 |
| MarkdownCardModel | `string Markdown` | 文档 / 提示 (可展开) |
| ActionCardModel | `Action OnInvoke` | 按钮执行 |
| SettingCardModel | `object Initial/Pending`, `float? Min/Max`, `string[] Options`, `Persist*` | 简单值设置 |
| BoundSettingCardModel | `Func<object> Getter`, `Action<object> Setter`, `Type ValueType`, `Persist*` | 与外部配置对象绑定 |
| ListSettingCardModel | `string[] InitialValues/PendingValues`, `Separator`, `Persist*` | 字符串集合 |
| ToggleSliderSettingCardModel | `bool Initial/Pending`, `Persist*` | 布尔开关 (迷你样式) |
| TableCardModel | `ITableSchema Schema`, `ITableDataSet DataSet` | 通用表格 |

### Persist 元数据 (可选自动保存)
- `bool Persist` 启用自动保存.
- `string PersistGroup` 分组 (默认 ProviderId). 文件路径：`Mods/<Group>/config.json`.
- `string PersistKey` 键 (默认卡片 Id). 建议使用 `a.b.c` 点式层级.

### 应用流程
- 构建页面 → 用户修改 → 调用 `SettingsApplyService.Apply(models)` → Setter 应用 & Persist 写文件.

---
## 4. 设置应用 & 持久化
```csharp
bool changed = SettingsApplyService.Apply(models); // models = 本页所有 ICardModel
```
- UI 尺寸与主题写入：`SettingsStore.Save()` 自动在 Apply 成功后调用。
- 自定义键值读取：
```csharp
var v = SettingsPersistence.Get<int>("MyMod", "graphics.vsync", 0);
```
- 手动保存任意键集合：`SettingsPersistence.Save(group, IDictionary<string,object>)`。

---
## 5. 表格系统
### Schema
```csharp
public sealed class MySchema : ITableSchema {
  public IReadOnlyList<TableColumn> Columns => _cols; // 定义列
}
// TableColumn 关键字段:
// Id, Title, Kind(TableCellKind: Readonly/Number/Toggle/Dropdown/Text), ValueType,
// WidthHint(float?), ReadOnly(bool), Formatter(Func<object,string>), Validator(Func<object,bool>)
```
### DataSet
```csharp
public sealed class MyData : ITableDataSet {
  public int Count { get; }
  public IRowAdapter GetRow(int index);
  public bool AddNew(); bool RemoveAt(int i); bool Move(int from, int to);
  public bool Commit(); bool Reload(); bool IsDirty { get; }
}
// 行适配: IRowAdapter.Get(string columnId) / Set(string columnId, object value)
```
### 绑定到卡片
```csharp
new TableCardModel { Id="MyMod:Items", Title="Items", Schema=new MySchema(), DataSet=new MyData() };
```
- 默认 Size = XLarge.
- Provider Manager 特殊：移动后内部自动重新编号 order=1..N.

---
## 6. ProviderPreferences (显示/排序持久化)
```csharp
var pref = ProviderPreferences.GetOrCreate(id, title); // Entry: Enabled/Order
ProviderPreferences.Set(id, enabled, order, title);
ProviderPreferences.Save();
var list = ProviderPreferences.BuildOrderedList(ProviderRegistry.All); // 排序+强制 SettingsUI 启用
```
规则：SettingsUI 永不禁用；排序按 `Entry.Order` 后 Title。

---
## 7. 主题与布局
- 核心字段 (部分)：`ThemeMetrics.WindowWidth / WindowHeight / NavWidth / CardHeight* / InputWidth* / SliderFixedWidth / ScrollSensitivity` 等。
- 加载 & 保存：`SettingsStore.Load()` / `SettingsStore.Save()`；当前配置对象：`SettingsStore.Current`。
- Apply 后调用 `SettingsRefreshUtil.RefreshLayout()` 自动刷新尺寸。

---
## 8. Markdown 支持语法
- 标题: `#`/`##`/`###`/`####` + 空格
- 引用: `> text`
- 列表: `- item`
- 行内代码: `` `code` ``
- 加粗: `**bold**`
- 斜体: `*italic*`
(无外部依赖; 不支持多行代码块/链接自动化，可按需扩展)

---
## 9. 事务 (ItemModKit 集成相关概念)
- 编辑核心字段触发事务开始；页面包含 Commit / Rollback / Refresh 操作卡。
- 回滚优先事务，其次快照重放。

---
## 10. 诊断
- `DebugFlags.TextDiagEnabled`：卡片文本绑定日志。
- `DebugFlags.TableDiagEnabled`：表格重建 / 行更新日志。
(发布构建建议关闭)

---
## 11. 刷新 / 重绘
- `SettingsRefreshUtil.RefreshLayout()`：重新应用窗口与卡片布局。
- 表格内部尺寸变化或数据变化自动触发 `_dirtyFull` 重建。

---
## 12. 扩展点
| 扩展 | 方法 |
|------|------|
| 新卡片种类绑定 | `CardBindingRegistry.RegisterKindHandler(CardKind, Func<ICardModel,GameObject,GameObject>)` |
| 新 Setting 子类型 | `CardBindingRegistry.RegisterSettingSubtypeHandler(Type, Func<ICardModel,GameObject,GameObject>)` |
| 自定义面包屑 | 实现 `IBreadcrumbProvider` 并在 Provider 中暴露 |
| 数据持久化策略 | 使用自定义 DataSet 或扩展 SettingsPersistence | 
| 表格验证 | 利用 `TableColumn.Validator` 返回 false 后自定义样式 (未来: 错误色支持) |

---
## 13. 约束 / 规范
- Provider Id 不含冒号；逻辑页面 Id 格式 `ProviderId:PageId`；Root 页逻辑 Id `ProviderId:Root`。
- Persist 分组默认 ProviderId；避免与系统保留组名 `IMK.SettingsUI` 冲突 (除非刻意写入 UI 自身配置)。
- 不直接持久化临时/一次性操作卡。
- 表格需在 Commit 前保证行数据有效；Provider Manager 行移动后内部自重排无需手动维护 `order`.

---
## 14. 最小接入示例 (组合持久化 + 表格)
```csharp
public sealed class ConfigProvider : ISettingsProvider, INavPageModelProvider {
  public string Id => "Cfg"; public string Title => "Config";
  public IEnumerable<NavItem> GetNavItems(){ yield return new NavItem{ Id="Cfg:Root", Title="Config" }; }
  public IEnumerable<ICardModel> BuildPageModels(string pageId){
    if (pageId=="Root") {
      yield return new MarkdownCardModel{ Id="cfg.header", Title="Config", Markdown="### 配置示例" };
      yield return new BoundSettingCardModel{ Id="Cfg:enable", Title="Enable", Getter=()=>MyState.Enable, Setter=v=>MyState.Enable=(bool)v, ValueType=typeof(bool), Persist=true };
    }
  }
}
```
Apply:
```csharp
// 在用户点击保存 / 应用按钮位置：
SettingsApplyService.Apply(currentModels);
```

---
## 15. 常见问题速查
| 问题 | 解决 |
|------|------|
| Root 页面无卡片显示 | 确认 `BuildPageModels("Root")` 返回非空集合 |
| Persist 未写入文件 | 检查是否调用 `SettingsApplyService.Apply` 且卡片 `Persist=true` 值已变化 |
| Provider 隐藏后无法恢复 | SettingsUI Provider 不可隐藏；其它 Provider 可通过 Provider Manager 重新启用 |
| 表格列宽异常 | 确认列 `WidthHint` 与数据样本是否合理；必要时降低列数量或设定最小宽度 |

---
本文件仅列运行期稳定接口；若添加新入口请同步更新日期与章节。