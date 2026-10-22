using System.IO;
using Game.Core.Text;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>Bảng chuỗi tiếng Việt gốc cho test Core: đọc thẳng Client/Resources/Lang/vi.json.</summary>
    public static class ViText
    {
        static Strings cached;

        public static string JsonPath => Path.Combine(Application.dataPath, "Game", "Client", "Resources", "Lang", "vi.json");

        public static Strings Load()
        {
            if (cached == null)
                cached = new Strings(JsonUtility.FromJson<StringFile>(File.ReadAllText(JsonPath)));
            return cached;
        }
    }
}
