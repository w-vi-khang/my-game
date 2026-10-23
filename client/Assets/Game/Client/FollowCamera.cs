using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Camera PC (0034, world 7l b): mặc định chéo từ trên cao (sim_m1.json / Cài đặt), bám nhân vật; giữ chuột giữa xoay, lăn chuột thu phóng
    /// (Alt + lăn là chọn loại khối); Space về sau lưng; X góc nhìn thứ nhất. SphereCast chống xuyên khối.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public PlayerCharacter target;
        public float distance = 14f, minDistance = 2.5f, maxDistance = 24f;
        public float pitch = 55f, minPitch = -20f, maxPitch = 85f;
        public float yaw;
        public float lookSensitivity = 0.15f;
        public bool invertY;
        public float pivotHeight = 1.6f, castRadius = 0.2f;
        public float defaultPitch = 55f, defaultDistance = 14f, firstPersonPitch = 15f, eyeHeight = 1.5f, eyeForward = 0.25f;

        public bool FirstPerson { get; private set; }
        bool defaultsApplied;
        float savedYaw, savedPitch;

        /// <summary>Khoảng cách thực sau khi chống xuyên khối.</summary>
        public float CurrentDistance { get; private set; }

        public void SetAngles(float yawDeg, float pitchDeg)
        {
            yaw = yawDeg;
            pitch = Mathf.Clamp(pitchDeg, minPitch, maxPitch);
        }

        void Start()
        {
            if (!defaultsApplied) ApplyDefaults(PlayerMotion.CameraPitch, PlayerMotion.CameraDistance);
            firstPersonPitch = PlayerMotion.FirstPersonPitch;
        }

        /// <summary>Góc cúi + khoảng cách mặc định (Space về đây). Cài đặt gọi; không gọi thì Start lấy từ sim_m1.json. Kẹp theo min/max; chỉ đổi góc/khoảng cách đang dùng khi mặc định thật sự đổi hoặc lần đầu.</summary>
        public void ApplyDefaults(float pitchDeg, float dist)
        {
            float p = Mathf.Clamp(pitchDeg, minPitch, maxPitch);
            float d = Mathf.Clamp(dist, minDistance, maxDistance);
            bool changed = !defaultsApplied || p != defaultPitch || d != defaultDistance;
            defaultPitch = p; defaultDistance = d; defaultsApplied = true;
            if (changed) { pitch = p; distance = d; }
        }

        /// <summary>Space: thoát góc nhìn thứ nhất, yaw = hướng mặt nhân vật, pitch/distance = mặc định, SnapNow.</summary>
        public void ResetBehind()
        {
            if (FirstPerson) SetFirstPerson(false);
            if (target != null) yaw = target.FacingYaw;
            pitch = defaultPitch;
            distance = defaultDistance;
            SnapNow();
        }

        /// <summary>X. Bật: lưu yaw/pitch, ẩn thân. Tắt: trả yaw/pitch đã lưu, hiện thân. Cùng trạng thái → không làm gì. SnapNow.</summary>
        public void SetFirstPerson(bool on)
        {
            if (on == FirstPerson) return;
            FirstPerson = on;
            if (on) { savedYaw = yaw; savedPitch = pitch; }
            else { yaw = savedYaw; pitch = savedPitch; }
            if (target != null) target.SetBodyVisible(!on);
            SnapNow();
        }

        /// <summary>Chuột giữa + di chuột: xoay yaw + pitch; góc nhìn thứ nhất → không làm gì.</summary>
        public void ApplyLook(Vector2 delta)
        {
            if (FirstPerson) return;
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity * (invertY ? -1f : 1f), minPitch, maxPitch);
        }

        /// <summary>Lăn chuột: wheel &gt; 0,01 → distance × 0,9; &lt; −0,01 → × 1,1; kẹp; góc nhìn thứ nhất → không làm gì.</summary>
        public void ApplyZoom(float wheel)
        {
            if (FirstPerson) return;
            if (wheel > 0.01f) distance *= 0.9f;
            else if (wheel < -0.01f) distance *= 1.1f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        void OnEnable() { if (FirstPerson && target != null) target.SetBodyVisible(false); }
        void OnDisable() { if (target != null) target.SetBodyVisible(true); }

        void LateUpdate()
        {
            if (target == null) return;
            if (GameInput.HasPointer)
            {
                if (GameInput.RotateCamera.IsPressed() && !MinimapView.IsAnyOpen)
                    ApplyLook(GameInput.Look.ReadValue<Vector2>());
                var pos = GameInput.PointerPosition;
                bool alt = GameInput.CycleBlockModifier.IsPressed();
                if (!alt && !MinimapView.BlocksPointer(pos) && !TimeDial.BlocksPointer(pos) && !SimView.BlocksPointer(pos))
                    ApplyZoom(GameInput.Zoom.ReadValue<float>());
            }
            if (!MinimapView.IsAnyOpen && !SettingsView.IsAnyOpen)
            {
                if (GameInput.CameraReset.WasPressedThisFrame()) ResetBehind();
                else if (GameInput.ToggleFirstPerson.WasPressedThisFrame()) SetFirstPerson(!FirstPerson);
            }
            SnapNow();
        }

        /// <summary>Đặt camera ngay theo góc, khoảng cách và nhân vật hiện tại.</summary>
        public void SnapNow()
        {
            if (target == null) return;
            if (FirstPerson)
            {
                float fy = target.FacingYaw;
                CurrentDistance = 0f;
                transform.SetPositionAndRotation(
                    target.transform.position + Vector3.up * eyeHeight + Quaternion.Euler(0f, fy, 0f) * Vector3.forward * eyeForward,
                    Quaternion.Euler(firstPersonPitch, fy, 0f));
                return;
            }
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
