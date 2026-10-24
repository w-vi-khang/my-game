using System;
using System.Collections.Generic;
using Game.Core.DayCycle;
using Game.Core.Sim;

namespace Game.Core.Habitat
{
    /// <summary>
    /// Môi trường sống (sim 8a): đếm ô cho từng yêu cầu "cells" theo sự kiện ô đổi (không quét lại cả vùng), tính các yêu cầu khác trực tiếp
    /// từ SimState, và mở loài khi mọi yêu cầu đạt. Nơi gọi tự nối <c>editor.CellChanged += tracker.OnCellChanged</c>.
    /// </summary>
    public sealed class HabitatTracker
    {
        sealed class Req
        {
            public HabitatRequirement Cfg;
            public BlockMaterial Mat;
            public bool Solid;
            public PeriodCondition Period;
            public ulong[] Bits;
            public int Count;
        }

        readonly HabitatConfig config;
        readonly WorldData world;
        readonly int x0, z0, x1, z1, w, d;
        readonly Req[][] reqs;
        readonly bool[] unlocked;

        public HabitatTracker(HabitatConfig config, WorldData world)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            config.Validate();
            var size = world.Size;
            if (config.WholeWorld) { x0 = 0; z0 = 0; x1 = size.X; z1 = size.Z; }
            else
            {
                x0 = config.regionX0; z0 = config.regionZ0; x1 = config.regionX1; z1 = config.regionZ1;
                if (x1 > size.X || z1 > size.Z)
                    throw new ArgumentException($"Region exceeds world size {size}: [{x0},{x1}) x [{z0},{z1})");
            }
            w = x1 - x0;
            d = z1 - z0;

            int n = config.species.Length;
            reqs = new Req[n][];
            unlocked = new bool[n];
            long bitCount = (long)w * d * size.Y;
            for (int s = 0; s < n; s++)
            {
                var list = config.species[s].requirements;
                reqs[s] = new Req[list.Length];
                for (int r = 0; r < list.Length; r++)
                {
                    var cfg = list[r];
                    var q = new Req { Cfg = cfg };
                    if (cfg.kind == HabitatRequirement.KindCells)
                    {
                        q.Solid = cfg.material == HabitatRequirement.SolidMaterial;
                        if (!q.Solid) q.Mat = (BlockMaterial)Enum.Parse(typeof(BlockMaterial), cfg.material);
                        q.Bits = new ulong[(bitCount + 63) / 64];
                    }
                    else if (cfg.kind == HabitatRequirement.KindPeriod)
                        q.Period = new PeriodCondition(cfg.periods);
                    reqs[s][r] = q;
                }
                unlocked[s] = Array.IndexOf(config.unlockedAtStart ?? new string[0], config.species[s].id) >= 0;
            }
            Scan(size);
        }

        void Scan(WorldSize size)
        {
            for (int s = 0; s < reqs.Length; s++)
                foreach (var q in reqs[s])
                {
                    if (q.Bits == null) continue;
                    for (int y = 0; y < size.Y; y++)
                    for (int z = z0; z < z1; z++)
                    for (int x = x0; x < x1; x++)
                        if (Matches(q, x, y, z))
                        {
                            int bit = BitIndex(x, y, z);
                            q.Bits[bit >> 6] |= 1UL << (bit & 63);
                            q.Count++;
                        }
                }
        }

        int BitIndex(int x, int y, int z) => (x - x0) + w * ((z - z0) + d * y);

        /// <summary>Điều kiện của yêu cầu "cells" cho một ô — dùng chung cho quét đầu và OnCellChanged.</summary>
        bool Matches(Req q, int x, int y, int z)
        {
            var c = q.Cfg;
            if (y < c.minY) return false;
            var m = world.GetMaterial(x, y, z);
            if (q.Solid ? (m == BlockMaterial.Empty || m == BlockMaterial.Water) : m != q.Mat) return false;
            if (c.exposed && y + 1 < world.Size.Y && world.GetMaterial(x, y + 1, z) != BlockMaterial.Empty) return false;
            if (c.nearWater && !NearWater(x, y, z)) return false;
            return true;
        }

