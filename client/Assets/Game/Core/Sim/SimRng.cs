using System;

namespace Game.Core.Sim
{
    /// <summary>PCG32 (pcg-c-basic, pcg-random.org) — bộ sinh số ngẫu nhiên riêng của mô phỏng, chỉ phép toán số nguyên.</summary>
    public sealed class SimRng
    {
        public ulong State;
        public ulong Inc;

        public SimRng(ulong initState, ulong initSeq)
        {
            State = 0UL;
            Inc = (initSeq << 1) | 1UL;
            NextUInt();
            State = unchecked(State + initState);
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = State;
            State = unchecked(old * 6364136223846793005UL + Inc);
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        /// <summary>Số nguyên đều trong [0, bound), không lệch (loại bỏ phần dư như pcg32_boundedrand_r).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
            uint b = (uint)bound;
            uint threshold = unchecked(0u - b) % b;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % b);
            }
        }
    }
}
