using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Client.Tests
{
    /// <summary>Brief 0010 QĐ 11: đang giữ W/A/S/D/Q/E thì bỏ qua đào/đặt.</summary>
    public class FlyCameraMoveKeyPlayTests
    {
        [Test]
        public void MoveKeysBlockEditOnlyWhileHeld()
        {
            var kb = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState()); InputSystem.Update();
                Assert.IsFalse(DebugFlyCamera.IsMoveKeyHeld(kb));
                foreach (var k in new[] { Key.W, Key.A, Key.S, Key.D, Key.Q, Key.E })
                {
                    InputSystem.QueueStateEvent(kb, new KeyboardState(k)); InputSystem.Update();
                    Assert.IsTrue(DebugFlyCamera.IsMoveKeyHeld(kb), k.ToString());
                }
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.LeftShift, Key.Digit1)); InputSystem.Update();
                Assert.IsFalse(DebugFlyCamera.IsMoveKeyHeld(kb));
                Assert.IsFalse(DebugFlyCamera.IsMoveKeyHeld(null));
            }
            finally { InputSystem.RemoveDevice(kb); }
        }
    }
}
