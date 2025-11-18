using System;
using UnityEngine;

namespace IMK.SettingsUI.Navigation
{
    /// <summary>
    /// Very simple blocking confirmation modal. Creates an overlay under SettingsUI window and invokes callbacks on button clicks.
    /// </summary>
    public sealed class ConfirmModal : MonoBehaviour
    {
        private static ConfirmModal s_instance;
        private RectTransform _overlay;
        private RectTransform _panel;
        private UnityEngine.UI.Text _title;
        private UnityEngine.UI.Text _message;
        private UnityEngine.UI.Button _btnPrimary;
        private UnityEngine.UI.Button _btnSecondary;
        private UnityEngine.UI.Button _btnCancel;
        private UnityEngine.UI.Text _btnPrimaryText;
        private UnityEngine.UI.Text _btnSecondaryText;
        private UnityEngine.UI.Text _btnCancelText;
        private Action _onPrimary;
        private Action _onSecondary;
        private Action _onCancel;

        public static void Show(string title, string message, string primaryLabel, Action onPrimary, string secondaryLabel, Action onSecondary, string cancelLabel, Action onCancel)
        {
            EnsureInstance();
            s_instance.Bind(title, message, primaryLabel, onPrimary, secondaryLabel, onSecondary, cancelLabel, onCancel);
            s_instance.gameObject.SetActive(true);
        }

        public static void Close()
        {
            if (s_instance != null) s_instance.gameObject.SetActive(false);
        }

        private static void EnsureInstance()
        {
            if (s_instance != null) return;
            var window = GameObject.Find("IMK.SettingsUI.Canvas/Window");
            if (window == null)
            {
                var go = new GameObject("ConfirmModal_Root");
                s_instance = go.AddComponent<ConfirmModal>();
                s_instance.BuildUI(go.transform as RectTransform);
                return;
            }
            var root = new GameObject("ConfirmModal").AddComponent<RectTransform>();
            root.SetParent(window.transform, false);
            s_instance = root.gameObject.AddComponent<ConfirmModal>();
            s_instance.BuildUI(root);
        }

        private void BuildUI(RectTransform root)
        {
            _overlay = root; _overlay.anchorMin = Vector2.zero; _overlay.anchorMax = Vector2.one; _overlay.pivot = new Vector2(0.5f, 0.5f); _overlay.offsetMin = Vector2.zero; _overlay.offsetMax = Vector2.zero;
            var img = _overlay.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0f, 0f, 0f, 0.6f); img.raycastTarget = true;

            // panel
            _panel = new GameObject("Panel").AddComponent<RectTransform>(); _panel.SetParent(_overlay, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f); _panel.anchorMax = new Vector2(0.5f, 0.5f); _panel.pivot = new Vector2(0.5f, 0.5f); _panel.sizeDelta = new Vector2(520f, 240f);
            var pbg = _panel.gameObject.AddComponent<UnityEngine.UI.Image>(); pbg.color = Theme.ThemeColors.ContentBg;

            // title
            var titleGo = new GameObject("Title").AddComponent<RectTransform>(); titleGo.SetParent(_panel, false);
            titleGo.anchorMin = new Vector2(0f, 1f); titleGo.anchorMax = new Vector2(1f, 1f); titleGo.pivot = new Vector2(0.5f, 1f); titleGo.sizeDelta = new Vector2(0f, 48f); titleGo.anchoredPosition = new Vector2(0f, -8f);
            _title = titleGo.gameObject.AddComponent<UnityEngine.UI.Text>(); _title.font = Theme.ThemeColors.DefaultFont; _title.alignment = TextAnchor.UpperLeft; _title.color = Color.white; _title.fontSize = 18;