        bool NearWater(int x, int y, int z)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                int yy = y + dy;
                if (IsWater(x + 1, yy, z) || IsWater(x - 1, yy, z) || IsWater(x, yy, z + 1) || IsWater(x, yy, z - 1)) return true;
            }
            return false;
        }

        bool IsWater(int x, int y, int z) => world.Size.InBounds(x, y, z) && world.GetMaterial(x, y, z) == BlockMaterial.Water;

        /// <summary>Ô (x, y, z) vừa đổi: tính lại đúng 10 ô bị ảnh hưởng (chính nó, ô dưới, 4 ô ngang cùng tầng, 4 ô ngang tầng dưới).</summary>
        public void OnCellChanged(int x, int y, int z)
        {
            for (int dy = 0; dy >= -1; dy--)
            {
                Refresh(x, y + dy, z);
                Refresh(x + 1, y + dy, z);
                Refresh(x - 1, y + dy, z);
                Refresh(x, y + dy, z + 1);
                Refresh(x, y + dy, z - 1);
            }
        }

        void Refresh(int x, int y, int z)
        {
            if (x < x0 || x >= x1 || z < z0 || z >= z1 || y < 0 || y >= world.Size.Y) return;
            int bit = BitIndex(x, y, z);
            int word = bit >> 6;
            ulong mask = 1UL << (bit & 63);
            for (int s = 0; s < reqs.Length; s++)
                foreach (var q in reqs[s])
                {
                    if (q.Bits == null) continue;
                    bool was = (q.Bits[word] & mask) != 0;
                    bool now = Matches(q, x, y, z);
                    if (was == now) continue;
                    if (now) { q.Bits[word] |= mask; q.Count++; }
                    else { q.Bits[word] &= ~mask; q.Count--; }
                }
        }

        public int SpeciesCount => config.species.Length;
        public string SpeciesId(int s) => config.species[s].id;
        public int RequirementCount(int s) => reqs[s].Length;

        /// <summary>Số đếm hiện tại; kind "period": 1 = đạt, 0 = chưa.</summary>
        public int Current(int s, int r, SimState state, DayPeriod period)
        {
            var q = reqs[s][r];
            var c = q.Cfg;
            switch (c.kind)
            {
                case HabitatRequirement.KindCells:
                    return q.Count;
                case HabitatRequirement.KindCrops:
                    int crops = 0;
                    foreach (var p in state.Plots)
                    {
                        if (p.Removed || p.CurrentCrop < 0) continue;
                        if (p.Phase != PlotPhase.Water && p.Phase != PlotPhase.Growing && p.Phase != PlotPhase.Harvest) continue;
                        if (c.cropId == -1 || p.CurrentCrop == c.cropId) crops++;
                    }
                    return crops;
                case HabitatRequirement.KindStructures:
                    return c.structure == "storage" ? state.Storages.Count : state.Houses.Count;
                case HabitatRequirement.KindSpecies:
                    int n = 0;
                    foreach (var cr in state.Creatures)
                        if (cr.SpeciesId == c.species) n++;
                    return n;
                default:
                    return q.Period.IsMet(period) ? 1 : 0;
            }
        }

        public int Target(int s, int r) => reqs[s][r].Cfg.kind == HabitatRequirement.KindPeriod ? 1 : reqs[s][r].Cfg.min;

        public bool IsMet(int s, int r, SimState state, DayPeriod period) =>
            Current(s, r, state, period) >= Target(s, r);

        public bool IsUnlocked(int s) => unlocked[s];

        /// <summary>Xét loài chưa mở theo thứ tự config; loài có MỌI yêu cầu đạt → mở (không khoá lại), thêm id vào returned. Trả số loài vừa mở.</summary>
        public int Update(SimState state, DayPeriod period, List<string> returned)
        {
            int opened = 0;
            for (int s = 0; s < unlocked.Length; s++)
            {
                if (unlocked[s] || reqs[s].Length == 0) continue;
                bool all = true;
                for (int r = 0; r < reqs[s].Length && all; r++)
                    all = IsMet(s, r, state, period);
                if (!all) continue;
                unlocked[s] = true;
                opened++;
                returned?.Add(config.species[s].id);
            }
            return opened;
        }
    }
}
