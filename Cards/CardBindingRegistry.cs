using System;
using System.Collections.Generic;

namespace IMK.SettingsUI.Cards
{
    /// <summary>
    /// Public registry allowing internal and external mods to register card binding handlers at runtime.
    /// Thread-safe; supports optional override.
    /// </summary>
    public static class CardBindingRegistry
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<CardKind, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject>> _kindHandlers = new Dictionary<CardKind, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject>>();
        private static readonly Dictionary<Type, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject>> _settingSubtypeHandlers = new Dictionary<Type, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject>>();
        private static bool _initialized;

        /// <summary>Ensure default handlers are registered (idempotent).</summary>
        public static void EnsureInit()
        {
            if (_initialized) return;
            lock(_lock)
            {
                if (_initialized) return;
                // Default handlers map directly to CardTemplates internal bind methods.
                _kindHandlers[CardKind.Navigation] = (m, ex)=> CardTemplates.BindNavigation((NavigationCardModel)m, ex);
                _kindHandlers[CardKind.Markdown]   = (m, ex)=> CardTemplates.BindMarkdown((MarkdownCardModel)m, ex); // rich markdown removed
                _kindHandlers[CardKind.Action]     = (m, ex)=> CardTemplates.BindAction((ActionCardModel)m, ex);
                _kindHandlers[CardKind.Setting]    = (m, ex)=> CardTemplates.BindSettingStrategy(m, ex);
                // Default setting subtypes
                _settingSubtypeHandlers[typeof(BoundSettingCardModel)] = (m, ex)=> CardTemplates.BindBoundSetting((BoundSettingCardModel)m, ex);
                _settingSubtypeHandlers[typeof(ListSettingCardModel)]  = (m, ex)=> CardTemplates.BindListSetting((ListSettingCardModel)m, ex);
                _settingSubtypeHandlers[typeof(ToggleSliderSettingCardModel)] = (m, ex)=> CardTemplates.BindToggleSlider((ToggleSliderSettingCardModel)m, ex);
                _settingSubtypeHandlers[typeof(IMK.SettingsUI.Table.TableCardModel)] = (m, ex)=> CardTemplates.BindSchemaTable((IMK.SettingsUI.Table.TableCardModel)m, ex);
                _settingSubtypeHandlers[typeof(SettingCardModel)] = (m, ex)=> CardTemplates.BindSetting((SettingCardModel)m, ex);
                _initialized = true;
            }
        }
        /// <summary>Register / replace a CardKind handler. Returns true if applied.</summary>
        public static bool RegisterKindHandler(CardKind kind, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject> handler, bool overrideExisting=false)
        {
            if (handler == null) return false; EnsureInit(); lock(_lock)
            {
                if (_kindHandlers.ContainsKey(kind) && !overrideExisting) return false;
                _kindHandlers[kind] = handler; return true;
            }
        }
        /// <summary>Register / replace a setting subtype handler. Returns true if applied.</summary>
        public static bool RegisterSettingSubtypeHandler(Type subtype, Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject> handler, bool overrideExisting=false)
        {
            if (subtype == null || handler == null) return false; EnsureInit(); lock(_lock)
            {
                if (_settingSubtypeHandlers.ContainsKey(subtype) && !overrideExisting) return false;
                _settingSubtypeHandlers[subtype] = handler; return true;
            }
        }
        /// <summary>Generic version for subtype registration.</summary>
        public static bool RegisterSettingSubtypeHandler<TCard>(Func<TCard, UnityEngine.GameObject, UnityEngine.GameObject> handler, bool overrideExisting=false) where TCard: ICardModel
        {
            if (handler == null) return false; return RegisterSettingSubtypeHandler(typeof(TCard), (m, ex)=> handler((TCard)m, ex), overrideExisting);
        }
        internal static Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject> TryGetKindHandler(CardKind kind)
        { EnsureInit(); lock(_lock){ return _kindHandlers.TryGetValue(kind, out var fn)? fn : null; } }
        internal static Func<ICardModel, UnityEngine.GameObject, UnityEngine.GameObject> TryGetSettingSubtypeHandler(Type t)
        { EnsureInit(); lock(_lock){ return _settingSubtypeHandlers.TryGetValue(t, out var fn)? fn : null; } }
    }
}
