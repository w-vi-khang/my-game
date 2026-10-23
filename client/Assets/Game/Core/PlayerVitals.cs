using System;
using Game.Core.Sim;

namespace Game.Core
{
    public enum ManaAction : byte { Dig = 0, Place = 1, Rain = 2 }

    /// <summary>Máu + mana của nhân vật (0034 mục 5). Số lấy từ PlayerConfig lúc tạo.</summary>
    public sealed class PlayerVitals
    {
        readonly int manaDig, manaPlace, manaRain, manaRegenPerCell;
        readonly int fallSafeCells, fallDamagePerCell, drownDamagePerSecond, hpRegenPerSecond, hpRegenDelayMs;
        float movedAcc;
        int drownMs, sinceDamageMs, regenMs;

        public PlayerVitals(PlayerConfig c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            HpMax = c.hpMax;
            ManaMax = c.manaMax;
            manaDig = c.manaDig;
            manaPlace = c.manaPlace;
            manaRain = c.manaRain;
            manaRegenPerCell = c.manaRegenPerCell;
            fallSafeCells = c.fallSafeCells;
            fallDamagePerCell = c.fallDamagePerCell;
            drownDamagePerSecond = c.drownDamagePerSecond;
            hpRegenPerSecond = c.hpRegenPerSecond;
            hpRegenDelayMs = c.hpRegenDelayMs;
            Hp = HpMax;
            Mana = ManaMax;
        }

        public int Hp { get; private set; }
        public int HpMax { get; }
        public int Mana { get; private set; }
        public int ManaMax { get; }
        public bool IsDead => Hp <= 0;
        /// <summary>Phát sau mỗi lần Hp hoặc Mana đổi giá trị.</summary>
        public event Action Changed;
        /// <summary>Phát đúng 1 lần khi Hp từ &gt; 0 về 0.</summary>
        public event Action Died;

        public int Cost(ManaAction a)
        {
            switch (a)
            {
                case ManaAction.Dig: return manaDig;
                case ManaAction.Place: return manaPlace;
                default: return manaRain;
            }
        }

        public bool CanSpend(ManaAction a) => !IsDead && Mana >= Cost(a);

        public bool TrySpend(ManaAction a)
        {
            if (!CanSpend(a)) return false;
            int cost = Cost(a);
            if (cost > 0)
            {
                Mana -= cost;
                Changed?.Invoke();
            }
            return true;
        }

        /// <summary>Mana ngấm theo quãng đi ngang (số ô).</summary>
        public void AddMoved(float horizontalCells)
        {
            if (horizontalCells < 0f)
                throw new ArgumentOutOfRangeException(nameof(horizontalCells), horizontalCells, "Moved distance must be >= 0");
            if (IsDead) return;
            movedAcc += horizontalCells;
            bool changed = false;
            while (movedAcc >= 1f)
            {
                movedAcc -= 1f;
                int next = Math.Min(ManaMax, Mana + manaRegenPerCell);
                if (next != Mana) { Mana = next; changed = true; }
            }
            if (changed) Changed?.Invoke();
        }

        /// <summary>Chạm đất sau khi rơi fallCells ô; trả số máu đã mất.</summary>
        public int Land(float fallCells)
        {
            if (fallCells < 0f)
                throw new ArgumentOutOfRangeException(nameof(fallCells), fallCells, "Fall distance must be >= 0");
            int over = (int)Math.Floor(fallCells - fallSafeCells + 1e-4f);
            int dmg = over > 0 ? over * fallDamagePerCell : 0;
            return Damage(dmg);
        }

        public void Tick(int ms, bool headUnderwater)
        {
            if (ms < 0)
                throw new ArgumentOutOfRangeException(nameof(ms), ms, "Elapsed time must be >= 0");
            if (IsDead) return;
            if (headUnderwater)
            {
                drownMs += ms;
                while (drownMs >= 1000)
                {
                    drownMs -= 1000;
                    Damage(drownDamagePerSecond);
                    if (IsDead) return;
                }
                sinceDamageMs = 0;
                regenMs = 0;
                return;
            }
            drownMs = 0;
            int before = sinceDamageMs;
            sinceDamageMs += ms;
            if (sinceDamageMs > hpRegenDelayMs)
            {
                regenMs += sinceDamageMs - Math.Max(before, hpRegenDelayMs);
                while (regenMs >= 1000)
                {
                    regenMs -= 1000;
                    if (Hp < HpMax)
                    {
                        Hp = Math.Min(HpMax, Hp + hpRegenPerSecond);
                        Changed?.Invoke();
                    }
                }
            }
        }

        public void Respawn()
        {
            Hp = HpMax;
            Mana = ManaMax;
            movedAcc = 0f;
            drownMs = 0;
            sinceDamageMs = 0;
            regenMs = 0;
            Changed?.Invoke();
        }

        int Damage(int n)
        {
            if (n <= 0 || IsDead) return 0;
            int lost = Math.Min(Hp, n);
            Hp -= lost;
            sinceDamageMs = 0;
            regenMs = 0;
            Changed?.Invoke();
            if (Hp == 0) Died?.Invoke();
            return lost;
        }
    }
}
