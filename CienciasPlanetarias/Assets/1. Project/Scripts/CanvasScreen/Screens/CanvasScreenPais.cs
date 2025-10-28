using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

public class CanvasScreenPais : CanvasScreen
{
    [System.Serializable]
    public class ContinentData
    {
        public string continentName;
        public GameObject backgroundImage;
        [HideInInspector] public List<string> countryScreens = new List<string>();
    }

    private static readonly Dictionary<string, string> CountryToContinent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "screen_eua", "americas" },
        { "screen_mexico", "americas" },
        { "screen_canada", "americas" },
        { "screen_brasil", "americas" },
        { "screen_uruguai", "americas" },

        { "screen_reinoUnido", "europa" },
        { "screen_noruega", "europa" },
        { "screen_paisesBaixos", "europa" },
        { "screen_dinamarca", "europa" },
        { "screen_marDoNorte", "europa" },

        { "screen_bangladesh", "asia" },
        { "screen_india", "asia" },
        { "screen_china", "asia" },
        { "screen_indonesia", "asia" },

        { "screen_uganda", "africa" },
        { "screen_tanzania", "africa" },
        { "screen_quenia", "africa" },
        { "screen_africaSubsariana", "africa" },

        { "screen_australia", "oceania" },
        { "screen_ilhasDoPacifico", "oceania" },
    };

    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text legendText;
    [SerializeField] private List<ContinentData> continents = new List<ContinentData>();
    [SerializeField] private string defaultCountryScreen;

    private readonly Dictionary<string, ContinentData> countryLookup = new Dictionary<string, ContinentData>(StringComparer.OrdinalIgnoreCase);
    private string currentCountryScreen;
    private Coroutine waitLocalizationCoroutine;
    private bool pendingSelfActivation;
    private string cachedScreenName;

    public override void OnValidate()
    {
        base.OnValidate();
        CacheScreenName();
        AutoPopulateContinents();
        BuildLookup();
    }

    private void Awake()
    {
        CacheScreenName();
        AutoPopulateContinents();
        BuildLookup();
    }

    private void Start()
    {
        EnsureLookup();
        if (string.IsNullOrEmpty(currentCountryScreen))
        {
            var fallback = GetFallbackCountryKey();
            if (!string.IsNullOrEmpty(fallback))
            {
                SelectCountryInternal(fallback, false);
            }
            else
            {
                ApplyCurrentCountry();
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        EnsureLookup();
        TryRegisterLocalizationCallback();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (LocalizationManager.instance != null)
        {
            LocalizationManager.instance.OnLanguageChanged -= HandleLanguageChanged;
        }
        if (waitLocalizationCoroutine != null)
        {
            StopCoroutine(waitLocalizationCoroutine);
            waitLocalizationCoroutine = null;
        }
    }

    public override void CallScreenListner(string screenName)
    {
        EnsureLookup();
        var normalized = NormalizeScreenKey(screenName);

        Debug.Log($"CanvasScreenPais: CallScreenListner received '{screenName}', normalized='{normalized}'", this);

        if (pendingSelfActivation && IsOwnScreenName(normalized))
        {
            Debug.Log("CanvasScreenPais: pending self-activation detected, delegating to base.", this);
            pendingSelfActivation = false;
            base.CallScreenListner(screenName);
            return;
        }

        if (!string.IsNullOrEmpty(normalized) && TryHandleCountryCall(normalized))
        {
            TurnOn();
            Debug.Log($"CanvasScreenPais: handled country call for '{normalized}'.", this);
            return;
        }

        Debug.Log($"CanvasScreenPais: forwarding '{screenName}' to base CallScreenListner.", this);
        base.CallScreenListner(screenName);
    }

    public override void TurnOn()
    {
        base.TurnOn();
        Debug.Log($"CanvasScreenPais: TurnOn invoked. currentCountryScreen='{currentCountryScreen ?? "null"}'", this);
        if (string.IsNullOrEmpty(currentCountryScreen))
        {
            var fallback = GetFallbackCountryKey();
            if (!string.IsNullOrEmpty(fallback))
            {
                SelectCountryInternal(fallback, false);
            }
        }
        else
        {
            ApplyCurrentCountry();
        }
    }

    public override void TurnOff()
    {
        base.TurnOff();
        Debug.Log($"CanvasScreenPais: TurnOff invoked. currentCountryScreen='{currentCountryScreen ?? "null"}'", this);
    }

    /// <summary>
    /// Seleciona um país sem alterar a navegação da tela.
    /// </summary>
    public void SelectCountry(string countryScreen)
    {
        EnsureLookup();
        SelectCountryInternal(countryScreen, true);
    }

    /// <summary>
    /// Seleciona o país desejado e chama a tela configurada neste Canvas.
    /// </summary>
    public void ShowCountry(string countryScreen)
    {
        SelectCountry(countryScreen);
        CallScreenByName(data.screenName);
    }

    public override void CallScreenByName(string name)
    {
        EnsureLookup();
        var normalized = NormalizeScreenKey(name);
        Debug.Log($"CanvasScreenPais: CallScreenByName received '{name}', normalized='{normalized}'", this);
        if (!string.IsNullOrEmpty(normalized) && TryHandleCountryCall(normalized))
        {
            Debug.Log($"CanvasScreenPais: handled CallScreenByName for country '{normalized}'.", this);
            return;
        }

        Debug.Log($"CanvasScreenPais: forwarding '{name}' to base CallScreenByName.", this);
        base.CallScreenByName(name);
    }

    private void SelectCountryInternal(string countryScreen, bool logWarnings)
    {
        var normalized = NormalizeScreenKey(countryScreen);
        var canonicalKey = FindCanonicalCountryKey(normalized);
        Debug.Log($"CanvasScreenPais: SelectCountryInternal input='{countryScreen}', normalized='{normalized}', canonical='{canonicalKey ?? "null"}'", this);
        if (string.IsNullOrEmpty(canonicalKey))
        {
            if (logWarnings && !string.IsNullOrEmpty(countryScreen))
            {
                Debug.LogWarning($"CanvasScreenPais: país '{countryScreen}' não está configurado nos continentes.", this);
            }
            canonicalKey = GetFallbackCountryKey();
        }

        if (string.IsNullOrEmpty(canonicalKey))
        {
            currentCountryScreen = null;
            ApplyCurrentCountry();
            return;
        }

        if (currentCountryScreen == canonicalKey)
        {
            Debug.Log($"CanvasScreenPais: '{canonicalKey}' já é o país ativo. Reaplicando conteúdo.", this);
            ApplyCurrentCountry();
            return;
        }

        currentCountryScreen = canonicalKey;
        cachedScreenName = canonicalKey;
        Debug.Log($"CanvasScreenPais: país atualizado para '{currentCountryScreen}'. Aplicando conteúdo.", this);
        ApplyCurrentCountry();
    }

    private void ApplyCurrentCountry()
    {
        UpdateBackgroundForCurrentCountry();
        UpdateTextsForCurrentCountry();
    }

    private void UpdateBackgroundForCurrentCountry()
    {
        ContinentData activeContinent = null;
        if (!string.IsNullOrEmpty(currentCountryScreen))
        {
            countryLookup.TryGetValue(currentCountryScreen, out activeContinent);
        }

        foreach (var continent in continents)
        {
            if (continent == null || continent.backgroundImage == null) continue;

            bool shouldBeActive = continent == activeContinent;
            if (continent.backgroundImage.activeSelf != shouldBeActive)
            {
                continent.backgroundImage.SetActive(shouldBeActive);
            }
        }
    }

    private void UpdateTextsForCurrentCountry()
    {
        if (string.IsNullOrEmpty(currentCountryScreen))
        {
            SetText(subtitleText, string.Empty);
            SetText(descriptionText, string.Empty);
            SetText(legendText, string.Empty);
            return;
        }

        SetText(subtitleText, ResolveLocalizationValue("_Subtitle"));
        SetText(descriptionText, ResolveLocalizationValue("_description"));
        SetText(legendText, ResolveLocalizationValue("_legenda"));
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }

    private string ResolveLocalizationValue(string suffix)
    {
        string key = currentCountryScreen + suffix;
        var manager = LocalizationManager.instance;
        return manager != null ? manager.Get(key) : key;
    }

    private void BuildLookup()
    {
        CacheScreenName();
        countryLookup.Clear();
        AutoPopulateContinents();
        if (continents == null) return;

        foreach (var continent in continents)
        {
            if (continent == null || continent.countryScreens == null) continue;

            foreach (var country in continent.countryScreens)
            {
                var normalized = NormalizeScreenKey(country);
                if (string.IsNullOrEmpty(normalized)) continue;

                if (countryLookup.ContainsKey(normalized))
                {
                    if (countryLookup[normalized] != continent)
                    {
                        Debug.LogWarning($"CanvasScreenPais: chave '{normalized}' está atribuída a mais de um continente.", this);
                    }
                    continue;
                }

                countryLookup.Add(normalized, continent);
            }
        }

        defaultCountryScreen = NormalizeScreenKey(defaultCountryScreen);
        if (!string.IsNullOrEmpty(defaultCountryScreen))
        {
            var canonical = FindCanonicalCountryKey(defaultCountryScreen);
            if (string.IsNullOrEmpty(canonical))
            {
                Debug.LogWarning($"CanvasScreenPais: país padrão '{defaultCountryScreen}' não foi encontrado nos continentes configurados.", this);
            }
            else
            {
                defaultCountryScreen = canonical;
            }
        }
    }

    private void EnsureLookup()
    {
        if (countryLookup.Count == 0)
        {
            BuildLookup();
        }
    }

    private string GetFallbackCountryKey()
    {
        if (!string.IsNullOrEmpty(defaultCountryScreen) && countryLookup.ContainsKey(defaultCountryScreen))
        {
            return defaultCountryScreen;
        }

        foreach (var key in countryLookup.Keys)
        {
            return key;
        }

        return null;
    }

    private string NormalizeScreenKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return key.Trim();
    }

    private string FindCanonicalCountryKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (var existingKey in countryLookup.Keys)
        {
            if (string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return existingKey;
            }
        }
        return null;
    }

    private string GetOwnScreenKey()
    {
        if (!string.IsNullOrEmpty(currentCountryScreen))
        {
            return currentCountryScreen;
        }
        if (string.IsNullOrEmpty(cachedScreenName))
        {
            cachedScreenName = NormalizeScreenKey(data != null ? data.screenName : null);
        }
        return cachedScreenName;
    }

    private bool IsOwnScreenName(string normalizedKey)
    {
        var ownKey = GetOwnScreenKey();
        return !string.IsNullOrEmpty(normalizedKey) &&
               !string.IsNullOrEmpty(ownKey) &&
               string.Equals(normalizedKey, ownKey, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryHandleCountryCall(string normalizedKey)
    {
        var canonical = FindCanonicalCountryKey(normalizedKey);
        if (string.IsNullOrEmpty(canonical) || !countryLookup.ContainsKey(canonical))
        {
            Debug.Log($"CanvasScreenPais: TryHandleCountryCall falhou para '{normalizedKey}' (canonical='{canonical ?? "null"}').", this);
            return false;
        }

        Debug.Log($"CanvasScreenPais: TryHandleCountryCall preparando país '{canonical}'.", this);
        SelectCountryInternal(canonical, false);
        TurnOn();

        if (!string.IsNullOrEmpty(currentCountryScreen))
        {
            ScreenManager.currentScreenName = currentCountryScreen;
            Debug.Log($"CanvasScreenPais: ScreenManager.currentScreenName atualizado para '{currentCountryScreen}'.", this);
        }

        var ownKey = GetOwnScreenKey();
        Debug.Log($"CanvasScreenPais: ownScreen='{ownKey ?? "null"}', data.screenName='{data?.screenName}' current='{ScreenManager.currentScreenName}'.", this);
        if (!string.IsNullOrEmpty(data?.screenName) &&
            !string.IsNullOrEmpty(ownKey) &&
            !string.Equals(normalizedKey, ownKey, StringComparison.OrdinalIgnoreCase) &&
            ScreenManager.currentScreenName != data.screenName)
        {
            Debug.Log($"CanvasScreenPais: solicitando ScreenManager.SetCallScreen('{data.screenName}') para mostrar tela de países.", this);
            pendingSelfActivation = true;
            ScreenManager.SetCallScreen(data.screenName);
        }
        else
        {
            Debug.Log("CanvasScreenPais: nenhuma mudança adicional de tela necessária.", this);
            pendingSelfActivation = false;
        }

        return true;
    }

    private static string NormalizeContinentName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return RemoveDiacritics(name.Trim().ToLowerInvariant());
    }

    private ContinentData FindContinentByNormalizedName(string continentKey)
    {
        if (string.IsNullOrEmpty(continentKey) || continents == null) return null;
        foreach (var continent in continents)
        {
            if (continent == null) continue;
            if (NormalizeContinentName(continent.continentName) == continentKey)
            {
                return continent;
            }
        }
        return null;
    }

    private void AutoPopulateContinents()
    {
        if (continents == null) return;

        bool anyConfigured = false;
        foreach (var continent in continents)
        {
            if (continent == null) continue;
            if (!string.IsNullOrWhiteSpace(continent.continentName) || continent.backgroundImage != null)
            {
                anyConfigured = true;
            }

            if (continent.countryScreens == null)
            {
                continent.countryScreens = new List<string>();
            }
            else
            {
                continent.countryScreens.Clear();
            }
        }

        if (!anyConfigured)
        {
            return;
        }

        foreach (var kvp in CountryToContinent)
        {
            var continent = FindContinentByNormalizedName(kvp.Value);
            if (continent == null)
            {
                Debug.LogWarning($"CanvasScreenPais: nenhum continente configurado com o nome '{kvp.Value}' para receber o país '{kvp.Key}'.", this);
                continue;
            }

            if (!continent.countryScreens.Contains(kvp.Key))
            {
                continent.countryScreens.Add(kvp.Key);
            }
        }
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void HandleLanguageChanged()
    {
        ApplyCurrentCountry();
    }

    private void TryRegisterLocalizationCallback()
    {
        if (LocalizationManager.instance != null)
        {
            LocalizationManager.instance.OnLanguageChanged -= HandleLanguageChanged;
            LocalizationManager.instance.OnLanguageChanged += HandleLanguageChanged;
        }
        else if (waitLocalizationCoroutine == null)
        {
            waitLocalizationCoroutine = StartCoroutine(WaitForLocalizationInstance());
        }
    }

    private IEnumerator WaitForLocalizationInstance()
    {
        while (LocalizationManager.instance == null)
        {
            yield return null;
        }

        LocalizationManager.instance.OnLanguageChanged += HandleLanguageChanged;
        waitLocalizationCoroutine = null;
        HandleLanguageChanged();
    }

    private void CacheScreenName()
    {
        cachedScreenName = NormalizeScreenKey(data != null ? data.screenName : null);
    }
}
