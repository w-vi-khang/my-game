using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>World-tech 7d (brief 0017): đào/đặt một cột → đúng một điểm ảnh minimap đổi theo độ cao mới.</summary>
    public class MinimapEditTests
    {
        /// <summary>Cột đất khô đầu tiên (duyệt z rồi x) có mặt ở mép tầng (surface % TierHeight == 0), cao hơn mực nước ≥ 2, ô trên cùng đào được.</summary>
        static bool FindTierEdgeColumn(WorldData w, int waterLevel, out int x, out int z, out int top)
        {
            for (z = 0; z < w.Size.Z; z++)
                for (x = 0; x < w.Size.X; x++)
                {
                    MinimapImage.ColumnInfo(w, x, z, out top, out int depth);
                    if (depth == 0 && top % MinimapImage.TierHeight == 0 && top >= waterLevel + 2 && Cell.IsDiggable(w.Get(x, top, z)))
                        return true;
                }
            x = z = top = -1;
            return false;
        }

        static int DiffPixels(byte[] a, byte[] b)
        {
            int n = 0;
            for (int i = 0; i < a.Length; i += 4)
                if (a[i] != b[i] || a[i + 1] != b[i + 1] || a[i + 2] != b[i + 2] || a[i + 3] != b[i + 3])
                    n++;
            return n;
        }

        [Test]
        public void DigAndPlaceChangeExactlyOnePixel()
        {
            var p = new WorldMapParams();
            var world = WorldMapGenerator.Generate(p, out _);
            var editor = new WorldEditor(world, new PlayerBag(), 1, p.waterLevel);
            Assert.IsTrue(FindTierEdgeColumn(world, p.waterLevel, out int x, out int z, out int top), "no column at the tier edge");
            var before = MinimapImage.Build(world);
            var img = (byte[])before.Clone();
            var material = world.GetMaterial(x, top, z);

            Assert.AreEqual(EditResult.Ok, editor.TryDig(x, top, z));
            Assert.IsTrue(MinimapImage.UpdateColumn(world, img, x, z), "digging the top cell at the tier edge must change color");
            Assert.AreEqual(1, DiffPixels(before, img), "exactly one pixel changes");
            CollectionAssert.AreEqual(MinimapImage.Build(world), img, "image updated per column = rebuilt image");
            int i = (x + world.Size.X * z) * 4;
            uint c = MinimapImage.ColumnColor(world, x, z);
            Assert.AreEqual(c, (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]));
            Assert.IsFalse(MinimapImage.UpdateColumn(world, img, x, z), "calling again changes nothing");

            editor.Bag.Add(material);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, top, z, material));
            Assert.IsTrue(MinimapImage.UpdateColumn(world, img, x, z));
            CollectionAssert.AreEqual(before, img, "placed back → image as before");
        }

        [Test]
        public void DigInsideTierKeepsPixel()
        {
            var p = new WorldMapParams();
            var world = WorldMapGenerator.Generate(p, out _);
            var editor = new WorldEditor(world, new PlayerBag(), 1, p.waterLevel);
            Assert.IsTrue(FindTierEdgeColumn(world, p.waterLevel, out int x, out int z, out int top));
            var img = MinimapImage.Build(world);
            // Đặt thêm 1 ô lên cột ở mép tầng (top % 4 == 0 → top + 1 cùng tầng) → màu không đổi.
            editor.Bag.Add(BlockMaterial.Dirt);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, top + 1, z, BlockMaterial.Dirt));
            Assert.IsFalse(MinimapImage.UpdateColumn(world, img, x, z), "same tier → no pixel change");
            CollectionAssert.AreEqual(MinimapImage.Build(world), img);
        }
    }
}
