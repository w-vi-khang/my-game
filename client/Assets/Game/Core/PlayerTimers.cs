using System;

namespace Game.Core
{
    /// <summary>Đồng hồ việc tay của người chơi (tự xới 7f): chạy theo thời gian thực ở client.</summary>
    public sealed class WorkTimer
    {
        public WorkTimer(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Thời lượng phải > 0");
            Duration = durationSeconds;
        }

        public float Duration { get; }
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        public void Start()
        {
            Elapsed = 0f;
            Running = true;
        }

        public void Cancel()
        {
            Elapsed = 0f;
            Running = false;
        }

        /// <summary>Đang chạy: Elapsed += dt. Elapsed ≥ Duration → Elapsed = Duration, Running = false, trả true (đúng một lần). Không chạy → false.</summary>
        public bool Advance(float dt)
        {
            if (dt < 0f)
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt phải ≥ 0");
            if (!Running) return false;
            Elapsed += dt;
            if (Elapsed < Duration) return false;
            Elapsed = Duration;
            Running = false;
            return true;
        }
    }

    /// <summary>Thời gian hồi của phép (Mưa 7f): chạy theo thời gian thực ở client.</summary>
    public sealed class Cooldown
    {
        public Cooldown(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Thời lượng phải > 0");
            Duration = durationSeconds;
        }

        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool Ready => Remaining <= 0f;

        public void Start() => Remaining = Duration;

        public void Advance(float dt)
        {
            if (dt < 0f)
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt phải ≥ 0");
            Remaining = Math.Max(0f, Remaining - dt);
        }

        /// <summary>Số giây còn lại làm tròn lên (HUD "Mưa (R): 7 giây"); 0 khi sẵn sàng.</summary>
        public int RemainingWholeSeconds => (int)Math.Ceiling(Remaining);
    }
}
