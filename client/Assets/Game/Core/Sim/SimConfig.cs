using System;

namespace Game.Core.Sim
{
    /// <summary>Năng lực làm việc: xới, gieo, tưới, thu hoạch, vận chuyển.</summary>
    public enum WorkAbility : byte { Till = 0, Sow = 1, Water = 2, Harvest = 3, Haul = 4 }

    [Serializable]
    public class StageTime
    {
        public string ability;
        public int baseTimeMs;

        public StageTime() { }
        public StageTime(string ability, int baseTimeMs)
        {
            this.ability = ability;
            this.baseTimeMs = baseTimeMs;
        }
    }

    [Serializable]
    public class SpeciesWork
    {
        public string id;
        public string ability1;
        public string ability2;
        public int mastery1;
        public int mastery2;
        public int moveSpeedMilliCellsPerSecond;
        public int sizeX = 1;
        public int sizeZ = 1;
        public int sizeY = 1;

        public SpeciesWork() { }
        public SpeciesWork(string id, string ability1, string ability2, int mastery1, int mastery2, int moveSpeedMilliCellsPerSecond)
        {
            this.id = id;
            this.ability1 = ability1;
            this.ability2 = ability2;
            this.mastery1 = mastery1;
            this.mastery2 = mastery2;
            this.moveSpeedMilliCellsPerSecond = moveSpeedMilliCellsPerSecond;
        }

        public SpeciesWork(string id, string ability1, string ability2, int mastery1, int mastery2, int moveSpeedMilliCellsPerSecond, int sizeX, int sizeZ, int sizeY)
            : this(id, ability1, ability2, mastery1, mastery2, moveSpeedMilliCellsPerSecond)
        {
            this.sizeX = sizeX;
            this.sizeZ = sizeZ;
            this.sizeY = sizeY;
        }
    }

    [Serializable]
    public class CropConfig
    {
        public int growTimeMs = 120000;
        public int seedsPerHarvest = 1;
        public int producePerHarvest = 1;
    }

    [Serializable]
    public class StorageConfig
    {
        public int capacity = 50;
        public int startSeeds = 10;
    }

    [Serializable]
    public class FarmConfig
    {
        public int plotQueueCapacity = 2;
    }

    /// <summary>
    /// Cấu hình mô phỏng M1. Nguồn chuẩn: design/data/sim_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Số là số tạm (user 2026-10-14), chỉnh khi chơi thử.
    /// </summary>
    [Serializable]
    public class SimConfig
    {
        public int configVersion = 1;
        public uint simSeed = 20261014;
        public int ticksPerSecond = 10;
        public StageTime[] stageTimes =
        {
            new StageTime("Till", 6000),
            new StageTime("Sow", 3000),
            new StageTime("Water", 3000),
            new StageTime("Harvest", 4000),
            new StageTime("Haul", 2000),
        };
        public SpeciesWork[] species =
        {
            new SpeciesWork("L1", "Till", "Sow", 100, 100, 1500),
            new SpeciesWork("L2", "Water", "Harvest", 100, 100, 1500),
            new SpeciesWork("L3", "Haul", "Harvest", 100, 100, 2000),
        };
        public CropConfig crop = new CropConfig();
        public StorageConfig storage = new StorageConfig();
        public FarmConfig farm = new FarmConfig();

        public static WorkAbility ParseAbility(string s)
        {
            if (!string.IsNullOrEmpty(s) && char.IsLetter(s[0])
                && Enum.TryParse(s, false, out WorkAbility a) && Enum.IsDefined(typeof(WorkAbility), a))
                return a;
            throw new ArgumentException("Năng lực không hợp lệ: " + s);
        }

        public int MsToTicks(int ms) => ms * ticksPerSecond / 1000;

        public int GrowTicks => MsToTicks(crop.growTimeMs);

        public int BaseTicks(WorkAbility a)
        {
            for (int i = 0; i < stageTimes.Length; i++)
                if (ParseAbility(stageTimes[i].ability) == a) return MsToTicks(stageTimes[i].baseTimeMs);
            throw new ArgumentException("Không có thời gian cho năng lực: " + a);
        }

