# IMK.SettingsUI 分层与迁移清单 (规划快照)

此文件用于长期记录当前 UI 框架分层目标、待迁移项与不足。供与其它线程或后续协作参考。

## 目标分层结构

- Core (纯 UI 框架，最小必需):
  - App: `SettingsShell`, `ModBehaviour`
  - Navigation: `NavController`, `NavPane`, `BreadcrumbBar`, `ContentPresenter`, `Frame`, `DragWindowOnBar`
  - Layout: `ItemsControl`, `StackPanelLayout`, `DockPanelLayout`
  - Theme: `ThemeMetrics`, `ThemeColors`
  - Cards: `CardModels`, `CardTemplates`, `MarkdownParser`, `MarkdownExpandable`, `DirtyIndicator`, (未来) DirtyOverlay / Validation
  - Providers: `ProviderRegistry`, `ISettingsProvider`
  - Table: `PublicTableApi`, `TableCardModel`, `SchemaTableController`, `Adapters/*`
  - Settings (核心存储与应用): `SettingsStore`, `SettingsApplyService`, `SettingsRefreshUtil`
  - Serialization (仍被核心使用的工具): `RecordListJsonUtil` (若仅旧演示，后续可移 InternalMods.Legacy)
  - Diagnostics: `DebugFlags`
  - PublicApi: 对外稳定入口

- InternalMods (内置后置模组 / 调试 / 框架自身配置):
  - SettingsPanel: 原 `UiSettingsPageBuilder`
  - Sample: 原 `SampleProvider`
  - AffixDemo: 与 Affix 编辑相关演示逻辑 (如保留的话)
  - ItemModKitPanel: 未来的物品编辑界面 (调用 IMK 工具)

- Adapters (桥接外部工具，仅薄封装):
  - 未来: `Adapters/ItemModKit/*` (物品反射、黑名单过滤、值读写 Facade)

- Samples (教学演示，可条件编译移除):
  - 可移动 `SampleProvider`；或添加更多最小示例文件

## 现有文件分类 (待迁移 / 已在目标位置)

