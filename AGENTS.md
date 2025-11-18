# IMK.SettingsUI Agents Quick Context

目标: 让 AI 代理/助手在最小上下文下快速理解本项目运行结构, 可立即为后置 Mod (Provider) 编写页面、设置卡片、表格与持久化逻辑。
更新时间: 2025-11-18

---
## 1. 核心目的
IMK.SettingsUI 提供一个可扩展的“设置与诊断”窗口, 外部模组通过注册 `ISettingsProvider` 提供导航页面与卡片模型(ICardModel)。系统包含:
- 导航 / 面包屑管理
- 标准卡片种类 (导航, Markdown, 动作, 设置, 表格)
- 设置持久化与应用 (大小/布局/自定义配置)
- 表格通用 Schema + DataSet 模型 (支持增删改移动提交)
- 事务与物品编辑 (ItemModKit 集成)

代理需理解 Provider → Page → Card 模型链, 以及 SettingsApply 与 Persist 流程。

---
## 2. 主要入口点
| 功能 | 代码入口 |
|------|----------|
| 注册 Provider | `PublicApi.RegisterProvider(ISettingsProvider)` 或 `ProviderRegistry.Register()` |
| 构建页面(推荐) | `INavPageModelProvider.BuildPageModels(pageId)` 返回 `IEnumerable<ICardModel>` |
| 导航跳转 | `NavController.NavigateTo(logicalId)` where logicalId=`ProviderId:PageId` |
| 设置应用 | `SettingsApplyService.Apply(List<ICardModel>)` |
| 持久化读/写 | `SettingsPersistence.Get<T>(group,key,def)` / `SettingsPersistence.Save(group, map)` |
| UI Metrics 修改 | 写入 `SettingsStore.Current` 后 `SettingsStore.Save()` + `SettingsRefreshUtil.RefreshLayout()` |
| 表格展示 | `TableCardModel { Schema, DataSet }` |
| 自定义卡片绑定 | `CardBindingRegistry.RegisterKindHandler` 或 `RegisterSettingSubtypeHandler` |

---
## 3. Provider 规范
```csharp
public sealed class MyProvider : ISettingsProvider, INavPageModelProvider {
  public string Id => "MyMod"; // 不含 ':'
  public string Title => "My Mod";
  public IEnumerable<NavItem> GetNavItems(){ yield return new NavItem{ Id="MyMod:Root", Title="Home" }; }
  public IEnumerable<ICardModel> BuildPageModels(string pageId){ if(pageId=="Root") return BuildRoot(); return null; }
}
```
- Root 页面 id 固定逻辑: `ProviderId:Root`。
- 若实现旧式 `BuildPage` 仍可兼容; 推荐使用模型接口。

---
## 4. 卡片模型速览
| 类型 | 用途 | 关键字段 |
|------|------|----------|
| `NavigationCardModel` | 跳转 | `OnClick` |
| `MarkdownCardModel` | 文本/说明 | `Markdown` (支持基础语法) |
| `ActionCardModel` | 按钮操作 | `OnInvoke` |
| `SettingCardModel` | 简单值设定 | `Initial/Pending/Options/Min/Max/Persist*` |
| `BoundSettingCardModel` | 绑定外部状态 | `Getter/Setter/ValueType/OriginalValue/Persist*` |
| `ListSettingCardModel` | 字符串集合 | `InitialValues/PendingValues/Separator` |
| `ToggleSliderSettingCardModel` | 布尔开关 | `Initial/Pending` |
| `TableCardModel` | 表格 | `Schema/DataSet` |

持久化相关字段 (`SettingCardModel` / `BoundSettingCardModel` 等):
- `Persist = true` 开启保存
- `PersistGroup` 默认 Provider Id
- `PersistKey` 默认 Card Id
保存路径: `Mods/<PersistGroup>/config.json`

---
## 5. 表格系统
定义列(Schema) + 数据(DataSet)。
```csharp
public sealed class MySchema : ITableSchema {
  private readonly List<TableColumn> _cols = new(){
     new TableColumn{ Id="key", Title="Key", Kind=TableCellKind.Text, ValueType=typeof(string) },
     new TableColumn{ Id="value", Title="Value", Kind=TableCellKind.Number, ValueType=typeof(float) }
  };
  public IReadOnlyList<TableColumn> Columns => _cols;
}
public sealed class MyData : ITableDataSet {
  private readonly List<(string key,float value)> _rows = new();
  public int Count => _rows.Count; public bool IsDirty {get; private set;}
  public IRowAdapter GetRow(int i)=> new Adapter(this,i);
  public bool AddNew(){ _rows.Add(("New",0)); IsDirty=true; return true; }
  public bool Commit(){ /* 持久化或写入外部对象 */ IsDirty=false; return true; }
  // ...
}
```
行适配器通过 `Get(string columnId)` / `Set(string columnId, object v)` 提供单元格访问。

---
## 6. 设置应用 & 刷新
- 用户修改 Pending → 调用 `SettingsApplyService.Apply(models)` → 自动写入 `SettingsPersistence`。
- UI 尺寸类配置写入 `SettingsStore.Current` 后需调用 `SettingsStore.Save()` + `SettingsRefreshUtil.RefreshLayout()`。

