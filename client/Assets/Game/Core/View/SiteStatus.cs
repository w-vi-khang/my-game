using Game.Core.Sim;

namespace Game.Core.View
{
    /// <summary>Chữ bảng HUD kho/ruộng và dấu mang hàng (sim bước 6, sửa sau user xem 2026-10-16).</summary>
    public static class SiteStatus
    {
        /// <summary>"Kho · nông sản 4 · hạt 10 · 14/50" — không tính phần đã giữ chỗ (Reserved).</summary>
        public static string StorageText(Storage s) =>
            "Kho · nông sản " + s.Produce + " · hạt " + s.Seeds + " · " + (s.Seeds + s.Produce) + "/" + s.Capacity;

        /// <summary>% sinh trưởng làm tròn xuống, chặn 0..100. Chỉ có nghĩa khi Phase = Growing.</summary>
        public static int GrowPercent(FarmPlot p, long tick, int growTicks)
        {
            long done = growTicks - (p.RipeTick - tick);
            if (done <= 0) return 0;
            if (done >= growTicks) return 100;
            return (int)(done * 100 / growTicks);
        }

        public static string PlotText(FarmPlot p, long tick, int growTicks)
        {
            string head = "Ruộng " + p.Id + " · ";
            if (p.Removed) return head + "đã huỷ";
            switch (p.Phase)
            {
                case PlotPhase.Till: return head + "xới";
                case PlotPhase.Sow: return head + "gieo";
                case PlotPhase.Water: return head + "tưới";
                case PlotPhase.Growing: return head + "lớn " + GrowPercent(p, tick, growTicks) + "%";
                case PlotPhase.Harvest: return head + "chín";
                default: return head + "trống";
            }
        }

        /// <summary>Đang cầm hàng: công đoạn vận chuyển của sinh vật đã lấy hàng (Carried).</summary>
        public static bool IsCarrying(SimState s, Creature c)
        {
            if (c.StageId < 0) return false;
            var st = s.Stages[c.StageId];
            return st.Ability == WorkAbility.Haul && st.Carried;
        }
    }
}