| 文件 | 当前路径 | 分类建议 | 迁移操作 |
|------|----------|----------|----------|
| SettingsShell.cs | App | Core | 保留 |
| ModBehaviour.cs | 根 | Core | 保留 |
| ProviderRegistry.cs | Providers | Core | 保留 |
| ISettingsProvider.cs | Providers | Core | 保留 |
| SampleProvider.cs | Providers | InternalMods.Sample | 改命名空间并移动到 InternalMods/Sample |
| UiSettingsPageBuilder.cs | Settings | InternalMods.SettingsPanel | 移动并改命名空间 |
| SettingsStore.cs | Settings | Core | 保留 |
| SettingsApplyService.cs | Settings | Core | 保留 |
| SettingsRefreshUtil.cs | Settings | Core | 保留 |
| CardModels.cs | Cards | Core | 保留 (标记 RecordListCardModel 为 Obsolete 后桥接 TableCardModel) |
| CardTemplates.cs | Cards | Core | 保留 |
| RecordListTableController.cs | Cards | Deprecated/Legacy | 后续替换为 SchemaTableController 或桥接；移 InternalMods.Legacy |
| MarkdownParser/Expandable.cs | Cards | Core | 保留 |
| DirtyIndicator.cs | App | Core | 保留 (后续扩展到所有卡) |
| ThemeMetrics / ThemeColors.cs | Theme | Core | 保留 |
| Navigation/*.cs | Navigation | Core | 保留 |
| Layout/*.cs | Layout | Core | 保留 |
| Serialization/RecordListJsonUtil.cs | Serialization | Legacy 工具 | 确认使用面后决定是否迁移 |
| Table/*.cs | Table | Core | 新增，继续完善 |
| Diagnostics/DebugFlags.cs | Diagnostics | Core | 保留 |
| PublicApi.cs | 根 | Core | 保留 |

## 计划迁移步骤

1. 创建目录: `InternalMods/SettingsPanel`, `InternalMods/Sample`, `InternalMods/ItemModKitPanel`, `InternalMods/Legacy`。
2. 移动 `SampleProvider.cs` → `InternalMods/Sample/SampleProvider.cs`；命名空间改为 `IMK.SettingsUI.InternalMods.Sample`；在启动时专门注册。
3. 移动 `UiSettingsPageBuilder.cs` → `InternalMods/SettingsPanel/UiSettingsPageBuilder.cs`；命名空间改 `IMK.SettingsUI.InternalMods.SettingsPanel`。
4. 将 `RecordListTableController.cs` 标记 `[Obsolete("Use TableCardModel + SchemaTableController")]` 并迁移到 `InternalMods/Legacy`。
5. 在 `ModBehaviour` 中显式注册 InternalMods Provider；增加配置开关 (如静态 bool 或 ini 条目)。
6. 为空窗口状态添加引导卡片 (Core): 若 ProviderRegistry.Count == 0 显示 MarkdownCard “No providers loaded. Register one or enable internal modules.”。
7. 添加 `Adapters/ItemModKit` 目录与初始 Facade 接口 (占位)。
8. 标记 `RecordListCardModel` 为 `[Obsolete]` 并提供自动转换方法 `ToTableCardModel()`。
9. 扩展 DirtyIndicator: 支持 `TableCardModel` 行 / 单元格变更高亮。
10. 添加验证反馈机制: `TableColumn.Validator` 返回 false 时 InputField 背景改为错误色。核心实现放 Table。

## 未来改进清单 (Backlog)

- Provider 生命周期事件: `ProviderRegistry.OnRegistered`, `OnUnregistered`。
- 验证提示 UI: tooltip 或悬浮红色边框。
- Clipboard 批量导入表格行 (统一 JSON schema)。
- 物品编辑 Facade: 聚合 ReadService / WriteService / Selection 到简化接口。
- 本地化支持: 标题与描述接入外部 localization store。
- 性能标记: 表格行虚拟化调试开关 + 行数预估。
- 样式主题扩展: 错误色、警告色、成功色统一放入 ThemeColors。
- 条件编译符号: `DISABLE_INTERNAL_MODS`, `DISABLE_SAMPLES`。
- 文档: 新增 `ARCHITECTURE.md` 详细说明分层哲学与扩展点。

## 黑名单 / 安全策略 (规划)

- 在 ItemModKit 侧暴露: `IItemEditableMetadata` 包含 AllowedMembers/ReadOnlyMembers/HiddenMembers。
- UI 构建详情页面时仅遍历 AllowedMembers；ReadOnlyMembers 映射为 TableCellKind.Readonly。

## 迁移命名空间示例

```csharp
// 迁移前: namespace IMK.SettingsUI.Providers
// 迁移后: namespace IMK.SettingsUI.InternalMods.Sample
```

## 依赖与耦合检查 (需保持的规则)

- Core 不引用 InternalMods 或 Samples。InternalMods 可引用 Core。
- Adapters 仅引用 Core + 外部库 (如 ItemModKit)，不引用 InternalMods。
- PublicApi 只暴露 Core 类型 (不暴露 InternalMods/Samples)。

## 当前不足 (标记)

- 表格验证反馈缺失 (Backlog #10)。
- 没有 provider 时的空态 UI 未实现 (迁移步骤 #6)。
- 没有统一的 diff/dirty 入口 (DirtyIndicator 仅局部)。
- RecordList 旧实现仍与核心混杂 (迁移步骤 #3/#8)。

## 下一次执行建议

开始执行步骤 1~4，提交为 “refactor(settingsui): namespace layering phase 1”。随后添加空态提示与 Obsolete 标记。

---
(本文件为规划快照；更新时请保持结构并增加日期与修改说明。)
