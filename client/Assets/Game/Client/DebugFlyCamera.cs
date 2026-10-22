using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>
    /// Camera bay để thử world M1 (bước 4, user duyệt 2026-10-13): WASD đi, Q/E xuống/lên, giữ chuột giữa để xoay, Shift đi nhanh.
    /// Chỉ dùng cho prototype — camera thật là Cinemachine (controls-ui).
    /// Bước 7b2 (brief 0011): gán terrain → không vào ô đặc, vào được ô nước, trượt dọc vách.
    /// Bước 7b3 (brief 0012): không ra khỏi world — chậm dần trong 2 ô cuối trước mép x/z và trần (đỉnh world + 16 ô).
    /// </summary>
    public class DebugFlyCamera : MonoBehaviour
    {
        public float speed = 10f;
        public float fastMultiplier = 3f;
        public float lookSensitivity = 0.15f;
        /// <summary>World để chặn camera vào ô đặc (brief 0011); null → bay xuyên như cũ (SimView).</summary>
        public TerrainChunkView terrain;
        /// <summary>Nửa cạnh hộp va chạm của camera (ô). Phải lớn hơn khoảng cách từ camera tới góc mặt near clip (near 0,1 → ~0,17).</summary>
        public float collisionRadius = 0.25f;

        float yaw, pitch;

        void Start() => SyncAngles();

        /// <summary>Đọc lại yaw/pitch từ transform — gọi sau khi camera bị đặt từ ngoài (task m1-demo-dong-bang), tránh giật góc ở lần kéo chuột đầu.</summary>
        public void SyncAngles()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        /// <summary>Đang giữ phím di chuyển W/A/S/D/Q/E (brief 0010 QĐ 11).</summary>
        public static bool IsMoveKeyHeld(Keyboard kb) => kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed || kb.qKey.isPressed || kb.eKey.isPressed);

        /// <summary>Dời camera theo delta (toạ độ thế giới); có terrain → không vào ô đặc, trượt dọc vách (brief 0011).</summary>
        public void Fly(Vector3 delta)
        {
            if (terrain == null || terrain.World == null)
            {
                transform.position += delta;
                return;
            }
            var t = terrain.transform;
            var p = t.InverseTransformPoint(transform.position);
            var d = t.InverseTransformVector(delta);
            float x = p.x, y = p.y, z = p.z;
            float dx = d.x, dy = d.y, dz = d.z;
            FlyCameraBounds.LimitDelta(terrain.World.Size, x, y, z, ref dx, ref dy, ref dz);
            FlyCameraCollision.Move(terrain.World, ref x, ref y, ref z, dx, dy, dz, collisionRadius);
            transform.position = t.TransformPoint(new Vector3(x, y, z));
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse != null && mouse.middleButton.isPressed && !MinimapView.IsAnyOpen)
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
            Fly(move * (v * Time.deltaTime));
        }
    }
}
