using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>
    /// Mã băm chuẩn của world M1 (seed + thông số trong design/data/world_m1.json, generatorVersion 1).
    /// Đổi thuật toán sinh làm số này đổi → tăng generatorVersion và ghi mã băm mới (world-tech "Lưu trữ").
    /// Cùng số này phải hiện trên điện thoại (scene HashCheck) — kiểm tất định giữa máy.
    /// </summary>
    public class GoldenHashTests
    {
        public const string ExpectedV1 = "86b0747d4b2be65b";

        [Test]
        public void DefaultWorldMatchesGoldenHash()
        {
            var p = new TerrainParams();
            Assert.AreEqual(1, p.generatorVersion);
            string actual = WorldHash.ToHex(WorldHash.Compute(TerrainGenerator.Generate(p)));
            TestContext.WriteLine($"world_m1 v{p.generatorVersion} hash = {actual}");
            Assert.AreEqual(ExpectedV1, actual);
        }
    }
}
