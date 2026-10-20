using System;

namespace Game.Core
{
    /// <summary>
    /// Ảnh nền minimap (world-tech bước 7c, brief 0015): 1 điểm ảnh = 1 cột (x, z). Cột có nước phía trên mặt đất → màu theo độ sâu nước;
    /// không có → màu theo tầng độ cao (mỗi TierHeight ô một tầng). Đọc thẳng WorldData, không dựng voxel → tạo lại được từ seed, không lưu.
    /// </summary>
    public static class MinimapImage
    {
        /// <summary>Số ô độ cao mỗi tầng màu.</summary>
        public const int TierHeight = 4;

        /// <summary>Màu tầng đất 0xRRGGBB; tầng = max(mặt đất, 0) / TierHeight (0–11 với world cao 48); tầng vượt bảng dùng màu cuối.</summary>
        public static readonly uint[] LandTiers =
        {
            0x6F8F55, // 0: 0–3
            0x7A9C5B, // 1: 4–7
            0x86A862, // 2: 8–11
            0xA8D47A, // 3: 12–15 (ngay trên mặt biển)
            0x93C66A, // 4: 16–19
            0x7DB65B, // 5: 20–23
            0x9DB15E, // 6: 24–27
            0xBFB46A, // 7: 28–31
            0xC9A26A, // 8: 32–35
            0xAE8A63, // 9: 36–39
            0x9C928A, // 10: 40–43
            0xEDEDED, // 11: 44–47
        };

        /// <summary>Màu nước 0xRRGGBB theo độ sâu (ô): 1–2, 3–4, 5–6, ≥ 7.</summary>
        public static readonly uint[] WaterBands = { 0x8FD3EC, 0x5DB0DE, 0x3486C4, 0x1F5799 };

        public static uint LandColor(int surface) =>
            LandTiers[Math.Min(Math.Max(surface, 0) / TierHeight, LandTiers.Length - 1)];

        public static uint WaterColor(int depth) =>
            WaterBands[Math.Min(Math.Max(depth - 1, 0) / 2, WaterBands.Length - 1)];

        /// <summary>
        /// Quét cột từ trên xuống: surface = ô rắn cao nhất (không tính nước; -1 nếu cột rỗng);
        /// waterDepth = ô nước cao nhất phía trên surface − surface (0 nếu không có nước phía trên).
        /// </summary>
        public static void ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)
        {
            var s = world.Size;
            var cells = world.Cells;
            int waterTop = -1;
            surface = -1;
            for (int y = s.Y - 1; y >= 0; y--)
            {
                var m = Cell.GetMaterial(cells[s.Index(x, y, z)]);
                if (m == BlockMaterial.Empty)
                    continue;
                if (m == BlockMaterial.Water)
                {
                    if (waterTop < 0)
                        waterTop = y;
                    continue;
                }
                surface = y;
                break;
            }
            waterDepth = waterTop >= 0 ? waterTop - surface : 0;
        }

        /// <summary>Màu 0xRRGGBB của cột (x, z).</summary>
        public static uint ColumnColor(WorldData world, int x, int z)
        {
            ColumnInfo(world, x, z, out int surface, out int waterDepth);
            return waterDepth > 0 ? WaterColor(waterDepth) : LandColor(surface);
        }

        /// <summary>
        /// Ảnh RGBA32 cả world: 4 byte (R, G, B, A = 255) mỗi cột, cột (x, z) ở byte (x + Size.X · z) · 4 —
        /// x chạy nhanh nhất, hàng z = 0 trước (khớp Texture2D.SetPixels32: hàng dưới cùng trước).
        /// </summary>
        public static byte[] Build(WorldData world)
        {
            var s = world.Size;
            var rgba = new byte[s.X * s.Z * 4];
            for (int z = 0; z < s.Z; z++)
                for (int x = 0; x < s.X; x++)
                {
                    uint c = ColumnColor(world, x, z);
                    int i = (x + s.X * z) * 4;
                    rgba[i] = (byte)(c >> 16);
                    rgba[i + 1] = (byte)(c >> 8);
                    rgba[i + 2] = (byte)c;
                    rgba[i + 3] = 255;
                }
            return rgba;
        }
    }
}
