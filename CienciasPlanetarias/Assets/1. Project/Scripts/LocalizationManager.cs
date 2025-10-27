using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager instance;

    public string defaultLang = "pt";
    public string filePrefix = "strings"; // strings.{lang}.json

    private Dictionary<string, string> strings = new Dictionary<string, string>();

    public event Action OnLanguageChanged;

    public bool editor_updateToEN = false;
    public bool editor_updateToPT = false;

    void OnValidate()
    {
        if (editor_updateToEN)
        {
            editor_updateToEN = false;
            SetLanguage("en");
        }
        if (editor_updateToPT)
        {
            editor_updateToPT = false;
            SetLanguage("pt");
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        PlayerPrefs.SetString("lang", defaultLang);
        PlayerPrefs.Save();
        LoadLanguage(defaultLang);
    }

    public void SetLanguage(string lang)
    {
        Debug.Log("Setting language to: " + lang);
        PlayerPrefs.SetString("lang", lang);
        LoadLanguage(lang);
        OnLanguageChanged?.Invoke();
        
    }

    public string Get(string key)
    {
        if (strings.TryGetValue(key, out var val)) return val;
        return key;
    }

    private void LoadLanguage(string lang)
    {
        strings.Clear();
        string fileName = filePrefix + "." + lang + ".json";
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var www = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            var op = www.SendWebRequest();
            while (!op.isDone) { }
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load localization file: " + www.error);
                return;
            }
            ParseJson(www.downloadHandler.text);
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError("Localization file not found: " + path);
            return;
        }
        string json = File.ReadAllText(path);
        ParseJson(json);
#endif
    }

    private void ParseJson(string json)
    {
        try
        {
            // Parser simples para chave-valor usando JsonUtility
            // Cria uma classe temporária para armazenar os pares
            var dict = SimpleJsonToDictionary(json);
            if (dict != null)
            {
                strings.Clear();
                foreach (var kvp in dict)
                {
                    strings[kvp.Key] = kvp.Value;
                }
                return;
            }
            Debug.LogError("Formato de JSON de localização inválido ou não reconhecido.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse localization JSON: " + ex.Message);
        }

    }

    // Helper para converter JSON simples em Dictionary
    private static Dictionary<string, string> SimpleJsonToDictionary(string json)
    {
        var dict = new Dictionary<string, string>();
        // Remove chaves externas e espaços
        json = json.Trim();
        if (json.StartsWith("{")) json = json.Substring(1);
        if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);
        // Separa por vírgula, ignora linhas vazias
        var lines = json.Split(',');
        foreach (var line in lines)
        {
            var pair = line.Split(new[] { ':' }, 2);
            if (pair.Length == 2)
            {
                var key = pair[0].Trim().Trim('"');
                var value = pair[1].Trim().Trim('"');
                // Remove possíveis aspas extras
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Substring(1, value.Length - 2);
                dict[key] = value;
            }
        }
        return dict;
    }
}