---
## 7. Markdown 支持
仅基础语法: 标题(#/##/###), 列表(-), 引用(>), 粗体(**), 斜体(*), 行内代码(`code`).
不支持多行代码块 / 图片 / 链接跳转 (链接以普通文字呈现)。

---
## 8. 扩展点
| 扩展 | 说明 |
|------|------|
| 自定义卡片种类 | 注册新的 `CardKind` 绑定函数 |
| Setting 子类型 | 注册 subtype handler 映射到自定义 UI |
| 面包屑链逻辑 | 实现 `IBreadcrumbProvider` 提供层级与标题 |
| 表格验证 | 使用 `TableColumn.Validator` 控制合法性 |

---
## 9. 常见模式示例
### 简单开关持久化
```csharp
new BoundSettingCardModel {
  Id="MyMod:enable",
  Title="Enable",
  Getter=()=> State.Enable,
  Setter=v=> State.Enable = (bool)v,
  ValueType=typeof(bool),
  Persist=true
};
```
### 表格 + 提交
```csharp
var table = new TableCardModel {
  Id="MyMod:items",
  Title="Items",
  Schema=new MySchema(),
  DataSet=new MyData(),
  ShowAddButton=false
};
```
### 应用后刷新布局
```csharp
var models = provider.BuildPageModels("Root").ToList();
if (SettingsApplyService.Apply(models)) {
   SettingsStore.Save();
   SettingsRefreshUtil.RefreshLayout();
}
```

---
## 10. 日志 / 诊断开关
- `DebugFlags.TextDiagEnabled`: 输出卡片绑定与文本渲染日志。
- `DebugFlags.TableDiagEnabled`: 输出表格构建、刷新日志。
建议生产环境关闭以减少性能与日志噪音。

---
## 11. 错误显示
- 未能构建页面: 使用 `ErrorCardFactory.CreateError(providerId, pageId, message)` 生成 Markdown 错误卡。
- 空页面: `ErrorCardFactory.CreateEmpty(...)`。

---
## 12. 事务与物品编辑 (ItemModKit 简述)
当编辑核心字段或使用 BoundSettingCardModel Setter 写入物品属性时:
- 自动启动事务 (BeginTransaction)
- 提交调用: 专用 ActionCardModel (CommitTransactionAndFlush)
- 回滚: RollbackPreferTransaction()

Mod 代理仅需保证 Getter/Setter 正常指向底层对象即可。

---
## 13. 性能注意事项
| 场景 | 建议 |
|------|------|
| 大量 Markdown | 合并为单卡; 避免频繁重建 GameObject |
| 表格频繁刷新 | 减少 Commit 次数, 合并批量修改后统一保存 |
| 大文本输入 | 未来支持底部抽屉 (LargeTextCard) 模式, 暂时保持简短文本 |
| 诊断多日志 | 关闭 DebugFlags 开关 |

---
## 14. 未来计划挂钩 (供 AI 预留)
- Bottom Drawer / LargeText 编辑模式
- Toast / 操作反馈队列
- 统一 LogBus 事件广播 (成功/失败/异常)

代理可在实现新功能时遵守现有配置与扩展点, 不破坏稳定 API。

---
## 15. 快速问答 (AI 应答模板)
| 用户意图 | 应返回 |
|-----------|--------|
| 添加新设置页 | Provider + BuildPageModels 示例代码 |
| 增加布尔配置 | BoundSettingCardModel 样例 |
| 保存并应用布局 | Apply + Save + RefreshLayout 三步 |
| 创建表格编辑数据 | Schema + DataSet + TableCardModel 模板 |
| 自定义卡片渲染 | CardBindingRegistry.RegisterKindHandler 用法 |
| 读取已持久化值 | SettingsPersistence.Get 示例 |

---
## 16. 最小脚手架片段 (复制即可扩展)
```csharp
public static class MyModState { public static bool Enable; }
public sealed class MyModProvider : ISettingsProvider, INavPageModelProvider {
  public string Id => "MyMod"; public string Title => "My Mod";
  public IEnumerable<NavItem> GetNavItems(){ yield return new NavItem{ Id="MyMod:Root", Title="My Mod" }; }
  public IEnumerable<ICardModel> BuildPageModels(string pageId){
    if(pageId=="Root") return new List<ICardModel>{
      new MarkdownCardModel{ Id="my.header", Title="My Mod", Markdown="### My Mod\n示例配置" },
      new BoundSettingCardModel{ Id="my.enable", Title="Enable", Getter=()=>MyModState.Enable, Setter=v=>MyModState.Enable=(bool)v, ValueType=typeof(bool), Persist=true }
    };
    return null;
  }
}
```
注册:
```csharp
PublicApi.RegisterProvider(new MyModProvider());
```

---
## 17. 安全 / 约束总结
- Provider Id 不含 ':'
- PersistGroup 不与系统关键组冲突 (除非刻意写入 UI 配置)
- 卡片 Id 全局唯一建议: `mod.section.name`
- 版本检查: `PublicApi.IsVersionAtLeast("1.0")`

---
此文件用于为 AI 注入最小必要上下文; 若新增抽屉/日志总线等功能需更新 “未来计划” 与相关扩展点说明。
