using System;
using System.IO;
using NUnit.Framework;

namespace Game.Client.Tests
{
    /// <summary>Cách ly test khỏi settings.json thật của Editor (0032): mọi test dùng thư mục tạm.</summary>
    [SetUpFixture]
    public class SettingsTestRoot
    {
        public static string Dir;

        [OneTimeSetUp]
        public void Setup()
        {
            Dir = Path.Combine(Path.GetTempPath(), "mygame-test-" + Guid.NewGuid().ToString("N"));
            GameSettings.ResetForTests(Dir);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            try { if (Directory.Exists(Dir)) Directory.Delete(Dir, true); } catch (Exception) { }
            GameSettings.ResetForTests(null);
        }
    }
}
