using UnityEngine;

namespace Game.Client
{
    /// <summary>Hệ số phóng IMGUI: theo chiều cao màn hình (mốc 720p) nhân cỡ chữ người chơi (0032).</summary>
    public static class UiScale
    {
        public static float Gui => Mathf.Max(1f, Screen.height / 720f) * GameSettings.Current.uiScale;
    }
}
