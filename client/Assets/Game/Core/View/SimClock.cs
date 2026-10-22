using System;

namespace Game.Core.View
{
    /// <summary>
    /// Đếm nhịp cho phần hiển thị (sim bước 6): cộng thời gian khung hình theo micro giây số nguyên → số nhịp phải chạy trong khung;
    /// Alpha = phần nhịp đã trôi (0..1), dùng để nội suy. Không thuộc trạng thái mô phỏng (chỉ quyết định khi nào gọi Tick).
    /// </summary>
    public sealed class SimClock
    {
        public readonly long TickMicros;
        public readonly int MaxTicksPerFrame;
        long accumulator;

        public SimClock(int ticksPerSecond, int maxTicksPerFrame = 5)
        {
            if (ticksPerSecond <= 0 || 1000000 % ticksPerSecond != 0)
                throw new ArgumentException("ticksPerSecond must be positive and divide 1000000: " + ticksPerSecond);
            if (maxTicksPerFrame < 1)
                throw new ArgumentException("maxTicksPerFrame must be >= 1: " + maxTicksPerFrame);
            TickMicros = 1000000 / ticksPerSecond;
            MaxTicksPerFrame = maxTicksPerFrame;
        }

        public long AccumulatorMicros => accumulator;
        public double Alpha => (double)accumulator / TickMicros;

        /// <summary>Cộng thời gian một khung; trả số nhịp phải chạy. Quá MaxTicksPerFrame → chạy MaxTicksPerFrame, bỏ phần dư (không đuổi theo).</summary>
        public int Advance(double deltaSeconds)
        {
            if (!(deltaSeconds > 0)) return 0;
            if (deltaSeconds > 10) deltaSeconds = 10;
            accumulator += (long)Math.Round(deltaSeconds * 1000000.0);
            long n = accumulator / TickMicros;
            if (n > MaxTicksPerFrame)
            {
                accumulator = 0;
                return MaxTicksPerFrame;
            }
            accumulator -= n * TickMicros;
            return (int)n;
        }
    }
}
