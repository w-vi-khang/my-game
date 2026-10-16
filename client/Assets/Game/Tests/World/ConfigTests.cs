using System.IO;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.World.Tests
{
    public class ConfigTests
    {
        // Application.dataPath = <repo>/client/Assets
        static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "world_m1.json"));
        static string ClientCopyPath => Path.Combine(Application.dataPath, "Game", "Client", "Resources", "Config", "world_m1.json");

        [Test]
        public void ClientCopyMatchesDesignData()
        {
            Assert.IsTrue(File.Exists(SourcePath), SourcePath);
            Assert.IsTrue(File.Exists(ClientCopyPath), ClientCopyPath);
            Assert.AreEqual(File.ReadAllText(SourcePath), File.ReadAllText(ClientCopyPath),
                "Bản trong client khác design/data/world_m1.json — chép lại từ design/data");
        }

        [Test]
        public void SimClientCopyMatchesDesignData()
        {
            var src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "sim_m1.json"));
            var copy = Path.Combine(Application.dataPath, "Game", "Client", "Resources", "Config", "sim_m1.json");
            Assert.IsTrue(File.Exists(src), src);
            Assert.IsTrue(File.Exists(copy), copy);
            Assert.AreEqual(File.ReadAllText(src), File.ReadAllText(copy),
                "Bản trong client khác design/data/sim_m1.json — chép lại từ design/data");
        }

        [Test]
        public void ConfigParsesAndValidates()
        {
            var p = JsonUtility.FromJson<TerrainParams>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(p.Validate);
            Assert.AreEqual(new WorldSize(64, 32, 64), p.Size);
            Assert.AreEqual(2, p.generatorVersion);
        }

        [Test]
        public void ConfigMatchesCodeDefaults()
        {
            // Giá trị mặc định trong TerrainParams phải bằng file — tránh test dùng số khác với game.
            var fromFile = JsonUtility.FromJson<TerrainParams>(File.ReadAllText(SourcePath));
            Assert.AreEqual(JsonUtility.ToJson(new TerrainParams()), JsonUtility.ToJson(fromFile));
        }
    }
}
