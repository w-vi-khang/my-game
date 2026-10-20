using System;

namespace Game.Core
{
    /// <summary>
    /// Vùng nhìn của minimap (world-tech bước 7c, brief 0015): phóng to/kéo/chụm 2 ngón trên ảnh nền.
    /// Toạ độ khung: pixel, gốc góc dưới-trái, x sang phải, y lên trên. Toạ độ world: cột (x, z), z lên trên màn hình.
    /// Zoom = 1 → cả world vừa khung (tỉ lệ nhỏ hơn của 2 trục); zoom trong [1, MaxZoom].
    /// </summary>
    public sealed class MinimapViewport
    {
        public const float MaxZoom = 8f;

        public readonly float WorldWidth, WorldHeight;
        public float PanelWidth { get; private set; }
        public float PanelHeight { get; private set; }
        public float Zoom { get; private set; } = 1f;
        /// <summary>Điểm world ở giữa khung.</summary>
        public float CenterX { get; private set; }
        public float CenterZ { get; private set; }

        public MinimapViewport(float worldWidth, float worldHeight, float panelWidth, float panelHeight)
        {
            if (!(worldWidth > 0f) || !(worldHeight > 0f))
                throw new ArgumentException("Kích thước world phải dương");
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            CenterX = worldWidth * 0.5f;
            CenterZ = worldHeight * 0.5f;
            SetPanelSize(panelWidth, panelHeight);
        }

        /// <summary>Số pixel khung cho mỗi cột world.</summary>
        public float PixelsPerColumn => Math.Min(PanelWidth / WorldWidth, PanelHeight / WorldHeight) * Zoom;

        /// <summary>Đổi kích thước khung (vd xoay màn hình), giữ zoom và tâm rồi chặn lại.</summary>
        public void SetPanelSize(float width, float height)
        {
            if (!(width > 0f) || !(height > 0f))
                throw new ArgumentException("Kích thước khung phải dương");
            PanelWidth = width;
            PanelHeight = height;
            Clamp();
        }

        /// <summary>Về zoom 1, tâm world.</summary>
        public void Reset()
        {
            Zoom = 1f;
            CenterX = WorldWidth * 0.5f;
            CenterZ = WorldHeight * 0.5f;
            Clamp();
        }

        public void WorldToPanel(float wx, float wz, out float px, out float py)
        {
            float k = PixelsPerColumn;
            px = PanelWidth * 0.5f + (wx - CenterX) * k;
            py = PanelHeight * 0.5f + (wz - CenterZ) * k;
        }

        public void PanelToWorld(float px, float py, out float wx, out float wz)
        {
            float k = PixelsPerColumn;
            wx = CenterX + (px - PanelWidth * 0.5f) / k;
            wz = CenterZ + (py - PanelHeight * 0.5f) / k;
        }

        /// <summary>Phóng zoom × factor quanh điểm khung (px, py): điểm world dưới (px, py) giữ nguyên chỗ, trừ khi bị chặn ở mép world.</summary>
        public void ZoomAt(float px, float py, float factor)
        {
            if (!(factor > 0f))
                return;
            PanelToWorld(px, py, out float wx, out float wz);
            Zoom = Math.Min(Math.Max(Zoom * factor, 1f), MaxZoom);
            float k = PixelsPerColumn;
            CenterX = wx - (px - PanelWidth * 0.5f) / k;
            CenterZ = wz - (py - PanelHeight * 0.5f) / k;
            Clamp();
        }

        /// <summary>Kéo theo (dx, dy) pixel khung: kéo sang phải → nội dung đi sang phải.</summary>
        public void Pan(float dx, float dy)
        {
            float k = PixelsPerColumn;
            CenterX -= dx / k;
            CenterZ -= dy / k;
            Clamp();
        }

        /// <summary>
        /// Chụm 2 ngón: ngón A từ (a0x, a0y) tới (a1x, a1y), ngón B từ (b0x, b0y) tới (b1x, b1y) (pixel khung) →
        /// kéo theo trung điểm, rồi phóng quanh trung điểm mới theo tỉ lệ khoảng cách mới / cũ (bỏ phóng khi khoảng cách cũ &lt; 1 pixel).
        /// </summary>
        public void Pinch(float a0x, float a0y, float b0x, float b0y, float a1x, float a1y, float b1x, float b1y)
        {
            float m0x = (a0x + b0x) * 0.5f, m0y = (a0y + b0y) * 0.5f;
            float m1x = (a1x + b1x) * 0.5f, m1y = (a1y + b1y) * 0.5f;
            Pan(m1x - m0x, m1y - m0y);
            float d0 = Distance(a0x, a0y, b0x, b0y), d1 = Distance(a1x, a1y, b1x, b1y);
            if (d0 >= 1f)
                ZoomAt(m1x, m1y, d1 / d0);
        }

        /// <summary>
        /// Vùng ảnh nền hiện trong khung, theo phần của world (u, v = góc dưới-trái; w, h = bề rộng, bề cao) — dùng làm RawImage.uvRect.
        /// Có thể ra ngoài 0..1 khi khung rộng hơn world (zoom 1, khung không vuông).
        /// </summary>
        public void VisibleUv(out float u, out float v, out float w, out float h)
        {
            float k = PixelsPerColumn;
            float hw = PanelWidth * 0.5f / k, hh = PanelHeight * 0.5f / k;
            u = (CenterX - hw) / WorldWidth;
            v = (CenterZ - hh) / WorldHeight;
            w = 2f * hw / WorldWidth;
            h = 2f * hh / WorldHeight;
        }

        void Clamp()
        {
            float k = PixelsPerColumn;
            CenterX = ClampAxis(CenterX, PanelWidth * 0.5f / k, WorldWidth);
            CenterZ = ClampAxis(CenterZ, PanelHeight * 0.5f / k, WorldHeight);
        }

        /// <summary>Vùng nhìn ≥ world theo trục này → căn giữa; ngược lại giữ vùng nhìn trong [0, size].</summary>
        static float ClampAxis(float center, float half, float size) =>
            2f * half >= size ? size * 0.5f : Math.Min(Math.Max(center, half), size - half);

        static float Distance(float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
