using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Game.Client.Tests
{
    /// <summary>0033: hành động project-wide chạy lúc chơi; đổi phím lúc chạy (chuẩn bị bảng đổi phím M3).</summary>
    public class GameControlsPlayTests
    {
        [Test]
        public void ProjectWideActionsAreGameControls()
        {
            Assert.AreEqual("GameControls", InputSystem.actions.name);
            // Dưới test runner project-wide actions không tự bật (0047 QĐ 9) → tự bật rồi trả lại.
            bool wasEnabled = InputSystem.actions.enabled;
            if (!wasEnabled) InputSystem.actions.Enable();
            try
            {
                Assert.IsTrue(GameInput.UseTool.enabled);
                Assert.IsTrue(GameInput.SettingsClose.enabled);
            }
            finally { if (!wasEnabled) InputSystem.actions.Disable(); }
        }

        [Test]
        public void SwapMouseButtonsByOverride()
        {
            var mouse = InputSystem.AddDevice<Mouse>(); // chắc chắn có chuột để resolve control
            bool wasEnabled = InputSystem.actions.enabled;
            if (!wasEnabled) InputSystem.actions.Enable();
            try
            {
                Assert.AreEqual("<Mouse>/leftButton", GameInput.UseTool.bindings[0].effectivePath);
                Assert.AreEqual("<Mouse>/rightButton", GameInput.Cancel.bindings[0].effectivePath);

                GameInput.UseTool.ApplyBindingOverride("<Mouse>/rightButton");
                GameInput.Cancel.ApplyBindingOverride("<Mouse>/leftButton");
                Assert.AreEqual("<Mouse>/rightButton", GameInput.UseTool.bindings[0].effectivePath);
                Assert.AreEqual("<Mouse>/leftButton", GameInput.Cancel.bindings[0].effectivePath);
                Assert.Greater(GameInput.UseTool.controls.Count, 0);
                foreach (var c in GameInput.UseTool.controls) Assert.AreEqual("rightButton", c.name);
                foreach (var c in GameInput.Cancel.controls) Assert.AreEqual("leftButton", c.name);

                GameInput.UseTool.RemoveAllBindingOverrides();
                GameInput.Cancel.RemoveAllBindingOverrides();
                Assert.AreEqual("<Mouse>/leftButton", GameInput.UseTool.bindings[0].effectivePath);
                foreach (var c in GameInput.UseTool.controls) Assert.AreEqual("leftButton", c.name);
            }
            finally
            {
                GameInput.UseTool.RemoveAllBindingOverrides();
                GameInput.Cancel.RemoveAllBindingOverrides();
                InputSystem.RemoveDevice(mouse);
                if (!wasEnabled) InputSystem.actions.Disable();
            }
        }
    }
}
