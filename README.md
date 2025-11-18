# IMK.SettingsUI

高性能可扩展的模组设置前置 UI。提供统一导航、面包屑、卡片化设置项、主题与持久化，供其它模组作为“前置依赖”集成自己的设置页面。

## 目标
- 统一与简洁：所有模组设置在同一窗口导航。
- 零外部依赖：仅使用 Unity UGUI + .NET Standard 2.1。
- 低 GC：卡片重用 (ItemsControl + 动态绑定)，避免频繁 Instantiate。
- 可扩展导航：多层级、动态生成、面包屑跳转与过渡动画。
- 持久化与应用：通过 `SettingsApplyService` 写入 JSON 并即时刷新布局参数。

## 特色功能
- 左侧导航栏 (Providers + NavItems)。
- 顶部面包屑 + 可点击回退。
- 内容区卡片：Navigation / Setting / Action / Markdown 四类。
- 页面过渡：矩形擦除 (左进右退) + 惯性滚轮平滑滚动。
- 主题度量 `ThemeMetrics` + 颜色 `ThemeColors` 可通过设置卡实时调整并保存。
- 动态 10 层嵌套示例 (SampleProvider Level1..Level10)。

## 快速集成 (其它模组)
1. 将 IMK.SettingsUI 标记为前置依赖 (在你的 `info.ini` 中声明)。
2. 启动时调用 `IMK.SettingsUI.App.SettingsShell.Init()` (若未自动)；玩家热键可调用 `SettingsShell.Toggle()`。
3. 实现 `ISettingsProvider`：
```csharp
public sealed class MyProvider : ISettingsProvider {
    public string Id => "MyMod";              // 唯一 ID
    public string Title => "My Mod";          // 导航显示标题
    public IEnumerable<NavItem> GetNavItems(){
        yield return new NavItem{ Id="General", Title="常规" };
        yield return new NavItem{ Id="Advanced", Title="高级" };
    }
    public void BuildPage(string pageId, Transform parent){ /* 构造自定义 UI 组件 (可选) */ }
}
```
4. 注册 Provider：
```csharp
ProviderRegistry.Register(new MyProvider());
```
5. 如果只需标准卡片式设置，不实现 `BuildPage`，而是生成 `ICardModel` 列表并扩展 NavController 逻辑 (示例参考 `NavController` 中 Sample 部分)。

## 动态页面 / 深层导航
- 深度用于决定动画方向：`NavController` 维护 `_depth`。
- 自定义多级：ID 规范 `ProviderId:PageId`，面包屑根据拆分生成链。 

## 设置与持久化
- 统一使用 `SettingCardModel` (Id/Initial/Pending/Min/Max)。
- 用户修改后点击顶栏 Apply 调用：
```csharp
var presenter = ... // ContentPresenter
SettingsApplyService.Apply(presenter.GetModels());
```
- 写入 / 读取：`SettingsStore.Save()` / `SettingsStore.Load()`；扩展字段需在 `UiConfig` + Apply/Save 映射。

## 主题可调参数 (节选)
| 分类 | 字段 | 说明 |
|------|------|------|
| 窗口 | WindowWidth/Height | 主窗口尺寸 |
| 导航 | NavWidth / NavItemHeight / Spacing | 左侧栏尺寸与间距 |
| 内容 | ContentPaddingX/Y | 内容区内边距 |
| 卡片 | CardPaddingX/Y / CardSpacing / CardHeight* | 卡片布局 |
| 字体 | CardTitleFontSize / CardDescFontSize | 文本字号 |
| 输入 | InputWidthSmall/Medium/Large | 输入框宽度 |
| 滑条 | SliderFixedWidth / SliderHandleWidth/Height | 滑条宽与手柄尺寸 |
| 滚动 | ScrollSensitivity | 滚动灵敏度 |

## 扩展 UI 组件
- 自行在 `BuildPage` 中使用 UGUI 创建控件并挂在 `parent`，面包屑与窗口框架不受影响。
- 为统一风格建议字体使用 `ThemeColors.DefaultFont`，背景与颜色参照 `ThemeColors`。

## 生命周期 / 入口
| 方法 | 用途 |
|------|------|
| SettingsShell.Init | 初始化 (事件系统 / Canvas / 导航)。|
| SettingsShell.Toggle | 显示/隐藏窗口。|
| NavController.NavigateTo | 切换页面 (带动画 + 面包屑更新)。|
| ProviderRegistry.Register | 注册第三方设置提供者。|

## 自定义动画 (可选拓展点)
- 当前实现：矩形擦除 + 覆盖层同步遮挡旧页面。
- 可替换：波浪 Mask / Shader Wipe。修改 `ContentPresenter` 中 `_reveal` 与 `_cover` 逻辑即可。

## 性能注意
- 大量卡片场景：优先复用 `GameObject`，不要在 `Bind*` 中创建多余组件。
- 避免每帧大量 `Debug.Log`；调试完成后移除或宏控制。
- 滚轮平滑参数可调：`WheelStep` / `WheelDecay` / 插值系数。

## 版本与兼容建议 (待落地)
- 在 `SettingsStore` 增加 `configVersion` 对旧配置迁移。
- 发布语义版本：首次公共 API 基线设为 `v1.0.0`，后续破坏性修改提升主版本。

## 已知待完善
- Markdown 标题高度可配置化（当前固定 30）。
- 更细粒度卡片池化统计与 GC 诊断。
- 外部主题覆写事件或接口。

## 示例 Provider
- 参考 `SampleProvider`：包含递进 Level1..Level10 用于测试面包屑与过渡动画。

## 许可证 / 使用
- 若开源发布请在此添加 License 声明；当前仓库中尚未附带 License 文件。

---
如需二次封装：推荐在你的模组中封装一个 `MyModSettingsBootstrap` 创建 Provider 并在主 ModBehaviour.Awake/OnEnable 调用。
