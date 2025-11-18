using IMK.SettingsUI.Cards;

namespace IMK.SettingsUI.Navigation
{
    internal static class ErrorCardFactory
    {
        public static ICardModel CreateError(string providerId, string pageId, string msg)
        {
            string md = $"### Page Error\nProvider: `{providerId}`\n\nPage: `{pageId}`\n\nError: `{msg}`\n\n请检查该 Provider 的 BuildPageModels 实现，或查看日志。";
            return new MarkdownCardModel { Id = $"{providerId}:{pageId}:error", Title = "Error", Markdown = md };
        }
        public static ICardModel CreateEmpty(string providerId, string pageId)
        {
            string md = $"### Empty Page\nProvider: `{providerId}`\n\nPage: `{pageId}`\n\n未返回任何卡片。请在 BuildPageModels 返回至少一个卡片模型（例如 Markdown 说明卡或跳转卡）。";
            return new MarkdownCardModel { Id = $"{providerId}:{pageId}:empty", Title = "Empty", Markdown = md };
        }
    }
}
