using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core.Text;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>Quét mã nguồn: chữ hiển thị phải nằm trong Resources/Lang/vi.json (0032 C), không viết thẳng trong file.</summary>
    public class HardcodedTextScanTests
    {
        static readonly string[] Files =
        {
            "Core/View/SiteStatus.cs", "Core/View/CreatureStatus.cs",
            "Client/SimView.cs", "Client/TerrainEditInput.cs", "Client/PlayerFarmActions.cs",
            "Client/MinimapView.cs", "Client/TimeDial.cs", "Client/SettingsView.cs",
        };

        static readonly string[] SkipLines =
        {
            "Box(", "Stretch(", "NewText(", "AddBlock(", ".name =", "name = ", "Debug.Log", "throw ", "Exception(",
        };

        static readonly Regex Literal = new Regex(@"""(?:[^""\\]|\\.)*""");
        static readonly Regex Vietnamese = new Regex("[À-ỹĐđ]");
        static readonly Regex KeyUse = new Regex(@"(?:\bT|\.Get|Format)\(\s*""([a-z0-9_]+\.[a-z0-9_]+)""");
        static readonly Regex KeyShape = new Regex(@"^[a-z0-9_]+\.[a-z0-9_]+$");

        static string[] Lines(string rel) => File.ReadAllLines(Path.Combine(Application.dataPath, "Game", rel));

        [Test]
        public void NoVietnameseLiterals()
        {
            var bad = new List<string>();
            foreach (var f in Files)
            {
                var lines = Lines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("//", StringComparison.Ordinal)) continue;
                    bool skip = false;
                    foreach (var k in SkipLines)
                        if (lines[i].Contains(k)) { skip = true; break; }
                    if (skip) continue;
                    foreach (Match m in Literal.Matches(lines[i]))
                        if (Vietnamese.IsMatch(m.Value)) bad.Add(f + ":" + (i + 1) + ": " + m.Value);
                }
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void AllUsedKeysExist()
        {
            var vi = ViText.Load();
            var missing = new List<string>();
            foreach (var f in Files)
            {
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
            Assert.AreEqual("Tiếng Việt", file.language);
            for (int i = 0; i < file.entries.Length; i++)
            {
                Assert.IsTrue(KeyShape.IsMatch(file.entries[i].key), file.entries[i].key);
                if (i > 0)
                    Assert.Less(string.CompareOrdinal(file.entries[i - 1].key, file.entries[i].key), 0, "Không sắp Ordinal: " + file.entries[i].key);
            }
        }

        [Test]
        public void ReachInText()
        {
            Assert.IsTrue(ViText.Load().Format("farm.sow_far", 6f).Contains("6 ô"));
            Assert.IsTrue(ViText.Load().Format("farm.sow_far", 8f).Contains("8 ô"));
        }
    }
}
