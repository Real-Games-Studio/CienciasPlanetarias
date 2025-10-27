using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        MainEvents.OnChanceLanguage?.Invoke();
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
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();

        int index = 0;
        int length = json.Length;

        void SkipWhitespace()
        {
            while (index < length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        string ParseString()
        {
            if (index >= length || json[index] != '"') throw new FormatException("Expected '\"' at position " + index);
            index++; // skip opening quote
            var sb = new StringBuilder();
            while (index < length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    return sb.ToString();
                }
                if (c == '\\')
                {
                    if (index >= length) throw new FormatException("Invalid escape sequence at end of string.");
                    char esc = json[index++];
                    switch (esc)
                    {
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 > length) throw new FormatException("Incomplete unicode escape.");
                            string hex = json.Substring(index, 4);
                            if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var code))
                            {
                                throw new FormatException("Invalid unicode escape: \\u" + hex);
                            }
                            sb.Append((char)code);
                            index += 4;
                            break;
                        default:
                            throw new FormatException("Invalid escape character: \\" + esc);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new FormatException("Unterminated string literal.");
        }

        var dict = new Dictionary<string, string>();

        SkipWhitespace();
        if (index < length && json[index] == '{')
        {
            index++;
        }
        else
        {
            throw new FormatException("JSON must start with '{'.");
        }

        while (true)
        {
            SkipWhitespace();
            if (index < length && json[index] == '}')
            {
                index++;
                break;
            }

            string key = ParseString();

            SkipWhitespace();
            if (index >= length || json[index] != ':')
            {
                throw new FormatException("Expected ':' after key \"" + key + "\".");
            }
            index++; // skip colon

            SkipWhitespace();

            string value;
            if (index < length && json[index] == '"')
            {
                value = ParseString();
            }
            else
            {
                // Accept non-string values as raw until comma or end.
                var sb = new StringBuilder();
                while (index < length && json[index] != ',' && json[index] != '}')
                {
                    sb.Append(json[index++]);
                }
                value = sb.ToString().Trim();
            }

            dict[key] = value;

            SkipWhitespace();
            if (index < length && json[index] == ',')
            {
                index++;
                continue;
            }
            if (index < length && json[index] == '}')
            {
                index++;
                break;
            }
            SkipWhitespace();
            if (index < length && json[index] == '}')
            {
                index++;
                break;
            }
            if (index >= length)
            {
                break;
            }
            throw new FormatException("Unexpected character at position " + index + ": " + json[index]);
        }

        return dict;
    }
}
