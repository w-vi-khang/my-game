using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Core.Text
{
    /// <summary>Bảng chuỗi ngôn ngữ (0032): khoá → chữ; thiếu khoá → bảng dự phòng (file gốc tiếng Việt) → `[khoá]`.</summary>
    public sealed class Strings
    {
        readonly Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);

        public string Language { get; }
        public Strings Fallback { get; }
        /// <summary>Số khoá của chính bảng này (không tính fallback).</summary>
        public int Count => map.Count;

        public Strings(StringFile file, Strings fallback = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            Language = file.language ?? "";
            Fallback = fallback;
            var entries = file.entries;
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.key))
                    throw new ArgumentException("Khoá rỗng ở dòng " + i + ".");
                if (map.ContainsKey(e.key))
                    throw new ArgumentException("Trùng khoá: " + e.key + ".");
                map[e.key] = e.text ?? "";
            }
        }

        /// <summary>Chỉ bảng của chính nó.</summary>
        public bool Has(string key) => key != null && map.ContainsKey(key);

        public string Get(string key)
        {
            if (key != null && map.TryGetValue(key, out var t)) return t;
            if (Fallback != null) return Fallback.Get(key);
            return "[" + key + "]";
        }

        /// <summary>Thay {0}… bằng InvariantCulture. Mẫu hỏng → thử fallback; không có → trả mẫu chưa thay. Không ném lỗi.</summary>
        public string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try { return string.Format(CultureInfo.InvariantCulture, pattern, args); }
            catch (FormatException)
            {
                if (Fallback != null) return Fallback.Format(key, args);
                return pattern;
            }
        }

        /// <summary>Xuất khoá của chính nó, sắp Ordinal.</summary>
        public StringFile ToFile()
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            var entries = new StringEntry[keys.Count];
            for (int i = 0; i < keys.Count; i++) entries[i] = new StringEntry(keys[i], map[keys[i]]);
            return new StringFile { formatVersion = 1, language = Language, entries = entries };
        }

        /// <summary>Khoá có ở Fallback (chỉ cấp ngay trên) mà bảng này không có, sắp Ordinal.</summary>
        public List<string> MissingKeys()
        {
            var r = new List<string>();
            if (Fallback == null) return r;
            foreach (var k in Fallback.map.Keys)
                if (!map.ContainsKey(k)) r.Add(k);
            r.Sort(StringComparer.Ordinal);
            return r;
        }
    }
}
