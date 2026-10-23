using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Ranh giới + vị trí tên map trên bảng map dựng tay (brief 0015).</summary>
    public class MinimapOverlayTests
    {
        static MapTable Table(int sx, int sz, int mapCount, Func<int, int, byte> id)
        {
            var ids = new byte[sx * sz];
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                    ids[x + sx * z] = id(x, z);
            var names = new string[mapCount];
            for (int i = 0; i < mapCount; i++)
                names[i] = "Map " + (i + 1);
            return new MapTable(sx, sz, ids, names);
        }

        [Test]
        public void TwoMapsGiveOneStraightBorder()
        {
            var t = Table(8, 8, 2, (x, z) => (byte)(x < 4 ? 0 : 1));
            var raw = MinimapOverlay.Borders(t, 0);
            Assert.AreEqual(1, raw.Count);
            CollectionAssert.AreEqual(new float[] { 4, 0, 4, 1, 4, 2, 4, 3, 4, 4, 4, 5, 4, 6, 4, 7, 4, 8 }, raw[0]);

            var smooth = MinimapOverlay.Borders(t);
            Assert.AreEqual(1, smooth.Count);
            var s = smooth[0];
            Assert.AreEqual(72, s.Length, "9 points → 18 → 36 points after 2 Chaikin passes");
            for (int i = 0; i < s.Length; i += 2)
                Assert.AreEqual(4f, s[i], 1e-6f, "straight line stays at x = 4");
            Assert.AreEqual(0f, s[1], 1e-6f, "keeps first point");
            Assert.AreEqual(8f, s[s.Length - 1], 1e-6f, "keeps last point");
        }

        [Test]
        public void OceanIsNotABorder()
        {
            var t = Table(8, 8, 1, (x, z) => x < 4 ? (byte)0 : MapTable.Ocean);
            Assert.AreEqual(0, MinimapOverlay.Borders(t).Count);
        }

        [Test]
        public void ThreeMapsMeetAtJunction()
        {
            var t = Table(8, 8, 3, (x, z) => (byte)(z >= 4 ? 2 : x < 4 ? 0 : 1));
            var raw = MinimapOverlay.Borders(t, 0);
            Assert.AreEqual(3, raw.Count);
            CollectionAssert.AreEqual(new float[] { 4, 0, 4, 1, 4, 2, 4, 3, 4, 4 }, raw[0]);
            CollectionAssert.AreEqual(new float[] { 0, 4, 1, 4, 2, 4, 3, 4, 4, 4 }, raw[1]);
            CollectionAssert.AreEqual(new float[] { 4, 4, 5, 4, 6, 4, 7, 4, 8, 4 }, raw[2]);

            foreach (var line in MinimapOverlay.Borders(t))
            {
                bool startsAtJunction = line[0] == 4f && line[1] == 4f;
                bool endsAtJunction = line[line.Length - 2] == 4f && line[line.Length - 1] == 4f;
                Assert.IsTrue(startsAtJunction || endsAtJunction, "after smoothing 3 lines still meet at (4, 4)");
            }
        }

        [Test]
        public void IslandMapGivesClosedLoop()
        {
            var t = Table(8, 8, 2, (x, z) => (byte)(x >= 2 && x <= 5 && z >= 2 && z <= 5 ? 1 : 0));
            var raw = MinimapOverlay.Borders(t, 0);
            Assert.AreEqual(1, raw.Count);
            var r = raw[0];
            Assert.AreEqual(17 * 2, r.Length, "16 edges → 17 points (first point repeated)");
            Assert.AreEqual(2f, r[0]); Assert.AreEqual(2f, r[1]);
            Assert.AreEqual(2f, r[r.Length - 2]); Assert.AreEqual(2f, r[r.Length - 1]);

            var s = MinimapOverlay.Borders(t)[0];
            Assert.AreEqual(65 * 2, s.Length, "17 → 33 → 65 points");
            Assert.AreEqual(s[0], s[s.Length - 2]);
            Assert.AreEqual(s[1], s[s.Length - 1]);
        }

        [Test]
        public void BordersAreDeterministic()
        {
            var t = Table(16, 16, 3, (x, z) => (byte)((x * 7 + z * 3) / 20 % 3));
            var a = MinimapOverlay.Borders(t);
            var b = MinimapOverlay.Borders(t);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                CollectionAssert.AreEqual(a[i], b[i]);
        }

        [Test]
        public void LabelAtCentroidOrNearestColumn()
        {
            // Map 1 là khối 4 × 4 ở giữa, map 0 bao quanh: trọng tâm map 0 = (4, 4) rơi vào map 1 → dùng tâm cột map 0 gần nhất,
            // bằng nhau (khoảng cách² 6,5) → cột gặp trước theo z rồi x = (3, 1).
            var t = Table(8, 8, 3, (x, z) => (byte)(x >= 2 && x <= 5 && z >= 2 && z <= 5 ? 1 : 0));
            var p = MinimapOverlay.LabelPositions(t);
            Assert.AreEqual(6, p.Length);
            Assert.AreEqual(3.5f, p[0], 1e-6f);
            Assert.AreEqual(1.5f, p[1], 1e-6f);
            Assert.AreEqual(4f, p[2], 1e-6f);
            Assert.AreEqual(4f, p[3], 1e-6f);
            Assert.IsTrue(float.IsNaN(p[4]) && float.IsNaN(p[5]), "map 2 has no column → NaN");
        }
    }
}
