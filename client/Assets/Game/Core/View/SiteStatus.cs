using Game.Core.Sim;
using Game.Core.Text;

namespace Game.Core.View
{
    /// <summary>Chữ bảng HUD kho/ruộng và dấu mang hàng (sim bước 6, sửa sau user xem 2026-10-16).</summary>
    public static class SiteStatus
    {
        /// <summary>"Kho · nông sản 4 · hạt 10 · 14/50" — không tính phần đã giữ chỗ (Reserved).</summary>
        public static string StorageText(Strings t, Stockpile s) =>
            t.Format("site.storage", s.ProduceTotal, s.SeedsTotal, s.SeedsTotal + s.ProduceTotal, s.Capacity);

        /// <summary>"Cây A" — tên tạm theo id trong sim_m1.json (0025; user đặt tên thật sau).</summary>
        public static string CropName(Strings t, SimConfig cfg, int cropId) => t.Format("crop.name", cfg.crops[cropId].id);

        /// <summary>Bảng kho (0025): dòng đầu "Kho chung · đã dùng 14/100", mỗi loại một dòng "Cây A · nông sản 4 · hạt 10". Đã dùng không tính Reserved (như StorageText).</summary>
        public static string StockPanelText(Strings t, Stockpile s, SimConfig cfg)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(t.Format("site.stock_head", s.SeedsTotal + s.ProduceTotal, s.Capacity));
            for (int i = 0; i < cfg.crops.Length; i++)
                sb.Append('\n').Append(t.Format("site.stock_line", CropName(t, cfg, i), s.Produce[i], s.Seeds[i]));
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
        public static string ProgressText(Strings t, FarmPlot p, SimState s, SimConfig cfg)
        {
            if (p.Removed) return t.Get("site.progress_removed");
            int pct = ProgressPercent(p, s, cfg);
            switch (p.Phase)
            {
                case PlotPhase.Till: return t.Format(p.TillPaused ? "site.progress_till_paused" : "site.progress_till", pct);
                case PlotPhase.Sow: return t.Format("site.progress_sow", CropName(t, cfg, p.CurrentCrop), pct);
                case PlotPhase.Water: return t.Format("site.progress_water", CropName(t, cfg, p.CurrentCrop), pct);
                case PlotPhase.Growing: return t.Format("site.progress_grow", CropName(t, cfg, p.CurrentCrop), pct);
                case PlotPhase.Harvest: return t.Format("site.progress_harvest", CropName(t, cfg, p.CurrentCrop), pct);
                default: return t.Get("site.progress_empty");
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

        public static string PlotText(Strings t, FarmPlot p, long tick, int growTicks)
        {
            string head = t.Format("plot.head", p.Id);
            if (p.Removed) return head + t.Get("plot.removed");
            switch (p.Phase)
            {
                case PlotPhase.Till: return head + t.Get("plot.till");
                case PlotPhase.Sow: return head + t.Get("plot.sow");
                case PlotPhase.Water: return head + t.Get("plot.water");
                case PlotPhase.Growing: return head + t.Format("plot.grow", GrowPercent(p, tick, growTicks));
                case PlotPhase.Harvest: return head + t.Get("plot.ripe");
                default: return head + t.Get("plot.empty");
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
