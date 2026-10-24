using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Core.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>Cài đặt người chơi + bảng chuỗi lúc chạy (0032): settings.json và lang/ trong persistentDataPath; tiếng Việt gốc ở Resources/Lang/vi.json.</summary>
    public static class GameSettings
    {
        /// <summary>Test đặt thư mục tạm; null → Application.persistentDataPath.</summary>
        public static string RootOverride;
        public static string Root => RootOverride ?? Application.persistentDataPath;
        public static string SettingsPath => Path.Combine(Root, "settings.json");
        public static string LangDir => Path.Combine(Root, "lang");

        static PlayerSettings current;
        static Strings baseText, text;

        public static PlayerSettings Current
        {
            get { if (current == null) Load(); return current; }
        }

        public static Strings BaseText
        {
            get
            {
                if (baseText == null)
                {
                    var asset = Resources.Load<TextAsset>("Lang/vi");
                    if (asset == null) throw new FileNotFoundException("Resources/Lang/vi.json not found");
                    baseText = new Strings(JsonUtility.FromJson<StringFile>(asset.text));
                }
                return baseText;
            }
        }

        public static Strings Text
        {
            get { if (text == null) Load(); return text; }
        }

        public static string LanguageError { get; private set; }
        public static bool Dirty { get; private set; }
        public static event Action Changed;

        public static void Load()
        {
            var s = new PlayerSettings();
            try
            {
                if (File.Exists(SettingsPath))
                    s = JsonUtility.FromJson<PlayerSettings>(File.ReadAllText(SettingsPath)) ?? new PlayerSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SETTINGS could not read settings.json: " + e.Message);
                s = new PlayerSettings();
            }
            s.Clamp();
            current = s;
            Dirty = false;
            ApplyBindings();
            LoadLanguage();
        }

        static void ApplyBindings()
        {
            var asset = InputSystem.actions;
            if (asset == null) return;
            if (!KeyRebind.Load(asset, current.bindingOverrides))
            {
                Debug.LogWarning("KEYS invalid binding overrides in settings.json, using defaults");
                current.bindingOverrides = "";
            }
        }

        /// <summary>Lưu chuỗi override phím (asset đã đổi trước khi gọi; không áp lại).</summary>
        public static void SetBindingOverrides(string json)
        {
            var s = Current.Clone();
            s.bindingOverrides = json ?? "";
            Apply(s);
        }

        static void LoadLanguage()
        {
            string id = current.language;
            if (id == PlayerSettings.DefaultLanguage)
            {
                text = BaseText;
                LanguageError = null;
                return;
            }
            try
            {
                string path = Path.Combine(LangDir, id + ".json");
                var file = JsonUtility.FromJson<StringFile>(File.ReadAllText(path));
                text = new Strings(file, BaseText);
                LanguageError = null;
            }
            catch (Exception e)
            {
                text = BaseText;
                LanguageError = id;
                Debug.LogWarning("LANG could not load " + id + ": " + e.Message);
            }
        }

        public static void Apply(PlayerSettings s)
        {
            if (current == null) Load();
            string before = current.language;
            current = s.Clone();
            current.Clamp();
            if (current.language != before) LoadLanguage();
            Dirty = true;
            Changed?.Invoke();
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Root);
                File.WriteAllText(SettingsPath, JsonUtility.ToJson(Current, true));
                Dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("SETTINGS could not write settings.json: " + e.Message);
            }
        }

        public static List<string> Languages()
        {
            var r = new List<string> { PlayerSettings.DefaultLanguage };
            if (Directory.Exists(LangDir))
            {
                var others = new List<string>();
                foreach (var f in Directory.GetFiles(LangDir, "*.json"))
                {
                    string id = Path.GetFileNameWithoutExtension(f);
                    if (id != PlayerSettings.DefaultLanguage && PlayerSettings.IsValidLanguage(id)) others.Add(id);
                }
                r.AddRange(others);
            }
            r.Sort(StringComparer.Ordinal);
            return r;
        }

        /// <summary>Ghi bảng chuỗi gốc ra lang/vi.json để dịch ("vi" luôn dùng bản trong Resources).</summary>
        public static string ExportBase()
        {
            Directory.CreateDirectory(LangDir);
            string path = Path.Combine(LangDir, "vi.json");
            File.WriteAllText(path, JsonUtility.ToJson(BaseText.ToFile(), true));
            return path;
        }

        public static void ResetForTests(string root)
        {
            RootOverride = root;
            InputSystem.actions?.RemoveAllBindingOverrides();
            current = null;
            text = null;
            baseText = null;
            LanguageError = null;
            Dirty = false;
            Changed = null;
        }
    }
}
