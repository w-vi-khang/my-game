using System;
using System.Collections.Generic;
using Game.Core.Sim;

namespace Game.Core.View
{
    /// <summary>
    /// Nội suy vị trí sinh vật giữa 2 nhịp (sim bước 6): mỗi nhịp lưu vị trí liên tục (ô + MoveProgress, phần nghìn ô);
    /// hiển thị = nhịp trước → nhịp sau theo alpha, nên trễ 1 nhịp. Dời quá 1 ô theo một trục, hoặc sự kiện hiện lại → đặt thẳng.
    /// Chỉ số = id sinh vật (= vị trí trong State.Creatures).
    /// </summary>
    public sealed class SimInterpolator
    {
        public const int SnapMilli = 1000;

        struct Track
        {
            public int PX, PY, PZ, CX, CY, CZ, HX, HZ;
        }

        readonly List<Track> tracks = new List<Track>();

        public int Count => tracks.Count;

        /// <summary>Vị trí liên tục (phần nghìn ô) của góc nhỏ nhất của khung.</summary>
        public static void PoseMilli(Creature c, out int x, out int y, out int z)
        {
            x = c.Cell.X * 1000;
            y = c.Cell.Y * 1000;
            z = c.Cell.Z * 1000;
            if (c.State == CreatureState.Moving && c.PathIndex < c.Path.Count)
            {
                var n = c.Path[c.PathIndex];
                x += (n.X - c.Cell.X) * c.MoveProgress;
                y += (n.Y - c.Cell.Y) * c.MoveProgress;
                z += (n.Z - c.Cell.Z) * c.MoveProgress;
            }
        }

        /// <summary>Gọi một lần sau khi dựng xong (trước Tick đầu), và sau mỗi Tick().</summary>
        public void Capture(SimState s)
        {
            var cs = s.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                PoseMilli(cs[i], out int x, out int y, out int z);
                if (i >= tracks.Count)
                {
                    tracks.Add(new Track { PX = x, PY = y, PZ = z, CX = x, CY = y, CZ = z, HX = 0, HZ = 1 });
                    continue;
                }
                var t = tracks[i];
                t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                t.CX = x; t.CY = y; t.CZ = z;
                if (Math.Abs(t.CX - t.PX) > SnapMilli || Math.Abs(t.CY - t.PY) > SnapMilli || Math.Abs(t.CZ - t.PZ) > SnapMilli)
                {
                    t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                }
                else if (t.CX != t.PX || t.CZ != t.PZ)
                {
                    t.HX = t.CX - t.PX;
                    t.HZ = t.CZ - t.PZ;
                }
                tracks[i] = t;
            }
        }

        /// <summary>Gọi sau Capture của cùng nhịp, trước khi Clear sự kiện: sinh vật vừa hiện lại → đặt thẳng.</summary>
        public void ApplyEvents(IReadOnlyList<SimEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Kind != SimEventKind.CreatureRespawned || e.CreatureId < 0 || e.CreatureId >= tracks.Count) continue;
                var t = tracks[e.CreatureId];
                t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                tracks[e.CreatureId] = t;
            }
        }

        /// <summary>Vị trí hiển thị (đơn vị ô) của góc nhỏ nhất của khung; alpha bị kẹp vào [0, 1].</summary>
        public void Sample(int creatureId, double alpha, out double x, out double y, out double z)
        {
            if (alpha < 0) alpha = 0;
            else if (alpha > 1) alpha = 1;
            var t = tracks[creatureId];
            x = (t.PX + (t.CX - t.PX) * alpha) / 1000.0;
            y = (t.PY + (t.CY - t.PY) * alpha) / 1000.0;
            z = (t.PZ + (t.CZ - t.PZ) * alpha) / 1000.0;
        }

        /// <summary>Hướng đi ngang gần nhất (không bao giờ là 0, 0); ban đầu (0, 1) = +Z.</summary>
        public void Heading(int creatureId, out int hx, out int hz)
        {
            var t = tracks[creatureId];
            hx = t.HX;
            hz = t.HZ;
        }
    }
}
