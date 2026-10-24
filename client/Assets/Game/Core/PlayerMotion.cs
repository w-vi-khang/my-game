using System;
using Game.Core.Sim;

namespace Game.Core
{
    /// <summary>Di chuyển PC (0034 mục 1–2, world 7l b): số đọc từ sim_m1.json player, luật tự nhảy bậc. Client gọi.</summary>
    public static class PlayerMotion
    {
        public static float DashCells { get; private set; } = 4f;
        public static float DashSeconds { get; private set; } = 0.15f;
        public static float DashCooldownSeconds { get; private set; } = 3f;
        public static float StuckSeconds { get; private set; } = 0.3f;
        public static float ClickMarkSeconds { get; private set; } = 0.5f;
        /// <summary>Vòng tầm mờ hết sau chừng này giây (0036 mục 5, số tạm).</summary>
        public static float ReachRingFadeSeconds { get; private set; } = 1f;
        /// <summary>F Thu: vòng vùng + gió hiện chừng này giây (0036 mục 6, số tạm).</summary>
        public static float CollectShowSeconds { get; private set; } = 0.5f;
        /// <summary>Vật rơi bay về nhân vật chừng này giây (0036 mục 6, số tạm).</summary>
        public static float CollectFlySeconds { get; private set; } = 0.3f;
        /// <summary>Thanh mana nháy chừng này giây khi không đủ mana (0034 mục 5, số tạm).</summary>
        public static float ManaFlashSeconds { get; private set; } = 0.6f;
        /// <summary>Dòng thông báo HUD tự ẩn sau chừng này giây (0034 mục 7, số tạm).</summary>
        public static float HudMessageSeconds { get; private set; } = 4f;
        /// <summary>Cỡ icon ô ruộng / sinh vật trên world (px ở 720p) và khoảng cách xa nhất còn hiện icon (ô) — 0034 mục 7, số tạm.</summary>
        public static float PlotIconPx { get; private set; } = 40f;
        public static float CreatureIconPx { get; private set; } = 28f;
        public static float IconMaxCells { get; private set; } = 30f;
        public static float CameraPitch { get; private set; } = 10f;
        public static float CameraDistance { get; private set; } = 9f;
        public static float FirstPersonPitch { get; private set; } = 15f;
        /// <summary>Camera quan sát: cao hơn mặt trên khối được click chừng này ô (0035).</summary>
        public static float ObservationHeight { get; private set; } = 2f;
        /// <summary>Tới đích khi khoảng cách ngang ≤ chừng này (ô).</summary>
        public const float ArriveDistance = 0.05f;
        /// <summary>Nhìn trước chừng này (ô) theo hướng đi khi xét tự nhảy bậc.</summary>
        public const float StepLookAhead = 0.7f;

        /// <summary>null → ArgumentNullException. ms → giây (/1000f).</summary>
        public static void Configure(PlayerConfig p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            DashCells = p.dashCells;
            DashSeconds = p.dashMs / 1000f;
            DashCooldownSeconds = p.dashCooldownMs / 1000f;
            StuckSeconds = p.stuckMs / 1000f;
            ClickMarkSeconds = p.clickMarkMs / 1000f;
            ReachRingFadeSeconds = p.reachRingFadeMs / 1000f;
            CollectShowSeconds = p.collectShowMs / 1000f;
            CollectFlySeconds = p.collectFlyMs / 1000f;
            ManaFlashSeconds = p.manaFlashMs / 1000f;
            HudMessageSeconds = p.hudMessageMs / 1000f;
            PlotIconPx = p.plotIconPx;
            CreatureIconPx = p.creatureIconPx;
            IconMaxCells = p.iconMaxCells;
            CameraPitch = p.cameraPitch;
            CameraDistance = p.cameraDistance;
            FirstPersonPitch = p.firstPersonPitch;
            ObservationHeight = p.observationHeight;
        }

        /// <summary>Tự nhảy bậc 1 ô: ô phía trước ở tầng chân đặc và hai ô phía trên nó không đặc.</summary>
        public static bool ShouldStepUp(WorldData world, float feetX, float feetY, float feetZ, float dirX, float dirZ)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (dirX * dirX + dirZ * dirZ < 1e-6f) return false;
            int px = (int)Math.Floor(feetX + dirX * StepLookAhead);
            int py = (int)Math.Floor(feetY + 0.05f);
            int pz = (int)Math.Floor(feetZ + dirZ * StepLookAhead);
            return WorldEditor.IsSolidAt(world, px, py, pz)
                && !WorldEditor.IsSolidAt(world, px, py + 1, pz)
                && !WorldEditor.IsSolidAt(world, px, py + 2, pz);
        }
    }

    /// <summary>Đếm thời gian liền không tiến được (kẹt).</summary>
    public sealed class StuckTimer
    {
        public StuckTimer(float seconds)
        {
            if (seconds <= 0f) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Seconds must be > 0");
            Seconds = seconds;
        }

        public float Seconds { get; }
        public float Elapsed { get; private set; }

        /// <summary>dt &lt; 0 → ném. progressed → Elapsed = 0, trả false. Không → Elapsed += dt, trả Elapsed ≥ Seconds.</summary>
        public bool Update(float dt, bool progressed)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt must be ≥ 0");
            if (progressed) { Elapsed = 0f; return false; }
            Elapsed += dt;
            return Elapsed >= Seconds;
        }

        public void Reset() => Elapsed = 0f;
    }

    /// <summary>Lướt (Shift): quãng Cells trong Seconds, hồi Cooldown tính từ lúc bắt đầu, trên không 1 lần mỗi lần rời đất.</summary>
    public sealed class DashState
    {
        public DashState(float cells, float seconds, float cooldownSeconds)
        {
            if (cells <= 0f) throw new ArgumentOutOfRangeException(nameof(cells), cells, "Cells must be > 0");
            if (seconds <= 0f) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Seconds must be > 0");
            Cells = cells;
            Seconds = seconds;
            Cooldown = new Cooldown(cooldownSeconds);
        }

        public float Cells { get; }
        public float Seconds { get; }
        public float Speed => Cells / Seconds;
        public Cooldown Cooldown { get; }
        public bool Active { get; private set; }
        public bool AirUsed { get; private set; }
        /// <summary>Số ô còn phải lướt.</summary>
        public float Remaining { get; private set; }

        /// <summary>Được khi !Active, Cooldown.Ready và (grounded hoặc !AirUsed).</summary>
        public bool TryStart(bool grounded)
        {
            if (Active || !Cooldown.Ready || (!grounded && AirUsed)) return false;
            Active = true;
            Remaining = Cells;
            Cooldown.Start();
            if (!grounded) AirUsed = true;
            return true;
        }

        /// <summary>dt &lt; 0 → ném. Trả quãng (ô) lướt trong dt; không Active → 0.</summary>
        public float Step(float dt)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt must be ≥ 0");
            if (!Active) return 0f;
            float d = Math.Min(Speed * dt, Remaining);
            Remaining -= d;
            if (Remaining <= 1e-4f) { Active = false; Remaining = 0f; }
            return d;
        }

        /// <summary>Chạm khối: dừng lướt.</summary>
        public void Stop()
        {
            Active = false;
            Remaining = 0f;
        }

        /// <summary>Mỗi frame: hồi chiêu; chạm đất và không lướt → được lướt trên không lần nữa.</summary>
        public void Tick(float dt, bool grounded)
        {
            Cooldown.Advance(dt);
            if (grounded && !Active) AirUsed = false;
        }
    }
}
