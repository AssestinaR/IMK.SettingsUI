using System.Collections.Generic;
using IMK.SettingsUI.Cards;
using IMK.SettingsUI.Theme;

namespace IMK.SettingsUI.InternalMods.SettingsPanel
{
    /// <summary>Internal mod: builds SettingsUI own configuration page.</summary>
    public static class UiSettingsPageBuilder
    {
        public static List<ICardModel> Build()
        {
            var list = new List<ICardModel>();
            list.Add(new MarkdownCardModel{ Id="ui.window.header", Title="Window", Markdown="## Window\n窗口尺寸" });
            list.Add(new SettingCardModel{ Id="ui.window.width", Title="Width", Desc="窗口宽度", Initial=ThemeMetrics.WindowWidth, Pending=ThemeMetrics.WindowWidth });
            list.Add(new SettingCardModel{ Id="ui.window.height", Title="Height", Desc="窗口高度", Initial=ThemeMetrics.WindowHeight, Pending=ThemeMetrics.WindowHeight });
            list.Add(new MarkdownCardModel{ Id="ui.nav.header", Title="Navigation", Markdown="## Navigation\n导航区域" });
            list.Add(new SettingCardModel{ Id="ui.nav.width", Title="Nav Width", Desc="导航宽度", Initial=ThemeMetrics.NavWidth, Pending=ThemeMetrics.NavWidth });
            list.Add(new SettingCardModel{ Id="ui.nav.itemHeight", Title="Item Height", Desc="按钮高度", Initial=ThemeMetrics.NavItemHeight, Pending=ThemeMetrics.NavItemHeight, Min=16, Max=72 });
            list.Add(new SettingCardModel{ Id="ui.nav.itemSpacing", Title="Item Spacing", Desc="按钮间距", Initial=ThemeMetrics.NavItemSpacing, Pending=ThemeMetrics.NavItemSpacing, Min=0, Max=32 });
            list.Add(new MarkdownCardModel{ Id="ui.content.header", Title="Content", Markdown="## Content\n主内容区边距" });
            list.Add(new SettingCardModel{ Id="ui.content.padX", Title="Padding X", Desc="水平内边距", Initial=ThemeMetrics.ContentPaddingX, Pending=ThemeMetrics.ContentPaddingX, Min=0, Max=64 });
            list.Add(new SettingCardModel{ Id="ui.content.padY", Title="Padding Y", Desc="垂直内边距", Initial=ThemeMetrics.ContentPaddingY, Pending=ThemeMetrics.ContentPaddingY, Min=0, Max=64 });
            list.Add(new MarkdownCardModel{ Id="ui.card.header", Title="Cards", Markdown="## Cards\n卡片尺寸" });
            list.Add(new SettingCardModel{ Id="ui.card.h.small", Title="Small Height", Desc="小卡高度", Initial=ThemeMetrics.CardHeightSmall, Pending=ThemeMetrics.CardHeightSmall });
            list.Add(new SettingCardModel{ Id="ui.card.h.medium", Title="Medium Height", Desc="中卡高度", Initial=ThemeMetrics.CardHeightMedium, Pending=ThemeMetrics.CardHeightMedium });
            list.Add(new SettingCardModel{ Id="ui.card.h.large", Title="Large Height", Desc="大卡高度", Initial=ThemeMetrics.CardHeightLarge, Pending=ThemeMetrics.CardHeightLarge });
            list.Add(new SettingCardModel{ Id="ui.card.h.markdown", Title="Markdown Height", Desc="Markdown 卡高度", Initial=ThemeMetrics.CardHeightMarkdown, Pending=ThemeMetrics.CardHeightMarkdown });
            list.Add(new SettingCardModel{ Id="ui.card.padX", Title="Card Pad X", Desc="卡水平内边距", Initial=ThemeMetrics.CardPaddingX, Pending=ThemeMetrics.CardPaddingX });
            list.Add(new SettingCardModel{ Id="ui.card.padY", Title="Card Pad Y", Desc="卡垂直内边距", Initial=ThemeMetrics.CardPaddingY, Pending=ThemeMetrics.CardPaddingY });
            list.Add(new SettingCardModel{ Id="ui.card.font.title", Title="Title Font", Desc="标题字号", Initial=ThemeMetrics.CardTitleFontSize, Pending=ThemeMetrics.CardTitleFontSize, Min=10, Max=32 });
            list.Add(new SettingCardModel{ Id="ui.card.font.desc", Title="Desc Font", Desc="说明字号", Initial=ThemeMetrics.CardDescFontSize, Pending=ThemeMetrics.CardDescFontSize, Min=8, Max=28 });
            list.Add(new SettingCardModel{ Id="ui.scroll.sensitivity", Title="Scroll Speed", Desc="滚动速度", Initial=ThemeMetrics.ScrollSensitivity, Pending=ThemeMetrics.ScrollSensitivity, Min=0.2f, Max=10f });
            list.Add(new SettingCardModel{ Id="ui.slider.fixedW", Title="Slider Width", Desc="滑条宽度", Initial=ThemeMetrics.SliderFixedWidth, Pending=ThemeMetrics.SliderFixedWidth, Min=100, Max=600 });
            return list;
        }
    }
}
