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

    /// <summary>Một loại cây (0025). Số tạm; game-balance đặt sau. startSeeds = hạt loại này mỗi kho góp vào kho chung.</summary>
    [Serializable]
    public class CropDef
    {
        public string id;
        public int growTimeMs;
        public int seedsPerHarvest;
        public int producePerHarvest;
        public int startSeeds;

        public CropDef() { }
        public CropDef(string id, int growTimeMs, int seedsPerHarvest, int producePerHarvest, int startSeeds)
        {
            this.id = id;
            this.growTimeMs = growTimeMs;
            this.seedsPerHarvest = seedsPerHarvest;
            this.producePerHarvest = producePerHarvest;
            this.startSeeds = startSeeds;
        }
    }

    [Serializable]
    public class StorageConfig
    {
        public int capacity = 50;
    }

    [Serializable]
    public class FarmConfig
    {
        /// <summary>Số ô đợi gốc của ô ruộng cấp 1 (0026/0027; nâng cấp cộng sau — ideas).</summary>
        public int queueSlots = 2;
        /// <summary>Số vụ tối đa gốc của một ô đợi.</summary>
        public int slotMaxCount = 3;
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

    /// <summary>Số của người chơi (0028, số tạm). rainCooldownMs = thời gian hồi phép Mưa.</summary>
    [Serializable]
    public class PlayerConfig
    {
        public int rainCooldownMs = 10000;
        public float toolReach = 8f;
        public float handReach = 8f;
        public int starterCrop = 0;
        public int starterSeeds = 4;
    }

    /// <summary>
    /// Cấu hình mô phỏng M1. Nguồn chuẩn: design/data/sim_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Số là số tạm (user 2026-10-14), chỉnh khi chơi thử.
    /// </summary>
    [Serializable]
    public class SimConfig
    {
        public int configVersion = 5;
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
        public CropDef[] crops =
        {
            new CropDef("A", 120000, 1, 1, 10),
            new CropDef("B", 180000, 2, 3, 10),
        };
        public StorageConfig storage = new StorageConfig();
        public FarmConfig farm = new FarmConfig();
        public HouseConfig house = new HouseConfig();
        public CreatureConfig creature = new CreatureConfig();
        public PlayerConfig player = new PlayerConfig();

        public static WorkAbility ParseAbility(string s)
        {
            if (!string.IsNullOrEmpty(s) && char.IsLetter(s[0])
                && Enum.TryParse(s, false, out WorkAbility a) && Enum.IsDefined(typeof(WorkAbility), a))
                return a;
            throw new ArgumentException("Invalid ability: " + s);
        }

        public int MsToTicks(int ms) => ms * ticksPerSecond / 1000;

        public int GrowTicks(int cropId) => MsToTicks(crops[cropId].growTimeMs);

        /// <summary>Thời gian người chơi tự xới (0028: bằng công đoạn xới ở tốc độ gốc 1000‰) — giây.</summary>
        public float PlayerTillSeconds => BaseTicks(WorkAbility.Till) / (float)ticksPerSecond;
        public float RainCooldownSeconds => player.rainCooldownMs / 1000f;

        public int StuckTicks => MsToTicks(creature.stuckTimeMs);

        public int BlockWaitTicks => MsToTicks(creature.blockWaitMs);

        public int PhaseTicks => MsToTicks(creature.phaseAfterMs);

        public int WanderMinTicks => MsToTicks(creature.wanderMinMs);

        public int WanderMaxTicks => MsToTicks(creature.wanderMaxMs);

        public int BaseTicks(WorkAbility a)
        {
            for (int i = 0; i < stageTimes.Length; i++)
                if (ParseAbility(stageTimes[i].ability) == a) return MsToTicks(stageTimes[i].baseTimeMs);
            throw new ArgumentException("No time for ability: " + a);
        }

        public SpeciesWork FindSpecies(string id)
        {
            for (int i = 0; i < species.Length; i++)
                if (string.Equals(species[i].id, id, StringComparison.Ordinal)) return species[i];
            throw new ArgumentException("No such species: " + id);
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
                throw new ArgumentException("ticksPerSecond must be > 0 and a divisor of 1000.");
            int msPerTick = 1000 / ticksPerSecond;
            if (regionSize < 16 || regionSize > 256)
                throw new ArgumentException("regionSize must be in [16, 256].");

            int abilityCount = Enum.GetValues(typeof(WorkAbility)).Length;
            if (stageTimes == null || stageTimes.Length != abilityCount)
                throw new ArgumentException("stageTimes must have exactly " + abilityCount + " elements (one per ability).");
            var seen = new bool[abilityCount];
            for (int i = 0; i < stageTimes.Length; i++)
            {
                var a = ParseAbility(stageTimes[i].ability);
                if (seen[(int)a]) throw new ArgumentException("stageTimes has duplicate ability: " + a);
                seen[(int)a] = true;
                if (stageTimes[i].baseTimeMs <= 0 || stageTimes[i].baseTimeMs % msPerTick != 0)
                    throw new ArgumentException("baseTimeMs must be > 0 and divisible by " + msPerTick + " (" + a + ").");
            }

            if (species == null || species.Length == 0)
                throw new ArgumentException("species must not be empty.");
            for (int i = 0; i < species.Length; i++)
            {
                var sp = species[i];
                if (string.IsNullOrEmpty(sp.id)) throw new ArgumentException("Species missing id.");
                for (int j = 0; j < i; j++)
                    if (string.Equals(species[j].id, sp.id, StringComparison.Ordinal))
                        throw new ArgumentException("Duplicate species id: " + sp.id);
                var a1 = ParseAbility(sp.ability1);
                var a2 = ParseAbility(sp.ability2);
                if (a1 == a2) throw new ArgumentException("Species " + sp.id + " has two identical abilities.");
                if (sp.mastery1 < 1 || sp.mastery2 < 1)
                    throw new ArgumentException("Species " + sp.id + ": mastery must be >= 1.");
                if (sp.moveSpeedMilliCellsPerSecond % ticksPerSecond != 0)
                    throw new ArgumentException("Species " + sp.id + ": speed must be divisible by ticksPerSecond.");
                int spt = SpeedPerTick(sp);
                if (spt < 1 || spt > 1000)
                    throw new ArgumentException("Species " + sp.id + ": speed per tick must be in [1, 1000].");
                if (sp.sizeX < 1 || sp.sizeX > 4 || sp.sizeZ < 1 || sp.sizeZ > 4 || sp.sizeY < 1 || sp.sizeY > 4)
                    throw new ArgumentException("Species " + sp.id + ": frame must be in [1, 4] per dimension.");
            }

            if (masteryLevels == null || masteryLevels.Length == 0)
                throw new ArgumentException("masteryLevels must not be empty.");
            if (masteryLevels[0].minMastery > 1)
                throw new ArgumentException("masteryLevels[0].minMastery must be <= 1.");
            for (int i = 0; i < masteryLevels.Length; i++)
            {
                var m = masteryLevels[i];
                if (m.speedPermille < 1 || m.speedPermille > 10000)
                    throw new ArgumentException("masteryLevels[" + i + "].speedPermille must be in [1, 10000].");
                if (i > 0 && m.minMastery <= masteryLevels[i - 1].minMastery)
                    throw new ArgumentException("masteryLevels: minMastery must be increasing (line " + i + ").");
                if (i > 0 && m.speedPermille < masteryLevels[i - 1].speedPermille)
                    throw new ArgumentException("masteryLevels: speedPermille must not decrease (line " + i + ").");
            }

            if (storage == null || storage.capacity < 1)
                throw new ArgumentException("storage.capacity must be >= 1.");
            if (crops == null || crops.Length < 1 || crops.Length > 8)
                throw new ArgumentException("crops must have 1–8 entries.");
            int seedSum = 0;
            for (int i = 0; i < crops.Length; i++)
            {
                var c = crops[i];
                if (c == null || string.IsNullOrEmpty(c.id))
                    throw new ArgumentException("crops[" + i + "].id must not be empty.");
                for (int j = 0; j < i; j++)
                    if (crops[j].id == c.id)
                        throw new ArgumentException("crops[" + i + "].id is duplicated: " + c.id + ".");
                if (c.growTimeMs <= 0 || c.growTimeMs % msPerTick != 0)
                    throw new ArgumentException("crops[" + i + "].growTimeMs must be > 0 and divisible by " + msPerTick + ".");
                if (c.seedsPerHarvest < 0 || c.producePerHarvest < 0 || c.startSeeds < 0)
                    throw new ArgumentException("crops[" + i + "]: seedsPerHarvest, producePerHarvest, startSeeds must be >= 0.");
                int load = c.seedsPerHarvest + c.producePerHarvest;
                if (load < 1 || load > storage.capacity)
                    throw new ArgumentException("crops[" + i + "]: seedsPerHarvest + producePerHarvest must be in [1, storage.capacity].");
                seedSum += c.startSeeds;
            }
            if (seedSum > storage.capacity)
                throw new ArgumentException("Sum of crops[].startSeeds must be <= storage.capacity.");
            if (player == null)
                throw new ArgumentException("player must not be missing.");
            if (!(player.toolReach >= 1f && player.toolReach <= 32f))
                throw new ArgumentException("player.toolReach must be in [1, 32].");
            if (!(player.handReach >= 1f && player.handReach <= 32f))
                throw new ArgumentException("player.handReach must be in [1, 32].");
            if (player.starterCrop < 0 || player.starterCrop >= crops.Length)
                throw new ArgumentException("player.starterCrop must be in [0, number of crops).");
            if (player.starterSeeds < 0 || player.starterSeeds > 99)
                throw new ArgumentException("player.starterSeeds must be in [0, 99].");
            if (farm == null || farm.queueSlots < 1 || farm.queueSlots > 8)
                throw new ArgumentException("farm.queueSlots must be in [1, 8].");
            if (farm.slotMaxCount < 1 || farm.slotMaxCount > 99)
                throw new ArgumentException("farm.slotMaxCount must be in [1, 99].");
            if (house == null || house.capacity < 1 || house.capacity > 16)
                throw new ArgumentException("house.capacity must be in [1, 16].");
            if (house.sizeX < 1 || house.sizeX > 4 || house.sizeZ < 1 || house.sizeZ > 4 || house.sizeY < 1 || house.sizeY > 4)
                throw new ArgumentException("house: size must be in [1, 4] per dimension.");
            if (creature == null || creature.stuckTimeMs <= 0 || creature.stuckTimeMs % msPerTick != 0)
                throw new ArgumentException("creature.stuckTimeMs must be > 0 and divisible by " + msPerTick + ".");
            if (creature.blockWaitMs <= 0 || creature.blockWaitMs % msPerTick != 0)
                throw new ArgumentException("creature.blockWaitMs must be > 0 and divisible by " + msPerTick + ".");
            if (creature.phaseAfterMs <= creature.blockWaitMs || creature.phaseAfterMs % msPerTick != 0)
                throw new ArgumentException("creature.phaseAfterMs must be > blockWaitMs and divisible by " + msPerTick + ".");
            if (creature.homeRadius < 1 || creature.homeRadius > 64)
                throw new ArgumentException("creature.homeRadius must be in [1, 64].");
            if (creature.wanderMinMs <= 0 || creature.wanderMinMs % msPerTick != 0)
                throw new ArgumentException("creature.wanderMinMs must be > 0 and divisible by " + msPerTick + ".");
            if (creature.wanderMaxMs < creature.wanderMinMs || creature.wanderMaxMs % msPerTick != 0)
                throw new ArgumentException("creature.wanderMaxMs must be >= wanderMinMs and divisible by " + msPerTick + ".");
            if (player == null || player.rainCooldownMs <= 0)
                throw new ArgumentException("player.rainCooldownMs must be > 0.");
        }
    }
}
