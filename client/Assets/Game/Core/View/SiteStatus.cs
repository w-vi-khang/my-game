using Game.Core.Sim;

namespace Game.Core.View
{
    /// <summary>Chữ bảng HUD kho/ruộng và dấu mang hàng (sim bước 6, sửa sau user xem 2026-10-16).</summary>
    public static class SiteStatus
    {
        /// <summary>"Kho · nông sản 4 · hạt 10 · 14/50" — không tính phần đã giữ chỗ (Reserved).</summary>
        public static string StorageText(Stockpile s) =>
            "Kho · nông sản " + s.ProduceTotal + " · hạt " + s.SeedsTotal + " · " + (s.SeedsTotal + s.ProduceTotal) + "/" + s.Capacity;

        /// <summary>"Cây A" — tên tạm theo id trong sim_m1.json (0025; user đặt tên thật sau).</summary>
        public static string CropName(SimConfig cfg, int cropId) => "Cây " + cfg.crops[cropId].id;

        /// <summary>Bảng kho (0025): dòng đầu "Kho chung · đã dùng 14/100", mỗi loại một dòng "Cây A · nông sản 4 · hạt 10". Đã dùng không tính Reserved (như StorageText).</summary>
        public static string StockPanelText(Stockpile s, SimConfig cfg)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Kho chung · đã dùng ").Append(s.SeedsTotal + s.ProduceTotal).Append('/').Append(s.Capacity);
            for (int i = 0; i < cfg.crops.Length; i++)
                sb.Append('\n').Append(CropName(cfg, i)).Append(" · nông sản ").Append(s.Produce[i]).Append(" · hạt ").Append(s.Seeds[i]);
            return sb.ToString();
        }

        /// <summary>Số dưới ô đợi index: "2/3"; ô đợi trống "0/3" (0027).</summary>
        public static string SlotCountText(FarmPlot p, int index) =>
            (index < p.Slots.Count ? p.Slots[index].Count : 0) + "/" + p.SlotMax;

        /// <summary>% ô tiến trình (0027): xới/gieo/tưới/thu theo tiến độ công đoạn, lớn theo GrowPercent; đứng yên / đã huỷ = 0.</summary>
        public static int ProgressPercent(FarmPlot p, SimState s, SimConfig cfg)
        {
            if (p.Removed) return 0;
            switch (p.Phase)
            {
                case PlotPhase.Growing: return GrowPercent(p, s.Tick, cfg.GrowTicks(p.CurrentCrop));
                case PlotPhase.Till:
                case PlotPhase.Sow:
                case PlotPhase.Water:
                case PlotPhase.Harvest:
                    return p.StageId >= 0 ? s.Stages[p.StageId].ProgressPermille / 10 : 0;
                default: return 0;
            }
        }

        /// <summary>Chữ ô tiến trình (0027): "Xới 60%", "Xới 60% · tạm dừng", "Cây A · gieo 0%", "Cây A · tưới 50%", "Cây A · lớn 40%", "Cây A · thu hoạch 20%", "Trống", "Đã huỷ".</summary>
        public static string ProgressText(FarmPlot p, SimState s, SimConfig cfg)
        {
            if (p.Removed) return "Đã huỷ";
            int pct = ProgressPercent(p, s, cfg);
            switch (p.Phase)
            {
                case PlotPhase.Till: return "Xới " + pct + "%" + (p.TillPaused ? " · tạm dừng" : "");
                case PlotPhase.Sow: return CropName(cfg, p.CurrentCrop) + " · gieo " + pct + "%";
                case PlotPhase.Water: return CropName(cfg, p.CurrentCrop) + " · tưới " + pct + "%";
                case PlotPhase.Growing: return CropName(cfg, p.CurrentCrop) + " · lớn " + pct + "%";
                case PlotPhase.Harvest: return CropName(cfg, p.CurrentCrop) + " · thu hoạch " + pct + "%";
                default: return "Trống";
            }
        }

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
