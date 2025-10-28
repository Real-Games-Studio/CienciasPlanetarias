using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSetLocalization : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    void Awake()
    {
        ScreenManager.CallScreen += OnScreenCall;

        toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            //OnLanguageChanged();
        }
    }


    void OnDisable()
    {
        ScreenManager.CallScreen -= OnScreenCall;
    }

    void OnScreenCall(string screenName)
    { 
        // Update the toggle state based on the current language
        string currentLang = PlayerPrefs.GetString("lang", "pt");
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(currentLang != "pt");
        }
    }
    
    private void OnToggleValueChanged(bool isOn)
    {
       // Debug.Log("Toggle value changed: " + isOn);
        if (isOn)
        {
            SetLanguageEN();

        }
        else
        {
            SetLanguagePT();
        }
    }

    // Chame este método no botão PT
    public void SetLanguagePT()
    {
        if (LocalizationManager.instance != null)
        {
            Debug.Log("Toggle is ON - Setting language to PT");

            LocalizationManager.instance.SetLanguage("pt");
        }
    }

    // Chame este método no botão EN
    public void SetLanguageEN()
    {
        if (LocalizationManager.instance != null)
        {
            Debug.Log("Toggle is OFF - Setting language to EN");

            LocalizationManager.instance.SetLanguage("en");
        }
    }

}
