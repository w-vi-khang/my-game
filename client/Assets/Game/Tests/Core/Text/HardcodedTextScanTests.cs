using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Core.Text;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>Quét mọi .cs trong client/Assets/Game: chữ tiếng Việt chỉ nằm trong Resources/Lang/vi.json (0032 C, 0049).
    /// Ngoại lệ: dòng gắn // vi-text trong Tests/ và Core/WorldMapParams.cs (chữ hiển thị kỳ vọng, dữ liệu tạm).</summary>
    public class HardcodedTextScanTests
    {
        const string Marker = "// vi-text";

        static readonly Regex Literal = new Regex(@"""(?:[^""\\]|\\.)*""");
        static readonly Regex Vietnamese = new Regex("[\u00C0-\u1EF9\u0110\u0111]");
        static readonly Regex Unaccented = new Regex(@"\b(kho|tui|nha|ruong|cay|khoi|hinh|bien|huong|khong|duoc|cua|gieo|xoi|tuoi|hoach|buoi|binh|minh|hoang|dem)\b", RegexOptions.IgnoreCase);
        static readonly Regex KeyUse = new Regex(@"(?:\bT|\.Get|Format)\(\s*""([a-z0-9_]+\.[a-z0-9_]+)""");
        static readonly Regex KeyShape = new Regex(@"^[a-z0-9_]+\.[a-z0-9_]+$");

        const string ThisFile = "Tests/Core/Text/HardcodedTextScanTests.cs";

        static string Root => Path.Combine(Application.dataPath, "Game");

        static string[] AllFiles()
        {
            string root = Root;
            var list = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToList();
            list.Sort(StringComparer.Ordinal);
            return list.ToArray();
        }

        static string[] Lines(string rel) => File.ReadAllLines(Path.Combine(Root, rel));

        static bool ViTextAllowed(string rel) => rel.StartsWith("Tests/", StringComparison.Ordinal) || rel == "Core/WorldMapParams.cs";

        [Test]
        public void NoVietnameseLiterals()
        {
            var bad = new List<string>();
            foreach (var f in AllFiles())
            {
                var lines = Lines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Trim().StartsWith("//", StringComparison.Ordinal)) continue;
                    bool marked = line.TrimEnd().EndsWith(Marker, StringComparison.Ordinal);
                    bool hit = false;
                    foreach (Match m in Literal.Matches(line))
                    {
                        bool accent = Vietnamese.IsMatch(m.Value);
                        bool plain = f != ThisFile && Unaccented.IsMatch(m.Value);
                        if (!accent && !plain) continue;
                        hit = true;
                        if (marked) continue;
                        bad.Add(f + ":" + (i + 1) + ": " + (accent ? "" : "unaccented Vietnamese: ") + m.Value);
                    }
                    if (marked && !ViTextAllowed(f)) bad.Add(f + ":" + (i + 1) + ": vi-text not allowed here");
                    else if (marked && !hit) bad.Add(f + ":" + (i + 1) + ": unused vi-text");
                }
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void AllUsedKeysExist()
        {
            var vi = ViText.Load();
            var missing = new List<string>();
            foreach (var f in AllFiles())
            {
                if (f.StartsWith("Tests/", StringComparison.Ordinal)) continue;
                string all = string.Join("\n", Lines(f));
                foreach (Match m in KeyUse.Matches(all))
                    if (!vi.Has(m.Groups[1].Value)) missing.Add(f + ": " + m.Groups[1].Value);
            }
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        public void BaseFileValid()
        {
            var file = JsonUtility.FromJson<StringFile>(File.ReadAllText(ViText.JsonPath));
            Assert.DoesNotThrow(() => new Strings(file));
            Assert.AreEqual("Tiếng Việt", file.language); // vi-text
            for (int i = 0; i < file.entries.Length; i++)
            {
                Assert.IsTrue(KeyShape.IsMatch(file.entries[i].key), file.entries[i].key);
                if (i > 0)
                    Assert.Less(string.CompareOrdinal(file.entries[i - 1].key, file.entries[i].key), 0, "Not sorted Ordinal: " + file.entries[i].key);
            }
        }

        [Test]
        public void ReachInText()
        {
            Assert.IsTrue(ViText.Load().Format("farm.sow_far", 6f).Contains("6 ô")); // vi-text
            Assert.IsTrue(ViText.Load().Format("farm.sow_far", 8f).Contains("8 ô")); // vi-text
        }
    }
}
