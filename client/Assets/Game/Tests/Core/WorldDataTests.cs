using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class WorldDataTests
    {
        static readonly WorldSize M1 = new WorldSize(64, 32, 64);

        [Test]
        public void M1SizeHas32Chunks()
        {
            Assert.AreEqual(4, M1.ChunksX);
            Assert.AreEqual(2, M1.ChunksY);
            Assert.AreEqual(4, M1.ChunksZ);
            Assert.AreEqual(32, M1.ChunkCount);
            Assert.AreEqual(64 * 32 * 64, M1.CellCount);
        }

        [Test]
        public void IndexRoundTrip()
        {
            for (int i = 0; i < M1.CellCount; i += 997)
            {
                M1.Coords(i, out int x, out int y, out int z);
                Assert.IsTrue(M1.InBounds(x, y, z));
                Assert.AreEqual(i, M1.Index(x, y, z));
            }
            Assert.AreEqual(M1.CellCount - 1, M1.Index(63, 31, 63));
        }

        [Test]
        public void ChunkIndexAtBoundaries()
        {
            Assert.AreEqual(0, M1.ChunkIndex(0, 0, 0));
            Assert.AreEqual(0, M1.ChunkIndex(15, 15, 15));
            Assert.AreEqual(1, M1.ChunkIndex(16, 0, 0));
            Assert.AreEqual(4, M1.ChunkIndex(0, 0, 16));
            Assert.AreEqual(16, M1.ChunkIndex(0, 16, 0));
            Assert.AreEqual(31, M1.ChunkIndex(63, 31, 63));
        }

        [Test]
        public void SizeMustBeMultipleOfChunk()
        {
            Assert.Throws<System.ArgumentException>(() => new WorldSize(60, 32, 64));
            Assert.Throws<System.ArgumentException>(() => new WorldSize(0, 32, 64));
        }

        [Test]
        public void GetSetAndSurface()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(3, 0, 4, Cell.Make(BlockMaterial.Bedrock));
            w.Set(3, 5, 4, Cell.Make(BlockMaterial.Grass));
            w.Set(3, 6, 4, Cell.Make(BlockMaterial.Water, 7));
            Assert.AreEqual(BlockMaterial.Grass, w.GetMaterial(3, 5, 4));
            Assert.AreEqual(5, w.SurfaceHeight(3, 4));
            Assert.AreEqual(-1, w.SurfaceHeight(0, 0));
        }

        [Test]
        public void HashChangesWithAnyCell()
        {
            var a = new WorldData(new WorldSize(16, 16, 16));
            var b = new WorldData(new WorldSize(16, 16, 16));
            Assert.AreEqual(WorldHash.Compute(a), WorldHash.Compute(b));
            b.Set(15, 15, 15, Cell.SetModified(0, true));
            Assert.AreNotEqual(WorldHash.Compute(a), WorldHash.Compute(b));
        }

        [Test]
        public void HashIncludesSize()
        {
            var a = new WorldData(new WorldSize(16, 32, 16));
            var b = new WorldData(new WorldSize(32, 16, 16));
            Assert.AreNotEqual(WorldHash.Compute(a), WorldHash.Compute(b));
        }

    }
}
