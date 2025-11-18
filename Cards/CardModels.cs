namespace IMK.SettingsUI.Cards
{
    public enum CardKind { Navigation, Setting, Action, Markdown }
    public enum CardSize { Small, Medium, Large, XLarge }
    public interface ICardModel { string Id { get; } string Title { get; } string Desc { get; } CardKind Kind { get; } CardSize Size { get; } float HeightOverride { get; } }
    public abstract class CardModelBase : ICardModel { public string Id { get; set; } public string Title { get; set; } public string Desc { get; set; } public CardKind Kind { get; protected set; } public CardSize Size { get; set; } = CardSize.Medium; public float HeightOverride { get; set; } = 0f; }
    public sealed class NavigationCardModel : CardModelBase { public System.Action OnClick; public NavigationCardModel() { Kind = CardKind.Navigation; } }
    public sealed class MarkdownCardModel : CardModelBase { public string Markdown; public string MarkdownText { get => Markdown; set => Markdown = value; } public MarkdownCardModel() { Kind = CardKind.Markdown; Size = CardSize.Large; } }
    public sealed class ActionCardModel : CardModelBase { public System.Action OnInvoke; public ActionCardModel() { Kind = CardKind.Action; } }
    // Optional persistence flags: when Persist=true, SettingsApplyService will write the changed value to Mods/<PersistGroup>/config.json under PersistKey (defaults to Id)
    public sealed class SettingCardModel : CardModelBase { public object Initial; public object Pending; public float? Min; public float? Max; public string[] Options; public bool Persist; public string PersistGroup; public string PersistKey; public SettingCardModel() { Kind = CardKind.Setting; } }
    public sealed class BoundSettingCardModel : CardModelBase { public System.Func<object> Getter; public System.Action<object> Setter; public System.Type ValueType; public object Pending; public float? Min; public float? Max; public object OriginalValue; public string[] Options; public bool Persist; public string PersistGroup; public string PersistKey; public BoundSettingCardModel() { Kind = CardKind.Setting; } }
    public sealed class ListSettingCardModel : CardModelBase { public string[] InitialValues; public string[] PendingValues; public string Separator = ","; public System.Action<string[]> Setter; public bool Persist; public string PersistGroup; public string PersistKey; public ListSettingCardModel() { Kind = CardKind.Setting; Size = CardSize.Large; } }
    public sealed class ToggleSliderSettingCardModel : CardModelBase { public bool Initial; public bool Pending; public System.Action<bool> Setter; public bool Persist; public string PersistGroup; public string PersistKey; public ToggleSliderSettingCardModel() { Kind = CardKind.Setting; Size = CardSize.Small; } }
}
