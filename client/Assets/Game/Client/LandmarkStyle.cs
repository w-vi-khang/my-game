using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Màu dấu mốc (0023, brief 0018) — dùng chung cho khối trong world và biểu tượng trên bản đồ. Màu tạm, chỉnh khi chơi thử.</summary>
    public static class LandmarkStyle
    {
        public static readonly Color32 CheckpointColor = new Color32(40, 200, 230, 255);

        static readonly Color32[] StructureColors =
        {
            new Color32(230, 120, 40, 255),
            new Color32(200, 60, 60, 255),
            new Color32(240, 200, 50, 255),
            new Color32(150, 90, 200, 255),
            new Color32(230, 90, 170, 255),
            new Color32(240, 240, 240, 255),
        };

        public static Color32 ColorOf(in Landmark l) =>
            l.Kind == LandmarkKind.Checkpoint ? CheckpointColor : StructureColors[l.Type % StructureColors.Length];
    }
}
