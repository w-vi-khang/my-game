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
    /// bit 0–7 vật liệu · bit 8 cờ khối cốt truyện · bit 9 cờ đã sửa · bit 10–12 mực nước 0–7 · bit 13–15 dự phòng.
    /// </summary>
    public static class Cell
    {
        public const int MaxWaterLevel = 7;

        const ushort MaterialMask = 0x00FF;
        const ushort StoryFlag = 1 << 8;
        const ushort ModifiedFlag = 1 << 9;
        public const int WaterShift = 10;
        const ushort WaterMask = 0x7 << WaterShift;
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
                throw new System.ArgumentOutOfRangeException(nameof(level), level, "Mực nước 0–7");
            return (ushort)((cell & ~WaterMask) | (level << WaterShift));
        }

        public static int GetReserved(ushort cell) => (cell & ReservedMask) >> ReservedShift;

        public static ushort SetReserved(ushort cell, int value)
        {
            if (value < 0 || value > 7)
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Dự phòng 3 bit");
            return (ushort)((cell & ~ReservedMask) | (value << ReservedShift));
        }

        /// <summary>Ô có thể đào: không rỗng, không phải nước, không phải đá đáy, không thuộc khối cốt truyện.</summary>
        public static bool IsDiggable(ushort cell)
        {
            var m = GetMaterial(cell);
            return m != BlockMaterial.Empty && m != BlockMaterial.Water && m != BlockMaterial.Bedrock && !IsStory(cell);
        }
    }
}
