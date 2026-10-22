using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>Phím debug / bench (F1–F3…): đọc thẳng bàn phím, không qua hành động, không vào bảng đổi phím (0033).</summary>
    public static class DebugKeys
    {
        public static bool WasPressed(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }
    }
}
