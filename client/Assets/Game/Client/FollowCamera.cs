using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>
    /// Camera góc nhìn thứ ba sau lưng nhân vật (world 7h, brief 0035): giữ chuột giữa + kéo xoay quanh nhân vật, lăn chuột thu phóng
    /// (Alt + lăn là chọn loại khối, không thu phóng), SphereCast từ tâm xoay ra sau để không xuyên khối.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public PlayerCharacter target;
        public float distance = 6f, minDistance = 2.5f, maxDistance = 14f;
        public float pitch = 20f, minPitch = -20f, maxPitch = 70f;
        public float yaw;
        public float lookSensitivity = 0.15f;
        public float pivotHeight = 1.6f, castRadius = 0.2f;

        /// <summary>Khoảng cách thực sau khi chống xuyên khối.</summary>
        public float CurrentDistance { get; private set; }

        public void SetAngles(float yawDeg, float pitchDeg)
        {
            yaw = yawDeg;
            pitch = Mathf.Clamp(pitchDeg, minPitch, maxPitch);
        }

        void LateUpdate()
        {
            if (target == null) return;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.middleButton.isPressed && !MinimapView.IsAnyOpen)
                {
                    var d = mouse.delta.ReadValue();
                    yaw += d.x * lookSensitivity;
                    pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, minPitch, maxPitch);
                }
                var kb = Keyboard.current;
                var pos = mouse.position.ReadValue();
                bool alt = kb != null && kb.altKey.isPressed;
                if (!alt && !MinimapView.BlocksPointer(pos) && !TimeDial.BlocksPointer(pos) && !SimView.BlocksPointer(pos))
                {
                    float wheel = mouse.scroll.ReadValue().y;
                    if (wheel > 0.01f) distance *= 0.9f;
                    else if (wheel < -0.01f) distance *= 1.1f;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
            SnapNow();
        }

        /// <summary>Đặt camera ngay theo góc, khoảng cách và nhân vật hiện tại.</summary>
        public void SnapNow()
        {
            if (target == null) return;
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var pivot = target.transform.position + Vector3.up * pivotHeight;
            var back = rot * Vector3.back;
            CurrentDistance = distance;
            if (Physics.SphereCast(pivot, castRadius, back, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                CurrentDistance = Mathf.Max(hit.distance - 0.1f, 0.3f);
            transform.SetPositionAndRotation(pivot + back * CurrentDistance, rot);
        }
    }
}
