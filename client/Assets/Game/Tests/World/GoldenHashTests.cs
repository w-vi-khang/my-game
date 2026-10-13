using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>
    /// Mã băm chuẩn của world M1 (seed + thông số trong design/data/world_m1.json).
    /// Đổi thuật toán sinh làm số này đổi → tăng generatorVersion và ghi mã băm mới (world-tech "Lưu trữ"); bản cũ vẫn
    /// sinh lại đúng mã băm cũ. Cùng số của phiên bản trong cấu hình phải hiện trên điện thoại (scene HashCheck).
    /// </summary>
    public class GoldenHashTests
    {
        /// <summary>v1 (M1 bước 1) — đã trùng giữa macOS ARM64 và Android 14 ARM64 (2026-10-13).</summary>
        public const string ExpectedV1 = "86b0747d4b2be65b";
        /// <summary>v2 (đảo + vành biển, 0009) — trùng giữa macOS ARM64 và Android 14 ARM64 Xiaomi 24095PCADG (TerrainBench, 2026-10-13).</summary>
        public const string ExpectedV2 = "807d00bd048026a0";

        static string Hash(TerrainParams p) => WorldHash.ToHex(WorldHash.Compute(TerrainGenerator.Generate(p)));

        [Test]
        public void Version1StillMatchesItsGoldenHash()
        {
            var p = new TerrainParams { generatorVersion = 1 };
            string actual = Hash(p);
            TestContext.WriteLine($"world_m1 v1 hash = {actual}");
            Assert.AreEqual(ExpectedV1, actual);
        }

        [Test]
        public void DefaultWorldMatchesGoldenHash()
        {
            var p = new TerrainParams();
            Assert.AreEqual(2, p.generatorVersion);
            string actual = Hash(p);
            TestContext.WriteLine($"world_m1 v2 hash = {actual}");
            Assert.AreEqual(ExpectedV2, actual);
        }
    }
}
