using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Game.Client.Tests
{
    /// <summary>0033: hành động project-wide chạy lúc chơi; đổi phím lúc chạy (chuẩn bị bảng đổi phím 7l f).</summary>
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
                Assert.AreEqual("<Mouse>/rightButton", GameInput.UseTool.bindings[0].effectivePath);
                Assert.AreEqual("<Mouse>/leftButton", GameInput.MoveClick.bindings[0].effectivePath);

                GameInput.UseTool.ApplyBindingOverride("<Mouse>/leftButton");
                GameInput.MoveClick.ApplyBindingOverride("<Mouse>/rightButton");
                Assert.AreEqual("<Mouse>/leftButton", GameInput.UseTool.bindings[0].effectivePath);
                Assert.AreEqual("<Mouse>/rightButton", GameInput.MoveClick.bindings[0].effectivePath);
                Assert.Greater(GameInput.UseTool.controls.Count, 0);
                foreach (var c in GameInput.UseTool.controls) Assert.AreEqual("leftButton", c.name);
                foreach (var c in GameInput.MoveClick.controls) Assert.AreEqual("rightButton", c.name);

                GameInput.UseTool.RemoveAllBindingOverrides();
                GameInput.MoveClick.RemoveAllBindingOverrides();
                Assert.AreEqual("<Mouse>/rightButton", GameInput.UseTool.bindings[0].effectivePath);
                foreach (var c in GameInput.UseTool.controls) Assert.AreEqual("rightButton", c.name);
            }
            finally
            {
                GameInput.UseTool.RemoveAllBindingOverrides();
                GameInput.MoveClick.RemoveAllBindingOverrides();
                InputSystem.RemoveDevice(mouse);
                if (!wasEnabled) InputSystem.actions.Disable();
            }
        }
    }
}
