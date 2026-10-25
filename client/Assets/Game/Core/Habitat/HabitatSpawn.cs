using System.Collections.Generic;
using Game.Core.Sim;

namespace Game.Core.Habitat
{
    /// <summary>Sinh cá thể khi loài trở lại (sim 8b, 0039): ở ô rìa vùng gần người chơi nhất.</summary>
    public static class HabitatSpawn
    {
        public const int CountPerSpecies = 2; // luật M1: 2 cá thể

        /// <summary>Thêm tối đa count sinh vật speciesId ở ô rìa của vùng [x0, x1) × [z0, z1) gần (px, pz) nhất; mỗi con vào nhà đầu tiên còn chỗ (id tăng dần), hết chỗ thì không nhà. Ghi id mới vào created; trả số con đã thêm.</summary>
        public static int SpawnAtEdge(Simulation sim, int x0, int z0, int x1, int z1, int px, int pz, string speciesId, int count, List<int> created)
        {
            var cols = new List<(int d, int x, int z)>();
            for (int z = z0; z < z1; z++)
                for (int x = x0; x < x1; x++)
                    if (x == x0 || x == x1 - 1 || z == z0 || z == z1 - 1)
                        cols.Add(((x - px) * (x - px) + (z - pz) * (z - pz), x, z));
            cols.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));
            var world = sim.World;
            int added = 0;
            for (int i = 0; i < cols.Count && added < count; i++)
            {
                int x = cols[i].x, z = cols[i].z;
                int y = world.Size.Y - 1;
                while (y >= 0 && Cell.GetMaterial(world.Get(x, y, z)) == BlockMaterial.Empty) y--;
                if (y < 0 || Cell.GetMaterial(world.Get(x, y, z)) == BlockMaterial.Water) continue;
                var cell = new CellPos(x, y + 1, z);
                if (!sim.CanPlaceCreature(speciesId, cell)) continue;
                int id = sim.AddCreature(speciesId, cell);
                var houses = sim.State.Houses;
                for (int h = 0; h < houses.Count; h++)
                    if (houses[h].Residents.Count < houses[h].Capacity)
                    {
                        sim.AssignHome(id, h);
                        break;
                    }
                created?.Add(id);
                added++;
            }
            return added;
        }
    }
}
