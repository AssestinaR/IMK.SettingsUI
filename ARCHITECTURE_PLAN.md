# IMK.SettingsUI 架构规划 (更新版)

本文件描述当前已完成的分层、仍在演进的模块以及后续路线图。请在有结构性变更后附加日期与要点。

最后更新: 2025-11-18

## 分层总览

Core 层 (不依赖 InternalMods / 示例):
- App: `SettingsShell`, `ModBehaviour`
- Navigation: `NavController`, `NavPane`, `BreadcrumbBar`, `ContentPresenter`, `DragWindowOnBar`, 过渡 / 错误卡片 `ErrorCardFactory`, `UITransitions`
- Layout: `StackPanelLayout`, `DockPanelLayout`, `ItemsControl`, `AutoWidthInput`
- Theme: `ThemeMetrics`, `ThemeColors`
- Cards: `CardModels`, `CardTemplates`, `MarkdownParser`, `MarkdownExpandable`, `DirtyIndicator`, `CardBindingRegistry`
- Table: `TableCardModel`, `SchemaTableController`, `ITableSchema`, `ITableDataSet`, `Adapters/*`
- Providers: `ISettingsProvider`, `INavPageModelProvider`, `IBreadcrumbProvider`, `ProviderRegistry`
- Provider Preferences: `ProviderPreferences` (Mods/IMK.SettingsUI/providers.json)
- Settings: `SettingsStore`, `SettingsApplyService`, `SettingsApplyRegistry`, `SettingsPersistence`, `SettingsRefreshUtil`
- Diagnostics: `DebugFlags`
- Public API: `PublicApi`

InternalMods (可选择启用):
- SettingsPanel: `SettingsPanelProvider`, `UiSettingsPageBuilder` (窗口/主题自配置 + Provider Manager)
- CoreShell: `CoreShellProvider` (Home / 欢迎页)
- Sample: `SampleProvider` (演示嵌套导航与多级页面)
- ItemModKitPanel: 物品编辑界面 (与 ItemModKit 桥接/事务支持)

## 近期完成要点
- Provider Manager 页面迁移到 SettingsPanel Root 子级；支持启用/排序；自动顺序重排。
- Provider 偏好文件路径规范化: `Mods/IMK.SettingsUI/providers.json`；移除迁移逻辑 (未发布阶段无需兼容旧路径)。
- SettingsUI Provider 强制启用，避免因隐藏无法再访问管理器死锁。
- TableCardModel 默认高度改为 XLarge；无显式尺寸声明时使用 XLarge；Provider 表格亦设为 XLarge。
- 持久化标记 `Persist` 添加到所有 Setting 类型卡片；集中持久化写入 Mods/<Group>/config.json。
- 事务与回滚策略：编辑核心字段会触发事务；Commit/Rollback/Refresh 顺序优先事务后快照。

## 设计约束
- Core 不引用 InternalMods。
- InternalMods 可引用 Core，不反向依赖。
- PublicApi 仅暴露 Core 类型。
- Provider Id 不得含冒号 (用于分隔导航路径)。
- 持久化键建议使用点分层命名 (如 graphics.vsync)。

## 关键数据路径
| 功能 | 路径 |
|------|------|
| UI 尺寸/主题 | Mods/IMK.SettingsUI/ui.json |
| Provider 显示/排序 | Mods/IMK.SettingsUI/providers.json |
| 自定义设置持久化 (Persist) | Mods/<ProviderId>/config.json |
| 表格数据 (JsonFileDataSet) | 自定义传入目录/文件 |

## 表格系统现状
- 列宽预测：标题 + 最多 20 行样本字符长度估算；不足时等比填充剩余宽度。
- 行虚拟化：仅构建可见行 + 2 缓冲行。
- 交互：Add / Remove / Move Up / Move Down / Save / Reload；移动后触发顺序重排 (Provider Manager)。
- 下一步：单元格验证反馈 + Dirty 行高亮 (扩展 DirtyIndicator)。

## Breadcrumb 策略
- Root 隐藏；链条由 IBreadcrumbProvider 提供或基于导航层级推断。
- 动态编辑页追加详细节点 (如 Edit:Modifiers:3)。

## 持久化策略细节
- Apply 阶段收集 Persist 变更一次性写入；按 ProviderGroup 聚合写文件，降低 I/O 次数。
- 默认 Group 取冒号前缀；无冒号或空则使用 IMK.SettingsUI。
- 不覆盖未变更键；读写使用 Newtonsoft.Json 字典结构。

## 待办 / 路线图
短期 (下一阶段):
1. 表格单元格 Validator 错误色与 Tooltip。
2. 表格行 Dirty 标记动画 / 颜色突出。
3. Markdown 支持有序列表 / 链接 (安全过滤) / 代码块 (``` 三引号)。
4. 全局 Provider 搜索框 + 过滤隐藏/禁用状态切换按钮。

中期:
1. 多窗口或弹层模式 (ConfirmModal 扩展)。
2. Persist 类型扩展：枚举、结构化对象自动展平 (key 前缀)。
3. 导航状态保存与恢复 (上次停留页面)。
4. 国际化：卡片 Title/Desc 通过外部 localization store 映射。

长期:
1. Profile 性能面板 (行虚拟化命中率、重建成本)。
2. 主题切换预设 / 导入导出。
3. 权限/黑名单：限制某些 Provider 仅开发模式显示。
4. 插件化验证：动态注册 Validator 套件。

## 过时 / 清理计划
| 项目 | 状态 | 计划 |
|------|------|------|
| RecordList 旧实现 | 部分仍可能存在 | 标记 Obsolete，示例迁移到 TableCardModel |
| 直接 BuildPage 手写 GameObject 的 Provider 页 | 支持但不推荐 | 引导使用 INavPageModelProvider + ICardModel 列表 |

## 质量与调试
- DebugFlags 控制文本/表格诊断日志；发布构建建议默认关闭。
- Provider 注册事件: `ProviderRegistry.OnRegister` / `OnUnregister` 可用于外部追踪。
- 持久化失败仅打印警告，不阻断 Apply 流程。

## 示例分层引用关系
```
InternalMods.SettingsPanel --> Core (Cards/Navigation/Settings/Table)
InternalMods.ItemModKitPanel --> Core + ItemModKit 外部库
SampleProvider --> Core (演示递归导航)
CoreShellProvider --> Core (仅 Markdown/Navigation)
```

## 命名规范建议
- Id: ProviderId:PageId 形式；Root 使用 "Root"；避免多层冒号嵌套过深。
- 持久化 Key: 小写 + 点分隔 (provider.feature.enabled)。
- 表格列 Id: 简短、语义化 (enabled / order / id)。

## 更新记录 (简要)
- 2025-11: 引入 Persist 标记与 SettingsPersistence；Provider Manager 重排逻辑；默认 TableCardModel 尺寸改为 XLarge；移除 Markdig。

---
若进行结构性变更请在“更新记录”补充日期与摘要，并确保 README 同步更新。
