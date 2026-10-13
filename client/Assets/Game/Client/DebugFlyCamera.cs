using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>
    /// Camera bay để thử world M1 (bước 4, user duyệt 2026-10-13): WASD đi, Q/E xuống/lên, giữ chuột giữa để xoay, Shift đi nhanh.
    /// Chỉ dùng cho prototype — camera thật là Cinemachine (controls-ui).
    /// </summary>
    public class DebugFlyCamera : MonoBehaviour
    {
        public float speed = 10f;
        public float fastMultiplier = 3f;
        public float lookSensitivity = 0.15f;

        float yaw, pitch;

        void Start()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse != null && mouse.middleButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                yaw += d.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            if (kb == null)
                return;

            var move = Vector3.zero;
            if (kb.wKey.isPressed) move += transform.forward;
            if (kb.sKey.isPressed) move -= transform.forward;
            if (kb.dKey.isPressed) move += transform.right;
            if (kb.aKey.isPressed) move -= transform.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move -= Vector3.up;
            float v = speed * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);
            transform.position += move * (v * Time.deltaTime);
        }
    }
}
