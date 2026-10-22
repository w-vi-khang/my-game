using System;

namespace Game.Core.Text
{
    /// <summary>Một dòng của file ngôn ngữ: khoá → chữ.</summary>
    [Serializable]
    public class StringEntry
    {
        public string key;
        public string text;

        public StringEntry() { }
        public StringEntry(string key, string text) { this.key = key; this.text = text; }
    }

    /// <summary>File ngôn ngữ (0032), đọc bằng JsonUtility: { "formatVersion": 1, "language": "Tiếng Việt", "entries": [ { "key", "text" } ] }.</summary>
    [Serializable]
    public class StringFile
    {
        public int formatVersion = 1;
        public string language = "";
        public StringEntry[] entries = new StringEntry[0];
    }
}
