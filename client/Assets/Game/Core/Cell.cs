namespace Game.Core
{
    /// <summary>Vật liệu của một ô gameplay. 0 = rỗng. Giá trị lưu trong 8 bit thấp của ô.</summary>
    public enum BlockMaterial : byte
    {
        Empty = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Sand = 4,
        Water = 5,
        /// <summary>Đá đáy map — không đào được (luật M1, design/02-gameplay/mechanics.md).</summary>
        Bedrock = 6,
        /// <summary>Ô ruộng: ô đất/cỏ sinh vật đã xới xong (0020). Không đặt được; đào ra +1 đất; bị che thì trở lại đất.</summary>
        Farmland = 7,
    }

    /// <summary>
    /// Một ô gameplay = 2 byte (design/04-tech/world-tech.md "Dữ liệu world"):
    /// bit 0–7 vật liệu · bit 8 cờ khối cốt truyện · bit 9 cờ đã sửa · bit 10–12 mực nước 0–7 · bit 13 nước đang chảy · bit 14 nước đang rơi · bit 15 nước đang rút.
    /// </summary>
    public static class Cell
    {
        public const int MaxWaterLevel = 7;

        const ushort MaterialMask = 0x00FF;
        const ushort StoryFlag = 1 << 8;
        const ushort ModifiedFlag = 1 << 9;
        public const int WaterShift = 10;
        const ushort WaterMask = 0x7 << WaterShift;
        const ushort FlowingFlag = 1 << 13; // nước đang chảy (tắt = nguồn) — 0040
        const ushort FallingFlag = 1 << 14; // nước đang rơi — 0040
        const ushort DrainingFlag = 1 << 15; // nước đang rút — 0041
        const int ReservedShift = 13;
        const ushort ReservedMask = 0x7 << ReservedShift;

        public static ushort Make(BlockMaterial material, int waterLevel = 0)
        {
            return SetWaterLevel((ushort)material, waterLevel);
        }

        public static BlockMaterial GetMaterial(ushort cell) => (BlockMaterial)(cell & MaterialMask);

        public static ushort SetMaterial(ushort cell, BlockMaterial material) =>
            (ushort)((cell & ~MaterialMask) | (byte)material);

        public static bool IsStory(ushort cell) => (cell & StoryFlag) != 0;

        public static ushort SetStory(ushort cell, bool value) =>
            value ? (ushort)(cell | StoryFlag) : (ushort)(cell & ~StoryFlag);

        public static bool IsModified(ushort cell) => (cell & ModifiedFlag) != 0;

        public static ushort SetModified(ushort cell, bool value) =>
            value ? (ushort)(cell | ModifiedFlag) : (ushort)(cell & ~ModifiedFlag);

        public static int GetWaterLevel(ushort cell) => (cell & WaterMask) >> WaterShift;

        public static ushort SetWaterLevel(ushort cell, int level)
        {
            if (level < 0 || level > MaxWaterLevel)
                throw new System.ArgumentOutOfRangeException(nameof(level), level, "Water level 0–7");
            return (ushort)((cell & ~WaterMask) | (level << WaterShift));
        }

        public static int GetReserved(ushort cell) => (cell & ReservedMask) >> ReservedShift;

        public static ushort SetReserved(ushort cell, int value)
        {
            if (value < 0 || value > 7)
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Reserved 3 bits");
            return (ushort)((cell & ~ReservedMask) | (value << ReservedShift));
        }

        public static bool IsFlowing(ushort cell) => (cell & FlowingFlag) != 0;

        public static bool IsFalling(ushort cell) => (cell & FallingFlag) != 0;

        public static bool IsDraining(ushort cell) => (cell & DrainingFlag) != 0;

        public static ushort SetDraining(ushort cell, bool value) =>
            value ? (ushort)(cell | DrainingFlag) : (ushort)(cell & ~DrainingFlag);

        /// <summary>Ô vật liệu Water và cờ đang chảy tắt (mực bất kỳ).</summary>
        public static bool IsWaterSource(ushort cell) =>
            GetMaterial(cell) == BlockMaterial.Water && (cell & FlowingFlag) == 0;

        /// <summary>Ô nước đang chảy: Water, cờ chảy bật, mực = level (1–7), cờ rơi theo falling; giữ cờ cốt truyện/đã sửa của cell.</summary>
        public static ushort MakeFlowing(ushort cell, int level, bool falling, bool draining = false)
        {
            if (level < 1 || level > MaxWaterLevel)
                throw new System.ArgumentOutOfRangeException(nameof(level), level, "Flowing water level 1–7");
            cell = SetWaterLevel(SetMaterial(cell, BlockMaterial.Water), level);
            cell = (ushort)(cell | FlowingFlag);
            cell = falling ? (ushort)(cell | FallingFlag) : (ushort)(cell & ~FallingFlag);
            return SetDraining(cell, draining);
        }

        /// <summary>Bỏ cờ chảy/rơi (khi ô nước bị đặt khối đè).</summary>
        public static ushort ClearFlow(ushort cell) => (ushort)(cell & ~(FlowingFlag | FallingFlag | DrainingFlag));

        /// <summary>Ô rỗng sau khi nước rút: bỏ vật liệu, mực, cờ chảy/rơi; cờ đã sửa bật.</summary>
        public static ushort ClearWater(ushort cell)
        {
            cell = SetWaterLevel(SetMaterial(cell, BlockMaterial.Empty), 0);
            return SetModified((ushort)(cell & ~(FlowingFlag | FallingFlag | DrainingFlag)), true);
        }

        /// <summary>Ô có thể đào: không rỗng, không phải nước, không phải đá đáy, không thuộc khối cốt truyện.</summary>
        public static bool IsDiggable(ushort cell)
        {
            var m = GetMaterial(cell);
            return m != BlockMaterial.Empty && m != BlockMaterial.Water && m != BlockMaterial.Bedrock && !IsStory(cell);
        }
    }
}
