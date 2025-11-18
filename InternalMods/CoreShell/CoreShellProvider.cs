using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Providers;
using UnityEngine;

namespace IMK.SettingsUI.InternalMods.CoreShell
{
    /// <summary>
    /// Internal provider hosting the Settings UI home (previously PageFactory) and core system pages.
    /// Id: CoreShell
    /// Pages:
    ///  Root     -> Home / welcome + empty state guidance
    /// </summary>
    public sealed class CoreShellProvider : ISettingsProvider, INavPageModelProvider
    {
        public string Id => "CoreShell";
        public string Title => "Core";
        public IEnumerable<NavItem> GetNavItems()
        {
            // Only expose Root explicitly (optional). Navigation controller auto rewrites provider selection to :Root.
            yield return new NavItem { Id = "CoreShell:Root", Title = "Home" };
        }
        public void BuildPage(string pageId, Transform parent)
        {
            // Legacy path (not used by current NavController but kept for safety)
            var models = BuildPageModels(pageId);
            for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
            if (models != null)
            {
                float y = 0f; const float gap = 8f; foreach (var m in models) { var go = CardTemplates.Bind(m, null); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = new Vector2(0f, -y); y += rt.sizeDelta.y + gap; }
            }
        }
        public IEnumerable<ICardModel> BuildPageModels(string pageId)
        {
            if (string.Equals(pageId, "Root")) return BuildHomeModels();
            return null;
        }
        private List<ICardModel> BuildHomeModels()
        {
            var list = new List<ICardModel>();
            list.Add(new MarkdownCardModel { Id = "home.welcome", Title = "IMK.SettingsUI Home", Markdown = "# IMK.SettingsUI\n\n欢迎使用统一设置窗口。\n\n**说明**:\n- 左侧导航列出已注册的 Provider (后置模组).\n- 面包屑显示层级路径方便返回.\n- 可通过 PublicApi.RegisterProvider 在运行期添加设置面板.\n\n**卡片类型**:\n- Navigation: 跳转\n- Setting: 参数编辑\n- Action: 立即执行\n- Markdown: 文档 / 提示\n", HeightOverride = -1 });
            if (IMK.SettingsUI.Providers.ProviderRegistry.All.Count <= 2) // core + settings panel only
            {
                list.Add(new MarkdownCardModel { Id = "home.empty", Title = "No Providers", Markdown = "### 空状态\n当前没有外部设置 Provider 被注册。\n\n你可以：\n1. 在 ModBehaviour 中调用 PublicApi.RegisterProvider(new MyProvider())。\n2. 启用其他 InternalMods 示例 (SampleProvider 等)。\n3. 编写实现 ISettingsProvider 的类并在 Awake() 注册。\n\n示例代码：\n````csharp\npublic sealed class MyProvider : ISettingsProvider { /* ... */ }\nvoid Awake(){ IMK.SettingsUI.PublicApi.RegisterProvider(new MyProvider()); }\n````\n" });
            }
            return list;
        }
    }
}
