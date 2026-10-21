using System;

namespace Game.Core.Sim
{
    /// <summary>
    /// Vùng mô phỏng (world 7e, brief 0019): hộp cột [X0, X0 + SizeX) × [Z0, Z0 + SizeZ), đủ chiều cao world.
    /// Tìm đường chỉ đi trong vùng → bộ nhớ theo vùng, không theo cả world.
    /// </summary>
    public readonly struct SimBounds : IEquatable<SimBounds>
    {
        public readonly int X0, Z0, SizeX, SizeZ;

        public SimBounds(int x0, int z0, int sizeX, int sizeZ)
        {
            if (sizeX < 1 || sizeZ < 1) throw new ArgumentException("Vùng mô phỏng phải có kích thước ≥ 1");
            X0 = x0;
            Z0 = z0;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }

        public static SimBounds Whole(WorldSize size) => new SimBounds(0, 0, size.X, size.Z);

        public bool Contains(int x, int z) => x >= X0 && x < X0 + SizeX && z >= Z0 && z < Z0 + SizeZ;

        public bool FitsIn(WorldSize size) => X0 >= 0 && Z0 >= 0 && X0 + SizeX <= size.X && Z0 + SizeZ <= size.Z;

        public bool Equals(SimBounds o) => X0 == o.X0 && Z0 == o.Z0 && SizeX == o.SizeX && SizeZ == o.SizeZ;
        public override bool Equals(object obj) => obj is SimBounds o && Equals(o);
        public override int GetHashCode() => ((X0 * 397 ^ Z0) * 397 ^ SizeX) * 397 ^ SizeZ;
        public static bool operator ==(SimBounds a, SimBounds b) => a.Equals(b);
        public static bool operator !=(SimBounds a, SimBounds b) => !a.Equals(b);
        public override string ToString() => $"x {X0}..{X0 + SizeX - 1}, z {Z0}..{Z0 + SizeZ - 1}";
    }
}
