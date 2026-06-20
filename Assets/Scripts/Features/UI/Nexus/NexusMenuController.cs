using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TimeAura.Features.Auth;
using TimeAura.Features.Localization;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Localization;
using VContainer;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Handles the side menu visibility and interactions.
    /// Extracted from the NexusController monolith.
    /// </summary>
    public class NexusMenuController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private LocalizationManager _localization;
        [Inject] private AuthManager _authManager;

        private VisualElement _sideMenu;
        private VisualElement _sideMenuDim;
        private Button _menuBtn;
        private List<Button> _langButtons = new();
        private List<Button> _personaButtons = new();

        private Button _btnProfile, _btnAura, _btnSettings, _btnLogout, _btnOrders;
        private Button _btnPersonaMystic, _btnPersonaBusiness, _btnPersonaCasual, _btnPersonaTech;
        private Label _lblMenuOracleVoice;

        public event Action<string> OnMenuItemSelected;

        public void Initialize(VisualElement root)
        {
            _sideMenu = root.Q("SideMenu");
            _sideMenuDim = root.Q("SideMenuDim");
            _menuBtn = root.Q<Button>("MenuBtn");

            _btnProfile = root.Q<Button>("BtnMenuProfile");
            _btnAura = root.Q<Button>("BtnMenuAura");
            _btnSettings = root.Q<Button>("BtnMenuSettings");
            _btnLogout = root.Q<Button>("BtnMenuLogout");
            
            // Or create one dynamically if not in UI yet
            _btnOrders = root.Q<Button>("BtnMenuOrders");
            if (_btnOrders == null && _btnAura != null)
            {
                _btnOrders = new Button { text = "ORDERS" };
                _btnOrders.AddToClassList("menu-btn");
                _btnAura.parent.Insert(_btnAura.parent.IndexOf(_btnAura) + 1, _btnOrders);
            }
            
            _lblMenuOracleVoice = root.Q<Label>(null, "menu-header"); // We'll find it by class or just Q if we add a name

            // Persona Buttons Binding
            if (_btnPersonaMystic == null) _btnPersonaMystic = root.Q<Button>("BtnSidePersonaMystic");
            if (_btnPersonaBusiness == null) _btnPersonaBusiness = root.Q<Button>("BtnSidePersonaBusiness");
            if (_btnPersonaCasual == null) _btnPersonaCasual = root.Q<Button>("BtnSidePersonaCasual");
            if (_btnPersonaTech == null) _btnPersonaTech = root.Q<Button>("BtnSidePersonaTech");

            if (_btnPersonaMystic != null) _btnPersonaMystic.clicked += () => SetPersona(OracleTone.Mystic);
            if (_btnPersonaBusiness != null) _btnPersonaBusiness.clicked += () => SetPersona(OracleTone.Business);
            if (_btnPersonaCasual != null) _btnPersonaCasual.clicked += () => SetPersona(OracleTone.Casual);
            if (_btnPersonaTech != null) _btnPersonaTech.clicked += () => SetPersona(OracleTone.Tech);

            _personaButtons.Add(_btnPersonaMystic);
            _personaButtons.Add(_btnPersonaBusiness);
            _personaButtons.Add(_btnPersonaCasual);
            _personaButtons.Add(_btnPersonaTech);

            var btnClose = root.Q<Button>("BtnCloseMenu");
            
            // Language Buttons Binding
            string[] langCodes = { "UA", "EN", "ES", "FR", "DE", "IT", "PL", "RU", "TR", "HI" };
            foreach (var code in langCodes)
            {
                var btn = root.Q<Button>($"BtnLang{code}");
                if (btn != null)
                {
                    _langButtons.Add(btn);
                    SystemLanguage lang = GetLanguageFromCode(code);
                    btn.clicked += () => SetLanguage(lang);
                }
            }

            if (_menuBtn != null) _menuBtn.clicked += () => ToggleMenu(true);
            if (btnClose != null) btnClose.clicked += () => ToggleMenu(false);
            
            _sideMenuDim?.RegisterCallback<ClickEvent>(e => ToggleMenu(false));

            _btnProfile?.RegisterCallback<ClickEvent>(e => { OnMenuItemSelected?.Invoke("vault"); ToggleMenu(false); });
            _btnAura?.RegisterCallback<ClickEvent>(e => { OnMenuItemSelected?.Invoke("aura"); ToggleMenu(false); });
            _btnOrders?.RegisterCallback<ClickEvent>(e => { OnMenuItemSelected?.Invoke("orders"); ToggleMenu(false); });
            _btnSettings?.RegisterCallback<ClickEvent>(e => { OnMenuItemSelected?.Invoke("settings"); ToggleMenu(false); });
            
            if (_btnLogout != null)
            {
                _btnLogout.clicked += () => {
                    _authManager?.SignOut();
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Initiation");
                };
            }
            
            UpdateLocalization();
        }

        private SystemLanguage GetLanguageFromCode(string code) => code switch
        {
            "UA" => SystemLanguage.Ukrainian,
            "EN" => SystemLanguage.English,
            "ES" => SystemLanguage.Spanish,
            "FR" => SystemLanguage.French,
            "DE" => SystemLanguage.German,
            "IT" => SystemLanguage.Italian,
            "PL" => SystemLanguage.Polish,
            "RU" => SystemLanguage.Russian,
            "TR" => SystemLanguage.Turkish,
            "HI" => SystemLanguage.Hindi,
            _ => SystemLanguage.English
        };

        private void SetLanguage(SystemLanguage lang)
        {
            if (_localization == null) return;
            _localization.SetLanguage(lang);
            UpdateLocalization();
        }

        private void SetPersona(OracleTone tone)
        {
            if (_authManager?.CurrentProfile != null)
            {
                _authManager.CurrentProfile.OracleTone = tone;
                
                // 🎭 NEW: Apply tone to the global localization engine
                if (_localization != null)
                    _localization.SetTone(tone);
                    
                UpdateLocalization();
            }
        }

        public void UpdateLocalization()
        {
            if (_localization == null) return;

            var tone = _localization.CurrentTone;
            if (_btnProfile != null) _btnProfile.text = _localization.GetPersonaString(AuraTerms.MENU_PROFILE, tone, "PROFILE").ToUpper();
            if (_btnAura != null) _btnAura.text = _localization.GetPersonaString(AuraTerms.MENU_AURA, tone, "AURA").ToUpper();
            if (_btnSettings != null) _btnSettings.text = _localization.GetPersonaString(AuraTerms.MENU_SETTINGS, tone, "SETTINGS").ToUpper();
            if (_btnOrders != null) _btnOrders.text = _localization.GetPersonaString(AuraTerms.MENU_ORDERS, tone, "ORDERS").ToUpper();
            if (_btnLogout != null) _btnLogout.text = _localization.GetPersonaString(AuraTerms.MENU_LOGOUT, tone, "LOGOUT").ToUpper();

            // Update Lang Buttons Visuals
            var currentLang = _localization.CurrentLanguage;
            foreach (var btn in _langButtons)
            {
                string code = btn.name.Replace("BtnLang", "");
                SystemLanguage btnLang = GetLanguageFromCode(code);
                btn.EnableInClassList("lang-btn--active", currentLang == btnLang);
            }

            // Update Persona Buttons Visuals
            var currentTone = _localization.CurrentTone; // Use synced tone
            if (_lblMenuOracleVoice != null)
            {
                _lblMenuOracleVoice.text = _localization.GetPersonaString(AuraTerms.MENU_HEADER, currentTone, "NEXUS MENU").ToUpper();
            }

            foreach (var btn in _personaButtons)
            {
                if (btn == null) continue;
                string pId = btn.name.Replace("BtnSidePersona", "").ToLower();
                btn.EnableInClassList("lang-btn--active", currentTone.ToString().ToLower() == pId);
            }
        }

        public void ToggleMenu(bool show)
        {
            if (_sideMenu == null) return;
            
            if (show)
            {
                _sideMenu.RemoveFromClassList("side-menu--hidden");
                _sideMenu.style.display = DisplayStyle.Flex;
            }
            else
            {
                _sideMenu.AddToClassList("side-menu--hidden");
                _sideMenu.style.display = DisplayStyle.None;
            }
        }
    }
}
