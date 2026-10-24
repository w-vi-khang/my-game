using System;
using Game.Core.Sim;

namespace Game.Core.View
{
    public enum WorldIconKind : byte { None, Till, Sow, Water, Grow, Ripe, Empty, Harvest, Haul, Idle, Stall }

    /// <summary>Icon trên world (0034 mục 7, brief 0065). Percent: 0..100 → vòng tiến trình; -1 → không vòng. Crop: loại cây (-1 không có). Dim: vẽ mờ.</summary>
    public readonly struct WorldIcon : IEquatable<WorldIcon>
    {
        public readonly WorldIconKind Kind;
        public readonly int Percent, Crop;
        public readonly bool Dim;

        public WorldIcon(WorldIconKind kind, int percent = -1, int crop = -1, bool dim = false)
        {
            Kind = kind;
            Percent = percent;
            Crop = crop;
            Dim = dim;
        }

        public bool Equals(WorldIcon o) => Kind == o.Kind && Percent == o.Percent && Crop == o.Crop && Dim == o.Dim;
        public override bool Equals(object obj) => obj is WorldIcon o && Equals(o);
        public override int GetHashCode() => ((((int)Kind * 397) ^ Percent) * 397 ^ Crop) * 397 ^ (Dim ? 1 : 0);
        public static bool operator ==(WorldIcon a, WorldIcon b) => a.Equals(b);
        public static bool operator !=(WorldIcon a, WorldIcon b) => !a.Equals(b);
    }

    /// <summary>Ánh xạ trạng thái mô phỏng → icon trên world (thay nhãn chữ).</summary>
    public static class WorldIcons
    {
        public static WorldIcon ForPlot(FarmPlot p, SimState s, SimConfig cfg)
        {
            if (p.Removed) return default;
            int pct = SiteStatus.ProgressPercent(p, s, cfg);
            switch (p.Phase)
            {
                case PlotPhase.Till: return new WorldIcon(WorldIconKind.Till, pct, -1, p.TillPaused);
                case PlotPhase.Sow: return new WorldIcon(WorldIconKind.Sow, pct, p.CurrentCrop);
                case PlotPhase.Water: return new WorldIcon(WorldIconKind.Water, pct, p.CurrentCrop);
                case PlotPhase.Growing: return new WorldIcon(WorldIconKind.Grow, pct, p.CurrentCrop);
                case PlotPhase.Harvest: return new WorldIcon(WorldIconKind.Ripe, pct, p.CurrentCrop);
                default: return p.Tilled ? new WorldIcon(WorldIconKind.Empty) : default;
            }
        }

        public static WorldIcon ForCreature(SimState s, Creature c)
        {
            if (c.State == CreatureState.Working && c.StageId >= 0)
            {
                var st = s.Stages[c.StageId];
                var k = KindOf(st.Ability);
                return k == WorldIconKind.Idle ? new WorldIcon(WorldIconKind.Idle) : new WorldIcon(k, st.ProgressPermille / 10);
            }
            if (c.State == CreatureState.Moving)
            {
                if (c.StageId < 0) return default;
                var k = KindOf(s.Stages[c.StageId].Ability);
                return k == WorldIconKind.Idle ? new WorldIcon(WorldIconKind.Idle) : new WorldIcon(k, -1, -1, true);
            }
            return new WorldIcon(WorldIconKind.Idle);
        }

        public static WorldIcon ForStall() => new WorldIcon(WorldIconKind.Stall);

        static WorldIconKind KindOf(WorkAbility a) => a switch
        {
            WorkAbility.Till => WorldIconKind.Till,
            WorkAbility.Sow => WorldIconKind.Sow,
            WorkAbility.Water => WorldIconKind.Water,
            WorkAbility.Harvest => WorldIconKind.Harvest,
            WorkAbility.Haul => WorldIconKind.Haul,
            _ => WorldIconKind.Idle,
        };
    }
}
