# IMK.SettingsUI

统一的可扩展设置窗口框架。面向后置模组 (Providers) 提供：
- 导航与面包屑层级 (NavPane + IBreadcrumbProvider)
- 多类型卡片渲染 (Navigation / Markdown / Action / Setting / Table 等)
- 表格编辑系统 (TableCardModel + SchemaTableController 虚拟行 + 列宽测量)
- 自动设置持久化 (Persist 标记 + SettingsPersistence)
- Provider 显示/排序管理 (Provider Manager + ProviderPreferences)
- UI 主题运行期调整与保存 (ThemeMetrics + SettingsStore)
- 事务与回滚集成 (与 ItemModKit 物品编辑页)
- 轻量 Markdown 渲染 (自研 MarkdownParser，已移除 Markdig 依赖)

## 已实现特性概览
1. ProviderRegistry 注册 / 卸载 / 枚举；左侧导航按 Order 排序、Enabled 过滤。
2. Provider Manager 页面：可切换显示、拖动排序 (上下移动后自动顺序重排为 1..N)。SettingsUI Provider 强制永不隐藏。
3. 设置卡片持久化：在卡片上设 `Persist=true`，Apply 时自动写入 `Application.persistentDataPath/Mods/<Group>/config.json`。
4. 表格系统：行高 24 + 表头 26，虚拟化仅创建可见行；支持增删/移动/保存/重载/Toggle/简单数值录入。
5. 面包屑：通过 IBreadcrumbProvider 解析父链，自动隐藏 Root 伪层级。
6. 主题 & 窗口布局：宽高、Padding、字体大小、输入/滑条尺寸可编辑＋保存到 `ui.json`。
7. 物品编辑集成 (ItemModKitPanel)：核心字段编辑触发事务，可 Commit / Rollback；编辑页面链条示例：Inspector > Detail > Modifiers > Edit。
8. Markdown：支持标题 (#/##/###/####)、加粗 **text**、斜体 *text*、行内 `code`、列表 - item、引用 > quote。
9. 错误与诊断：DebugFlags 控制日志；错误页使用 ErrorCardFactory 生成回退卡片。

## 快速开始
```csharp
public sealed class MyProvider : ISettingsProvider, INavPageModelProvider {
    public string Id => "MyMod";            // 唯一 Id (不含冒号)
    public string Title => "My Mod";        // 导航显示名
    public IEnumerable<NavItem> GetNavItems(){
        yield return new NavItem{ Id="MyMod:General", Title="General" };
    }
    public IEnumerable<ICardModel> BuildPageModels(string pageId){
        if (pageId == "Root") return new[]{ new MarkdownCardModel{ Id="mm.root", Title="My Mod", Markdown="### 欢迎" } };
        if (pageId == "General") return new[]{ new BoundSettingCardModel {
            Id="MyMod:EnableFeature", Title="Enable Feature", Getter=()=>Cfg.EnableFeature,
            Setter=v=>Cfg.EnableFeature = System.Convert.ToBoolean(v), ValueType=typeof(bool), Persist=true,
            PersistKey="enable.feature" } };
        return null;
    }
}
// 注册
ProviderRegistry.Register(new MyProvider());
// 初始化与显示
IMK.SettingsUI.App.SettingsShell.Init();
IMK.SettingsUI.App.SettingsShell.Toggle();
```

## 卡片类型
- NavigationCardModel：跳转
- MarkdownCardModel：文档 / 提示 (可展开)
- ActionCardModel：执行操作
- SettingCardModel / BoundSettingCardModel：简单或绑定设置 (支持 Min/Max/Options/Persist)
- ListSettingCardModel：字符串列表 (逗号分隔) 持久化支持
- ToggleSliderSettingCardModel：迷你布尔开关
- TableCardModel：通用表格 (Schema + DataSet)

## 持久化机制 (轻量键值)
- 在设置卡片上标记 `Persist=true` 即启用。
- 分组默认：Id 冒号左侧 ProviderId；若无则使用 `IMK.SettingsUI`。
- 文件位置：`Mods/<Group>/config.json`。
- 应用流程：`SettingsApplyService.Apply(models)` → Setter 写入运行期 → 收集 Persist 值 → SettingsPersistence.Save。

## Provider 显示与排序
- ProviderPreferences 文件：`Mods/IMK.SettingsUI/providers.json`
- 字段：Enabled + Order。
- SettingsUI Provider 强制 enabled 防止无法打开管理界面。
- 行移动后自动重新编号顺序 (1..N)。

## 表格系统要点
- 列宽通过标题与前若干行样本测量；不足时按可用宽度均衡扩展。
- 行虚拟化：仅保留可见 + 预取行数量，减少 GameObject 开销。
- 按需重建：尺寸变化或数据集改变触发全重建，其余滚动仅更新内容。

## 主题与布局
- ThemeMetrics 提供窗口宽高、导航宽度、卡片高度 (Small/Medium/Large/XLarge/Markdown)、输入宽度、滚动灵敏度、滑条参数等。
- SettingsStore.Load/Save 维护 `ui.json`；Apply 时写回修改后的尺寸类字段。

## 面包屑
- 实现 IBreadcrumbProvider 可自定义父链与标题。
- Root 隐藏，深层编辑页仍保留返回路径。

## API 摘要
| 入口 | 作用 |
|------|------|
| ProviderRegistry.Register | 注册 Provider |
| SettingsShell.Init / Toggle | 初始化与显示隐藏窗口 |
| NavController.NavigateTo(id) | 导航到指定逻辑页面 |
| SettingsApplyService.Apply(models) | 应用变更 + 持久化 |
| SettingsPersistence.Get<T>(group,key,def) | 读取自定义持久化值 |

## 扩展点
- 新卡片：在 CardBindingRegistry 注册绑定函数。
- 新表格：实现 ITableSchema + ITableDataSet。
- 自定义面包屑：实现 IBreadcrumbProvider。
- 事务扩展：在编辑入口触发事务包装。

## 已移除 / 过时
- Markdig 依赖移除，使用内置 MarkdownParser。
- 老旧 RecordList 系统被 TableCardModel 取代 (若遗留请标记 Obsolete)。

## 路线图 (部分计划)
- 表格单元验证颜色与 Tooltip 错误提示
- 更丰富 Persist 类型（枚举 / 结构体自动展平）
- 全局搜索与过滤、Provider 分组折叠
- 多窗口 / 模态对话框体系
- DirtyIndicator 扩展到表格单元格级别

---
在自己的模组中建议建立一个初始化入口 (如 ModBehaviour.Awake) 注册 Provider；复杂功能可分拆多个 Provider。
