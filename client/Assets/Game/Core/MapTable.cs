using System;

namespace Game.Core
{
    /// <summary>Bảng map (glossary): mỗi cột (x, z) thuộc map nào; Ocean = cột biển, không thuộc map nào (decision 0022).</summary>
    public sealed class MapTable
    {
        public const byte Ocean = 255;

        public readonly int SizeX, SizeZ;
        readonly byte[] ids;
        readonly string[] names;

        public MapTable(int sizeX, int sizeZ, byte[] ids, string[] names)
        {
            if (ids == null || ids.Length != sizeX * sizeZ)
                throw new ArgumentException("ids phải có đúng sizeX * sizeZ phần tử");
            if (names == null)
                throw new ArgumentException("names không được null");
            SizeX = sizeX;
            SizeZ = sizeZ;
            this.ids = ids;
            this.names = names;
        }

        public byte[] Ids => ids;
        public int MapCount => names.Length;
        public string Name(int map) => names[map];
        public byte Get(int x, int z) => ids[x + SizeX * z];
    }
}
