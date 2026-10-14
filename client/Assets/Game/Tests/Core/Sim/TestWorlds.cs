using Game.Core;

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
    }
}
