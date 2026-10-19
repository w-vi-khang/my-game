using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;

namespace Game.Core.Tests
{
    public static class TestWorlds
    {
        /// <summary>Mặt phẳng 16×16×16: y = 0 đá đáy, y = 1..groundTop đất. Sinh vật đứng ở y = groundTop + 1.</summary>
        public static WorldData Flat(int groundTop)
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int x = 0; x < 16; x++)
                for (int z = 0; z < 16; z++)
                {
                    w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                    for (int y = 1; y <= groundTop; y++) w.Set(x, y, z, Cell.Make(BlockMaterial.Dirt));
                }
            return w;
        }

        /// <summary>Tường vật liệu Stone tại x = k, y từ 1..m, mọi z.</summary>
        public static void Wall(WorldData w, int k, int m)
        {
            for (int z = 0; z < 16; z++)
                for (int y = 1; y <= m; y++) w.Set(k, y, z, Cell.Make(BlockMaterial.Stone));
        }

        /// <summary>Cột Stone tại (x, z), y từ 1..m.</summary>
        public static void Pillar(WorldData w, int x, int z, int m)
        {
            for (int y = 1; y <= m; y++) w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
        }

        /// <summary>
        /// Dùng với Flat(1): hành lang 1 ô dọc trục X tại z = zc, ô đứng y = 2, x từ x0..x1. Hai bên (z = zc ± 1) và hai đầu
        /// (x0 − 1, x1 + 1) là cột Stone y 1..3 (không leo được). pocketX ≥ 0: thêm ô tránh (pocketX, 2, zc + 1), chặn ở (pocketX, zc + 2).
        /// </summary>
        public static void Corridor(WorldData w, int x0, int x1, int zc, int pocketX = -1)
        {
            for (int x = x0 - 1; x <= x1 + 1; x++)
            {
                if (x != pocketX) Pillar(w, x, zc + 1, 3);
                Pillar(w, x, zc - 1, 3);
            }
            Pillar(w, x0 - 1, zc, 3);
            Pillar(w, x1 + 1, zc, 3);
            if (pocketX >= 0) Pillar(w, pocketX, zc + 2, 3);
        }

        static readonly int[,] HomeAnchors = { { 14, 14 }, { 14, 11 }, { 11, 14 } };

        /// <summary>Gắn nhà cho mọi sinh vật chưa có nhà (theo id, đủ sức chứa thì sang nhà kế). Neo nhà lần lượt (14, y, 14), (14, y, 11), (11, y, 14); y = ô Empty thấp nhất của cột neo.</summary>
        public static void HomeAll(Simulation sim)
        {
            int current = -1;
            int created = 0;
            var cs = sim.State.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                if (cs[i].HomeId >= 0) continue;
                if (current < 0 || sim.State.Houses[current].Residents.Count >= sim.State.Houses[current].Capacity)
                {
                    if (created >= HomeAnchors.GetLength(0)) throw new InvalidOperationException("Quá 3 nhà trong HomeAll");
                    int x = HomeAnchors[created, 0], z = HomeAnchors[created, 1];
                    int y = 0;
                    while (sim.World.GetMaterial(x, y, z) != BlockMaterial.Empty) y++;
                    current = sim.AddHouse(new CellPos(x, y, z));
                    created++;
                }
                sim.AssignHome(i, current);
            }
        }
    }
}
