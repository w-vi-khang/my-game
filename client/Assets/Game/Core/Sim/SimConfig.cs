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

    /// <summary>Một dòng của bảng mức thuần thục (0018): thuần thục >= minMastery → tốc độ làm speedPermille (1000 = thời gian gốc).</summary>
    [Serializable]
    public class MasteryLevel
    {
        public int minMastery;
        public int speedPermille;

        public MasteryLevel() { }
        public MasteryLevel(int minMastery, int speedPermille)
        {
            this.minMastery = minMastery;
            this.speedPermille = speedPermille;
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

    [Serializable]
    public class HouseConfig
    {
        public int capacity = 2;
        public int sizeX = 2;
        public int sizeZ = 2;
        public int sizeY = 2;
    }

    [Serializable]
    public class CreatureConfig
    {
        public int stuckTimeMs = 30000;
        public int blockWaitMs = 1000;
        public int phaseAfterMs = 5000;
        public int homeRadius = 4;
        public int wanderMinMs = 5000;
        public int wanderMaxMs = 10000;
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
        /// <summary>Cạnh vùng mô phỏng (cột) quanh trang trại trên world nhiều map (world 7e, brief 0019).</summary>
        public int regionSize = 64;
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
        public MasteryLevel[] masteryLevels =
        {
            new MasteryLevel(0, 500),
            new MasteryLevel(50, 750),
            new MasteryLevel(100, 1000),
            new MasteryLevel(200, 1250),
            new MasteryLevel(400, 1500),
        };
        public CropConfig crop = new CropConfig();
        public StorageConfig storage = new StorageConfig();
        public FarmConfig farm = new FarmConfig();
        public HouseConfig house = new HouseConfig();
        public CreatureConfig creature = new CreatureConfig();

        public static WorkAbility ParseAbility(string s)
        {
            if (!string.IsNullOrEmpty(s) && char.IsLetter(s[0])
                && Enum.TryParse(s, false, out WorkAbility a) && Enum.IsDefined(typeof(WorkAbility), a))
                return a;
            throw new ArgumentException("Năng lực không hợp lệ: " + s);
        }

        public int MsToTicks(int ms) => ms * ticksPerSecond / 1000;

        public int GrowTicks => MsToTicks(crop.growTimeMs);

        public int StuckTicks => MsToTicks(creature.stuckTimeMs);

        public int BlockWaitTicks => MsToTicks(creature.blockWaitMs);

        public int PhaseTicks => MsToTicks(creature.phaseAfterMs);

        public int WanderMinTicks => MsToTicks(creature.wanderMinMs);

        public int WanderMaxTicks => MsToTicks(creature.wanderMaxMs);

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

        /// <summary>Tốc độ làm (phần nghìn mỗi nhịp) theo bảng mức thuần thục: dòng có minMastery lớn nhất <= mastery (0018).</summary>
        public int WorkSpeedPermille(int mastery)
        {
            int v = masteryLevels[0].speedPermille;
            for (int i = 1; i < masteryLevels.Length; i++)
            {
                if (masteryLevels[i].minMastery > mastery) break;
                v = masteryLevels[i].speedPermille;
            }
            return v;
        }

        public void Validate()
        {
            if (ticksPerSecond <= 0 || 1000 % ticksPerSecond != 0)
                throw new ArgumentException("ticksPerSecond phải > 0 và là ước của 1000.");
            int msPerTick = 1000 / ticksPerSecond;
            if (regionSize < 16 || regionSize > 256)
                throw new ArgumentException("regionSize phải trong [16, 256].");

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

            if (masteryLevels == null || masteryLevels.Length == 0)
                throw new ArgumentException("masteryLevels không được rỗng.");
            if (masteryLevels[0].minMastery > 1)
                throw new ArgumentException("masteryLevels[0].minMastery phải <= 1.");
            for (int i = 0; i < masteryLevels.Length; i++)
            {
                var m = masteryLevels[i];
                if (m.speedPermille < 1 || m.speedPermille > 10000)
                    throw new ArgumentException("masteryLevels[" + i + "].speedPermille phải trong [1, 10000].");
                if (i > 0 && m.minMastery <= masteryLevels[i - 1].minMastery)
                    throw new ArgumentException("masteryLevels: minMastery phải tăng dần (dòng " + i + ").");
                if (i > 0 && m.speedPermille < masteryLevels[i - 1].speedPermille)
                    throw new ArgumentException("masteryLevels: speedPermille không được giảm (dòng " + i + ").");
            }

            if (crop == null || crop.growTimeMs <= 0 || crop.growTimeMs % msPerTick != 0)
                throw new ArgumentException("crop.growTimeMs phải > 0 và chia hết cho " + msPerTick + ".");
            if (storage == null || storage.capacity < 1)
                throw new ArgumentException("storage.capacity phải >= 1.");
            if (storage.startSeeds < 0 || storage.startSeeds > storage.capacity)
                throw new ArgumentException("storage.startSeeds phải trong [0, capacity].");
            if (farm == null || farm.plotQueueCapacity < 1 || farm.plotQueueCapacity > 8)
                throw new ArgumentException("farm.plotQueueCapacity phải trong [1, 8].");
            if (house == null || house.capacity < 1 || house.capacity > 16)
                throw new ArgumentException("house.capacity phải trong [1, 16].");
            if (house.sizeX < 1 || house.sizeX > 4 || house.sizeZ < 1 || house.sizeZ > 4 || house.sizeY < 1 || house.sizeY > 4)
                throw new ArgumentException("house: kích thước phải trong [1, 4] mỗi chiều.");
            if (creature == null || creature.stuckTimeMs <= 0 || creature.stuckTimeMs % msPerTick != 0)
                throw new ArgumentException("creature.stuckTimeMs phải > 0 và chia hết cho " + msPerTick + ".");
            if (creature.blockWaitMs <= 0 || creature.blockWaitMs % msPerTick != 0)
                throw new ArgumentException("creature.blockWaitMs phải > 0 và chia hết cho " + msPerTick + ".");
            if (creature.phaseAfterMs <= creature.blockWaitMs || creature.phaseAfterMs % msPerTick != 0)
                throw new ArgumentException("creature.phaseAfterMs phải > blockWaitMs và chia hết cho " + msPerTick + ".");
            if (creature.homeRadius < 1 || creature.homeRadius > 64)
                throw new ArgumentException("creature.homeRadius phải trong [1, 64].");
            if (creature.wanderMinMs <= 0 || creature.wanderMinMs % msPerTick != 0)
                throw new ArgumentException("creature.wanderMinMs phải > 0 và chia hết cho " + msPerTick + ".");
            if (creature.wanderMaxMs < creature.wanderMinMs || creature.wanderMaxMs % msPerTick != 0)
                throw new ArgumentException("creature.wanderMaxMs phải >= wanderMinMs và chia hết cho " + msPerTick + ".");
        }
    }
}
