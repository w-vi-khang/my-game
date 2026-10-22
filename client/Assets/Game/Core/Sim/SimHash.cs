namespace Game.Core.Sim
{
    /// <summary>Mã băm FNV-1a 64 bit của trạng thái mô phỏng — dùng kiểm tất định (cùng đầu vào → cùng mã mỗi nhịp).</summary>
    public static class SimHash
    {
        const ulong OffsetBasis = 14695981039346656037UL;
        const ulong Prime = 1099511628211UL;

        public static ulong Compute(SimState s)
        {
            ulong h = OffsetBasis;
            h = MixLong(h, s.Tick);
            h = MixLong(h, (long)s.Rng.State);
            h = MixLong(h, (long)s.Rng.Inc);

            h = MixInt(h, s.Creatures.Count);
            for (int i = 0; i < s.Creatures.Count; i++)
            {
                var c = s.Creatures[i];
                h = MixInt(h, c.Id);
                h = MixInt(h, c.Cell.X); h = MixInt(h, c.Cell.Y); h = MixInt(h, c.Cell.Z);
                h = MixInt(h, (int)c.State);
                h = MixInt(h, c.StageId);
                h = MixInt(h, c.MoveTarget.X); h = MixInt(h, c.MoveTarget.Y); h = MixInt(h, c.MoveTarget.Z);
                h = MixInt(h, c.MoveProgress);
                h = MixInt(h, c.PathIndex);
                h = MixInt(h, c.Path.Count);
                for (int j = 0; j < c.Path.Count; j++)
                {
                    h = MixInt(h, c.Path[j].X); h = MixInt(h, c.Path[j].Y); h = MixInt(h, c.Path[j].Z);
                }
                h = MixInt(h, c.HomeId);
                h = MixInt(h, c.HomeReachable ? 1 : 0);
                h = MixLong(h, c.StuckSince);
                h = MixInt(h, c.BlockedTicks);
                h = MixInt(h, c.Phasing ? 1 : 0);
                h = MixInt(h, c.YieldTo);
                h = MixInt(h, c.YieldHold ? 1 : 0);
                h = MixLong(h, c.WanderAt);
            }

            h = MixInt(h, s.Stages.Count);
            for (int i = 0; i < s.Stages.Count; i++)
            {
                var g = s.Stages[i];
                h = MixInt(h, g.Id);
                h = MixInt(h, g.OrderId);
                h = MixInt(h, (int)g.Ability);
                h = MixInt(h, g.Target.X); h = MixInt(h, g.Target.Y); h = MixInt(h, g.Target.Z);
                h = MixInt(h, g.HasDestination ? 1 : 0);
                h = MixInt(h, g.Destination.X); h = MixInt(h, g.Destination.Y); h = MixInt(h, g.Destination.Z);
                h = MixInt(h, g.BaseTicks);
                h = MixLong(h, g.WorkDone);
                h = MixInt(h, g.Priority);
                h = MixInt(h, (int)g.Status);
                h = MixInt(h, (int)g.AssigneeKind);
                h = MixInt(h, g.AssigneeId);
                h = MixInt(h, g.Carried ? 1 : 0);
                h = MixInt(h, (int)g.Stall);
                h = MixInt(h, (int)g.TargetApproach);
                h = MixInt(h, (int)g.DestinationApproach);
                h = MixInt(h, g.PlotId);
                h = MixInt(h, g.StorageId);
                h = MixInt(h, g.LoadProduce);
                h = MixInt(h, g.LoadSeeds);
                h = MixInt(h, g.CropId);
                h = MixInt(h, (int)g.ReportedStall);
                h = MixInt(h, g.DroppedBy);
            }

            h = MixInt(h, s.Orders.Count);
            for (int i = 0; i < s.Orders.Count; i++)
            {
                var o = s.Orders[i];
                h = MixInt(h, o.Id);
                h = MixInt(h, o.Area.Count);
                for (int j = 0; j < o.Area.Count; j++)
                {
                    h = MixInt(h, o.Area[j].X); h = MixInt(h, o.Area[j].Y); h = MixInt(h, o.Area[j].Z);
                }
                h = MixInt(h, o.StageIds.Count);
                for (int j = 0; j < o.StageIds.Count; j++) h = MixInt(h, o.StageIds[j]);
            }

            h = MixInt(h, s.Storages.Count);
            for (int i = 0; i < s.Storages.Count; i++)
            {
                var t = s.Storages[i];
                h = MixInt(h, t.Id);
                h = MixInt(h, t.Cell.X); h = MixInt(h, t.Cell.Y); h = MixInt(h, t.Cell.Z);
                h = MixInt(h, t.Capacity);
            }

            h = MixInt(h, s.Stock.Capacity);
            h = MixInt(h, s.Stock.Reserved);
            h = MixInt(h, s.Stock.Seeds.Length);
            for (int i = 0; i < s.Stock.Seeds.Length; i++)
            {
                h = MixInt(h, s.Stock.Seeds[i]);
                h = MixInt(h, s.Stock.Produce[i]);
            }

            h = MixInt(h, s.Plots.Count);
            for (int i = 0; i < s.Plots.Count; i++)
            {
                var p = s.Plots[i];
                h = MixInt(h, p.Id);
                h = MixInt(h, p.OrderId);
                h = MixInt(h, p.Soil.X); h = MixInt(h, p.Soil.Y); h = MixInt(h, p.Soil.Z);
                h = MixInt(h, (int)p.Phase);
                h = MixInt(h, p.StageId);
                h = MixLong(h, p.RipeTick);
                h = MixInt(h, p.Slots.Count);
                for (int j = 0; j < p.Slots.Count; j++) { h = MixInt(h, p.Slots[j].CropId); h = MixInt(h, p.Slots[j].Count); }
                h = MixInt(h, p.SlotCount);
                h = MixInt(h, p.SlotMax);
                h = MixInt(h, p.CurrentCrop);
                h = MixInt(h, p.TillPaused ? 1 : 0);
                h = MixInt(h, p.Tilled ? 1 : 0);
                h = MixInt(h, p.Removed ? 1 : 0);
                h = MixInt(h, p.SoilReady ? 1 : 0);
            }

            h = MixInt(h, s.Houses.Count);
            for (int i = 0; i < s.Houses.Count; i++)
            {
                var o = s.Houses[i];
                h = MixInt(h, o.Id);
                h = MixInt(h, o.Cell.X); h = MixInt(h, o.Cell.Y); h = MixInt(h, o.Cell.Z);
                h = MixInt(h, o.SizeX); h = MixInt(h, o.SizeZ); h = MixInt(h, o.SizeY);
                h = MixInt(h, o.Capacity);
                h = MixInt(h, o.Residents.Count);
                for (int j = 0; j < o.Residents.Count; j++) h = MixInt(h, o.Residents[j]);
            }
            return h;
        }

        static ulong MixInt(ulong h, int v)
        {
            for (int i = 0; i < 4; i++)
            {
                h = (h ^ (byte)(v >> (8 * i))) * Prime;
            }
            return h;
        }

        static ulong MixLong(ulong h, long v)
        {
            for (int i = 0; i < 8; i++)
            {
                h = (h ^ (byte)(v >> (8 * i))) * Prime;
            }
            return h;
        }
    }
}
