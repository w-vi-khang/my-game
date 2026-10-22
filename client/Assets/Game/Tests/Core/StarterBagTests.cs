using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Brief 0037: chọn ô đặt túi hạt đầu game.</summary>
    public class StarterBagTests
    {
        WorldData world;
        PlayerBag bag;
        WorldEditor editor;
        const float FX = 10.5f, FY = 2f, FZ = 10.5f;

        [TearDown]
        public void TearDown() => PlayerRules.Configure(new Game.Core.Sim.PlayerConfig());

        [Test]
        public void PlaceUsesConfiguredCropAndSeeds()
        {
            PlayerRules.Configure(new Game.Core.Sim.PlayerConfig { starterCrop = 1, starterSeeds = 2 });
            var s = StarterBag.Place(editor, FX, FY, FZ, 0, 1);
            Assert.AreEqual(ItemId.SeedBag(1), s.Item);
            Assert.AreEqual(2, s.Count);
        }

        [SetUp]
        public void SetUp()
        {
            world = new WorldData(new WorldSize(32, 16, 32));
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
            {
                world.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                world.Set(x, 1, z, Cell.Make(BlockMaterial.Dirt));
            }
            bag = new PlayerBag();
            editor = new WorldEditor(world, bag, 2);
        }

        void Spot(int dirX, int dirZ, int ex, int ey, int ez, float fx = FX, float fy = FY, float fz = FZ)
        {
            Assert.IsTrue(StarterBag.TryFindSpot(world, fx, fy, fz, dirX, dirZ, out int x, out int y, out int z));
            Assert.AreEqual((ex, ey, ez), (x, y, z));
        }

        [Test]
        public void SpotThreeAheadOnFlatGround()
        {
            Spot(0, 1, 10, 2, 13);
            var s = StarterBag.Place(editor, FX, FY, FZ, 0, 1);
            Assert.AreEqual(ItemId.SeedBag(0), s.Item);
            Assert.AreEqual(4, s.Count);
            Assert.AreEqual((10, 2, 13), (s.X, s.Y, s.Z));
            Assert.AreEqual(1, editor.Drops.CollectInReach(FX, FY, FZ, bag));
            Assert.AreEqual(4, bag.GetSeeds(0));
        }

        [Test]
        public void DirectionPlusX() => Spot(1, 0, 13, 2, 10);

        [Test]
        public void WaterAheadFallsBackToNearest()
        {
            world.Set(10, 1, 13, Cell.Make(BlockMaterial.Water, 7));
            Spot(0, 1, 10, 2, 9);
        }

        [Test]
        public void TallColumnAheadSkipped()
        {
            for (int y = 2; y <= 4; y++) world.Set(10, y, 13, Cell.Make(BlockMaterial.Stone));
            Spot(0, 1, 10, 2, 9);
        }

        [Test]
        public void HoleOneDownAccepted()
        {
            world.Set(10, 1, 13, Cell.Make(BlockMaterial.Empty));
            Spot(0, 1, 10, 1, 13);
        }

        [Test]
        public void NoSpotReturnsFalse()
        {
            for (int z = 6; z <= 16; z++)
            for (int x = 6; x <= 14; x++)
                world.Set(x, 1, z, Cell.Make(BlockMaterial.Water, 7));
            Assert.IsFalse(StarterBag.TryFindSpot(world, FX, FY, FZ, 0, 1, out _, out _, out _));
            Assert.IsNull(StarterBag.Place(editor, FX, FY, FZ, 0, 1));
            Assert.AreEqual(0, editor.Drops.Stacks.Count);
        }

        [Test]
        public void OutOfBoundsSkipped() => Spot(0, -1, 1, 2, 0, 0.5f, 2f, 0.5f);
    }
}
