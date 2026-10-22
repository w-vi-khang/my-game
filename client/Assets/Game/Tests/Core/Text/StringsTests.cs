using System;
using Game.Core.Text;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class StringsTests
    {
        static StringFile F(string lang, params string[] kv)
        {
            var e = new StringEntry[kv.Length / 2];
            for (int i = 0; i < e.Length; i++) e[i] = new StringEntry(kv[2 * i], kv[2 * i + 1]);
            return new StringFile { language = lang, entries = e };
        }

        [Test]
        public void Get()
        {
            var s = new Strings(F("x", "a", "A"));
            Assert.AreEqual("A", s.Get("a"));
            Assert.AreEqual("[x]", s.Get("x"));
        }

        [Test]
        public void FallbackAndMissingKeys()
        {
            var root = new Strings(F("vi", "a", "A", "b", "B"));
            var tr = new Strings(F("de", "a", "Ä"), root);
            Assert.AreEqual("Ä", tr.Get("a"));
            Assert.AreEqual("B", tr.Get("b"));
            Assert.AreEqual("[c]", tr.Get("c"));
            CollectionAssert.AreEqual(new[] { "b" }, tr.MissingKeys());
            Assert.IsFalse(tr.Has("b"));
            Assert.AreEqual(0, root.MissingKeys().Count);
        }

        [Test]
        public void FormatUsesInvariantCulture()
        {
            var s = new Strings(F("vi", "k", "Mưa: còn {0} giây", "f", "{0:F1}"));
            Assert.AreEqual("Mưa: còn 7 giây", s.Format("k", 7));
            Assert.AreEqual("2.5", s.Format("f", 2.5f));
        }

        [Test]
        public void BrokenFormatFallsBack()
        {
            var root = new Strings(F("vi", "k", "Mưa {0}"));
            var tr = new Strings(F("en", "k", "Rain {1}"), root);
            Assert.AreEqual("Mưa 3", tr.Format("k", 3));
            var alone = new Strings(F("en", "k", "Rain {1}"));
            Assert.AreEqual("Rain {1}", alone.Format("k", 3));
        }

        [Test]
        public void BadFileThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Strings(null));
            Assert.Throws<ArgumentException>(() => new Strings(F("x", "", "A")));
            Assert.Throws<ArgumentException>(() => new Strings(new StringFile { entries = new StringEntry[] { null } }));
            Assert.Throws<ArgumentException>(() => new Strings(F("x", "a", "1", "a", "2")));
            Assert.AreEqual(0, new Strings(new StringFile { entries = null }).Count);
            Assert.AreEqual("", new Strings(new StringFile { entries = new[] { new StringEntry("a", null) } }).Get("a"));
        }

        [Test]
        public void ToFileSortedAndJsonRoundTrip()
        {
            var s = new Strings(F("Tiếng Việt", "b", "Bê", "a", "Ă", "c", "Cờ"));
            var f = s.ToFile();
            Assert.AreEqual(new[] { "a", "b", "c" }, new[] { f.entries[0].key, f.entries[1].key, f.entries[2].key });
            var back = new Strings(JsonUtility.FromJson<StringFile>(JsonUtility.ToJson(f, true)));
            Assert.AreEqual(s.Language, back.Language);
            foreach (var k in new[] { "a", "b", "c" }) Assert.AreEqual(s.Get(k), back.Get(k));
        }

        [Test]
        public void ParsesSampleJson()
        {
            const string json = "{ \"formatVersion\": 1, \"language\": \"Tiếng Việt\", \"entries\": [ { \"key\": \"hud.rain_ready\", \"text\": \"Mưa (R): sẵn sàng\" } ] }";
            var s = new Strings(JsonUtility.FromJson<StringFile>(json));
            Assert.AreEqual("Tiếng Việt", s.Language);
            Assert.AreEqual("Mưa (R): sẵn sàng", s.Get("hud.rain_ready"));
        }
    }
}
