using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Ảnh nền minimap trên world dựng tay (brief 0015).</summary>
    public class MinimapImageTests
    {
        static void Fill(WorldData w, int x, int z, int fromY, int toY, BlockMaterial m)
        {
            for (int y = fromY; y <= toY; y++)
                w.Set(x, y, z, Cell.Make(m, m == BlockMaterial.Water ? Cell.MaxWaterLevel : 0));
        }

        static uint Pixel(byte[] img, WorldData w, int x, int z)
        {
            int i = (x + w.Size.X * z) * 4;
            Assert.AreEqual(255, img[i + 3], $"alpha column ({x}, {z})");
            return (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]);
        }

        [Test]
        public void PalettesHaveDistinctColors()
        {
            Assert.AreEqual(12, MinimapImage.LandTiers.Length);
            Assert.AreEqual(4, MinimapImage.WaterBands.Length);
            var all = new HashSet<uint>();
            foreach (var c in MinimapImage.LandTiers)
                Assert.IsTrue(c <= 0xFFFFFF && all.Add(c), $"land color {c:x6} duplicate or wrong");
            foreach (var c in MinimapImage.WaterBands)
                Assert.IsTrue(c <= 0xFFFFFF && all.Add(c), $"water color {c:x6} duplicate or wrong");
        }

        [Test]
        public void TierAndBandBoundaries()
        {
            var t = MinimapImage.LandTiers;
            var b = MinimapImage.WaterBands;
            Assert.AreEqual(t[0], MinimapImage.LandColor(-1));
            Assert.AreEqual(t[3], MinimapImage.LandColor(15));
            Assert.AreEqual(t[4], MinimapImage.LandColor(16));
            Assert.AreEqual(t[11], MinimapImage.LandColor(47));
            Assert.AreEqual(t[11], MinimapImage.LandColor(60));
            Assert.AreEqual(b[0], MinimapImage.WaterColor(0));
            Assert.AreEqual(b[0], MinimapImage.WaterColor(2));
            Assert.AreEqual(b[1], MinimapImage.WaterColor(3));
            Assert.AreEqual(b[2], MinimapImage.WaterColor(6));
            Assert.AreEqual(b[3], MinimapImage.WaterColor(7));
            Assert.AreEqual(b[3], MinimapImage.WaterColor(30));
        }

        [Test]
        public void ColumnColorFollowsTiersAndWaterDepth()
        {
            var w = new WorldData(new WorldSize(16, 48, 16));
            Fill(w, 0, 0, 0, 20, BlockMaterial.Stone);                                                  // mặt 20 → tầng 5
            Fill(w, 1, 0, 0, 10, BlockMaterial.Stone); Fill(w, 1, 0, 11, 14, BlockMaterial.Water);     // sâu 4 → dải 1
            Fill(w, 2, 0, 0, 10, BlockMaterial.Stone); Fill(w, 2, 0, 11, 11, BlockMaterial.Water);     // sâu 1 → dải 0
            Fill(w, 3, 0, 0, 2, BlockMaterial.Stone); Fill(w, 3, 0, 3, 20, BlockMaterial.Water);       // sâu 18 → dải 3
            Fill(w, 4, 0, 0, 47, BlockMaterial.Stone);                                                  // mặt 47 → tầng 11
            // cột (5, 0) rỗng → tầng 0
            Fill(w, 6, 0, 0, 12, BlockMaterial.Stone); Fill(w, 6, 0, 21, 21, BlockMaterial.Grass);     // vòm: ô rắn cao nhất 21 → tầng 5
            Fill(w, 0, 1, 0, 30, BlockMaterial.Bedrock); Fill(w, 0, 1, 31, 31, BlockMaterial.Sand);    // hàng z = 1: mặt 31 → tầng 7

            MinimapImage.ColumnInfo(w, 1, 0, out int s1, out int d1);
            Assert.AreEqual(10, s1);
            Assert.AreEqual(4, d1);
            MinimapImage.ColumnInfo(w, 5, 0, out int s5, out int d5);
            Assert.AreEqual(-1, s5);
            Assert.AreEqual(0, d5);

            var img = MinimapImage.Build(w);
            Assert.AreEqual(16 * 16 * 4, img.Length);
            var t = MinimapImage.LandTiers;
            var b = MinimapImage.WaterBands;
            Assert.AreEqual(t[5], Pixel(img, w, 0, 0), "surface 20");
            Assert.AreEqual(b[1], Pixel(img, w, 1, 0), "depth 4");
            Assert.AreEqual(b[0], Pixel(img, w, 2, 0), "depth 1");
            Assert.AreEqual(b[3], Pixel(img, w, 3, 0), "depth 18");
            Assert.AreEqual(t[11], Pixel(img, w, 4, 0), "surface 47");
            Assert.AreEqual(t[0], Pixel(img, w, 5, 0), "empty column");
            Assert.AreEqual(t[5], Pixel(img, w, 6, 0), "dome");
            Assert.AreEqual(t[7], Pixel(img, w, 0, 1), "row z = 1");
            Assert.AreEqual(t[0], Pixel(img, w, 15, 15), "empty top-right corner");
        }
    }
}
