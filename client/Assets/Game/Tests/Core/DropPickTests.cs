using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Brief 0037: chọn chồng vật rơi bằng tia.</summary>
    public class DropPickTests
    {
        DropField field;
        DropStack dirt, stone;

        [SetUp]
        public void SetUp()
        {
            PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig { dropScatterRadius = 0f });
            var world = new WorldData(new WorldSize(32, 16, 32));
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                world.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
            field = new DropField(world);
            dirt = field.Add(5, 1, 5, ItemId.Block(BlockMaterial.Dirt), 1);
            stone = field.Add(8, 1, 5, ItemId.Block(BlockMaterial.Stone), 1);
        }

        [TearDown]
        public void TearDown() => PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig());

        [Test]
        public void BoxFollowsScatter()
        {
            PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig());
            DropScatter.Offset(dirt.Id, 0.15f, out float dx, out float dz, out _);
            var hit = DropPick.Pick(field.Stacks, 5.5f + dx, 5f, 5.5f + dz, 0, -1, 0, 100f, 5.5f, 1f, 5.5f, out _);
            Assert.AreSame(dirt, hit);
            var miss = DropPick.Pick(field.Stacks, 5.5f + dx + 0.3f * Math.Sign(dx == 0 ? 1 : dx), 5f, 5.5f + dz, 0, -1, 0, 100f, 5.5f, 1f, 5.5f, out _);
            Assert.IsNull(miss);
        }

        [Test]
        public void RayFromAboveHitsStack()
        {
            var s = DropPick.Pick(field.Stacks, 5.5f, 5f, 5.5f, 0, -1, 0, 100f, 5.5f, 1f, 5.5f, out float d);
            Assert.AreSame(dirt, s);
            Assert.AreEqual(3.5f, d, 1e-4f);
        }

        [Test]
        public void MaxDistanceOccludes() =>
            Assert.IsNull(DropPick.Pick(field.Stacks, 5.5f, 5f, 5.5f, 0, -1, 0, 3.4f, 5.5f, 1f, 5.5f, out _));

        [Test]
        public void OutOfTargetReachIgnored()
        {
            Assert.IsNull(DropPick.Pick(field.Stacks, 5.5f, 5f, 5.5f, 0, -1, 0, 100f, 5.5f, 1f, 17.5f, out _));
            Assert.IsNotNull(DropPick.Pick(field.Stacks, 5.5f, 5f, 5.5f, 0, -1, 0, 100f, 5.5f, 1f, 14.5f, out _));
        }

        [Test]
        public void NearestAlongRayWins()
        {
            var a = DropPick.Pick(field.Stacks, 0f, 1.25f, 5.5f, 1, 0, 0, 100f, 6.5f, 1f, 5.5f, out float da);
            Assert.AreSame(dirt, a);
            Assert.AreEqual(5.25f, da, 1e-4f);
            var b = DropPick.Pick(field.Stacks, 20f, 1.25f, 5.5f, -1, 0, 0, 100f, 6.5f, 1f, 5.5f, out float db);
            Assert.AreSame(stone, b);
            Assert.AreEqual(11.25f, db, 1e-4f);
        }

        [Test]
        public void MissBesideBox() =>
            Assert.IsNull(DropPick.Pick(field.Stacks, 5.5f, 5f, 5.8f, 0, -1, 0, 100f, 5.5f, 1f, 5.5f, out _));

        [Test]
        public void OriginInsideBox()
        {
            var s = DropPick.Pick(field.Stacks, 5.5f, 1.2f, 5.5f, 1, 0, 0, 100f, 5.5f, 1f, 5.5f, out float d);
            Assert.AreSame(dirt, s);
            Assert.AreEqual(0f, d, 1e-6f);
        }

        [Test]
        public void TieKeepsEarlierStack()
        {
            field.Add(5, 1, 5, ItemId.Block(BlockMaterial.Sand), 1);
            var s = DropPick.Pick(field.Stacks, 5.5f, 5f, 5.5f, 0, -1, 0, 100f, 5.5f, 1f, 5.5f, out _);
            Assert.AreSame(dirt, s);
        }

        [Test]
        public void NullStacksThrows() =>
            Assert.Throws<ArgumentNullException>(() => DropPick.Pick(null, 0, 0, 0, 0, -1, 0, 1f, 0, 0, 0, out _));
    }
}