            // message
            var msgGo = new GameObject("Message").AddComponent<RectTransform>(); msgGo.SetParent(_panel, false);
            msgGo.anchorMin = new Vector2(0f, 0f); msgGo.anchorMax = new Vector2(1f, 1f); msgGo.pivot = new Vector2(0.5f, 0.5f); msgGo.offsetMin = new Vector2(16f, 64f); msgGo.offsetMax = new Vector2(-16f, -64f);
            _message = msgGo.gameObject.AddComponent<UnityEngine.UI.Text>(); _message.font = Theme.ThemeColors.DefaultFont; _message.alignment = TextAnchor.UpperLeft; _message.color = Theme.ThemeColors.TextDim; _message.fontSize = 14; _message.horizontalOverflow = HorizontalWrapMode.Wrap; _message.verticalOverflow = VerticalWrapMode.Truncate;

            // buttons container
            var btns = new GameObject("Buttons").AddComponent<RectTransform>(); btns.SetParent(_panel, false);
            btns.anchorMin = new Vector2(0.5f, 0f); btns.anchorMax = new Vector2(0.5f, 0f); btns.pivot = new Vector2(0.5f, 0f); btns.anchoredPosition = new Vector2(0f, 12f); btns.sizeDelta = new Vector2(480f, 44f);

            _btnPrimary = CreateButton("Primary", btns, new Vector2(-160f, 0f)); _btnPrimaryText = _btnPrimary.GetComponentInChildren<UnityEngine.UI.Text>();
            _btnSecondary = CreateButton("Secondary", btns, new Vector2(0f, 0f)); _btnSecondaryText = _btnSecondary.GetComponentInChildren<UnityEngine.UI.Text>();
            _btnCancel = CreateButton("Cancel", btns, new Vector2(160f, 0f)); _btnCancelText = _btnCancel.GetComponentInChildren<UnityEngine.UI.Text>();

            gameObject.SetActive(false);
        }

        private UnityEngine.UI.Button CreateButton(string name, RectTransform parent, Vector2 pos)
        {
            var go = new GameObject(name).AddComponent<RectTransform>(); go.SetParent(parent, false);
            go.anchorMin = new Vector2(0.5f, 0f); go.anchorMax = new Vector2(0.5f, 0f); go.pivot = new Vector2(0.5f, 0f); go.anchoredPosition = pos; go.sizeDelta = new Vector2(140f, 36f);
            var img = go.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Theme.ThemeColors.NavItem;
            var btn = go.gameObject.AddComponent<UnityEngine.UI.Button>();
            var txtGo = new GameObject("Text").AddComponent<RectTransform>(); txtGo.SetParent(go, false);
            txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one; txtGo.offsetMin = Vector2.zero; txtGo.offsetMax = Vector2.zero;
            var txt = txtGo.gameObject.AddComponent<UnityEngine.UI.Text>(); txt.font = Theme.ThemeColors.DefaultFont; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.fontSize = 14; txt.text = name;
            return btn;
        }

        private void Bind(string title, string message, string primaryLabel, Action onPrimary, string secondaryLabel, Action onSecondary, string cancelLabel, Action onCancel)
        {
            _title.text = string.IsNullOrEmpty(title) ? "Confirm" : title;
            _message.text = message ?? string.Empty;
            _btnPrimaryText.text = string.IsNullOrEmpty(primaryLabel) ? "Commit" : primaryLabel;
            _btnSecondaryText.text = string.IsNullOrEmpty(secondaryLabel) ? "Discard" : secondaryLabel;
            _btnCancelText.text = string.IsNullOrEmpty(cancelLabel) ? "Cancel" : cancelLabel;
            _onPrimary = onPrimary; _onSecondary = onSecondary; _onCancel = onCancel;
            _btnPrimary.onClick.RemoveAllListeners(); _btnSecondary.onClick.RemoveAllListeners(); _btnCancel.onClick.RemoveAllListeners();
            _btnPrimary.onClick.AddListener(() => { SafeClose(); try { _onPrimary?.Invoke(); } catch { } });
            _btnSecondary.onClick.AddListener(() => { SafeClose(); try { _onSecondary?.Invoke(); } catch { } });
            _btnCancel.onClick.AddListener(() => { SafeClose(); try { _onCancel?.Invoke(); } catch { } });
        }
        private void SafeClose() { try { gameObject.SetActive(false); } catch { } }
    }
}
