using System;

namespace Game.Core
{
    /// <summary>Cài đặt người chơi (0032): chỉ cảm giác / hiển thị, lưu ở settings.json (client). Không chứa luật game.</summary>
    [Serializable]
    public class PlayerSettings
    {
        public const float CameraDistanceMin = 2.5f, CameraDistanceMax = 24f;
        public const float CameraPitchMin = 10f, CameraPitchMax = 85f;
        public const float LookSensitivityMin = 0.05f, LookSensitivityMax = 0.5f;
        public const float UiScaleMin = 0.75f, UiScaleMax = 2f;
        public const string DefaultLanguage = "vi";

        public int formatVersion = 2;
        /// <summary>0 = theo mặc định của game (sim_m1.json player.cameraDistance / cameraPitch).</summary>
        public float cameraDistance = 0f;
        /// <summary>0 = theo mặc định của game (sim_m1.json player.cameraDistance / cameraPitch).</summary>
        public float cameraPitch = 0f;
        public float lookSensitivity = 0.15f;
        public bool invertY = false;
        public float uiScale = 1f;
        public int targetFps = 60;
        public string language = DefaultLanguage;

        public void Clamp()
        {
            if (formatVersion < 2) { cameraDistance = 0f; cameraPitch = 0f; formatVersion = 2; }
            cameraDistance = ClampOptional(cameraDistance, CameraDistanceMin, CameraDistanceMax);
            cameraPitch = ClampOptional(cameraPitch, CameraPitchMin, CameraPitchMax);
            lookSensitivity = ClampF(lookSensitivity, LookSensitivityMin, LookSensitivityMax, 0.15f);
            uiScale = ClampF(uiScale, UiScaleMin, UiScaleMax, 1f);
            if (targetFps != 30 && targetFps != 60) targetFps = 60;
            if (!IsValidLanguage(language)) language = DefaultLanguage;
        }

        public PlayerSettings Clone() => new PlayerSettings
        {
            formatVersion = formatVersion,
            cameraDistance = cameraDistance,
            cameraPitch = cameraPitch,
            lookSensitivity = lookSensitivity,
            invertY = invertY,
            uiScale = uiScale,
            targetFps = targetFps,
            language = language,
        };

        /// <summary>1–32 ký tự, chỉ a-z, 0-9, '_' , '-'.</summary>
        public static bool IsValidLanguage(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 32) return false;
            foreach (char c in id)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) return false;
            return true;
        }

        static float ClampOptional(float v, float min, float max)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0f) return 0f;
            return Math.Min(Math.Max(v, min), max);
        }

        static float ClampF(float v, float min, float max, float def)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return def;
            return Math.Min(Math.Max(v, min), max);
        }
    }
}