        public SpeciesWork FindSpecies(string id)
        {
            for (int i = 0; i < species.Length; i++)
                if (string.Equals(species[i].id, id, StringComparison.Ordinal)) return species[i];
            throw new ArgumentException("Không có loài: " + id);
        }

        public int SpeedPerTick(SpeciesWork s) => s.moveSpeedMilliCellsPerSecond / ticksPerSecond;

        public void Validate()
        {
            if (ticksPerSecond <= 0 || 1000 % ticksPerSecond != 0)
                throw new ArgumentException("ticksPerSecond phải > 0 và là ước của 1000.");
            int msPerTick = 1000 / ticksPerSecond;

            int abilityCount = Enum.GetValues(typeof(WorkAbility)).Length;
            if (stageTimes == null || stageTimes.Length != abilityCount)
                throw new ArgumentException("stageTimes phải có đúng " + abilityCount + " phần tử (mỗi năng lực một lần).");
            var seen = new bool[abilityCount];
            for (int i = 0; i < stageTimes.Length; i++)
            {
                var a = ParseAbility(stageTimes[i].ability);
                if (seen[(int)a]) throw new ArgumentException("stageTimes trùng năng lực: " + a);
                seen[(int)a] = true;
                if (stageTimes[i].baseTimeMs <= 0 || stageTimes[i].baseTimeMs % msPerTick != 0)
                    throw new ArgumentException("baseTimeMs phải > 0 và chia hết cho " + msPerTick + " (" + a + ").");
            }

            if (species == null || species.Length == 0)
                throw new ArgumentException("species không được rỗng.");
            for (int i = 0; i < species.Length; i++)
            {
                var sp = species[i];
                if (string.IsNullOrEmpty(sp.id)) throw new ArgumentException("Loài thiếu id.");
                for (int j = 0; j < i; j++)
                    if (string.Equals(species[j].id, sp.id, StringComparison.Ordinal))
                        throw new ArgumentException("Trùng id loài: " + sp.id);
                var a1 = ParseAbility(sp.ability1);
                var a2 = ParseAbility(sp.ability2);
                if (a1 == a2) throw new ArgumentException("Loài " + sp.id + " có hai năng lực giống nhau.");
                if (sp.mastery1 < 1 || sp.mastery2 < 1)
                    throw new ArgumentException("Loài " + sp.id + ": mastery phải >= 1.");
                if (sp.moveSpeedMilliCellsPerSecond % ticksPerSecond != 0)
                    throw new ArgumentException("Loài " + sp.id + ": tốc độ phải chia hết cho ticksPerSecond.");
                int spt = SpeedPerTick(sp);
                if (spt < 1 || spt > 1000)
                    throw new ArgumentException("Loài " + sp.id + ": tốc độ mỗi nhịp phải trong [1, 1000].");
                if (sp.sizeX < 1 || sp.sizeX > 4 || sp.sizeZ < 1 || sp.sizeZ > 4 || sp.sizeY < 1 || sp.sizeY > 4)
                    throw new ArgumentException("Loài " + sp.id + ": khung phải trong [1, 4] mỗi chiều.");
            }

            if (crop == null || crop.growTimeMs <= 0 || crop.growTimeMs % msPerTick != 0)
                throw new ArgumentException("crop.growTimeMs phải > 0 và chia hết cho " + msPerTick + ".");
            if (storage == null || storage.capacity < 1)
                throw new ArgumentException("storage.capacity phải >= 1.");
            if (storage.startSeeds < 0 || storage.startSeeds > storage.capacity)
                throw new ArgumentException("storage.startSeeds phải trong [0, capacity].");
            if (farm == null || farm.plotQueueCapacity < 1 || farm.plotQueueCapacity > 8)
                throw new ArgumentException("farm.plotQueueCapacity phải trong [1, 8].");
        }
    }
}
