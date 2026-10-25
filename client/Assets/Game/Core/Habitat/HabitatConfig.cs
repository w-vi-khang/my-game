using System;
using Game.Core.DayCycle;

namespace Game.Core.Habitat
{
    /// <summary>Yêu cầu môi trường sống của một loài (sim 8). Mọi trường là dữ liệu — số là số tạm, chưa chốt.</summary>
    [Serializable]
    public class HabitatRequirement
    {
        public const string KindCells = "cells", KindCrops = "crops", KindStructures = "structures", KindSpecies = "species", KindPeriod = "period";
        public const string SolidMaterial = "Solid";

        /// <summary>"cells" | "crops" | "structures" | "species" | "period".</summary>
        public string kind;
        /// <summary>Ngưỡng: đạt khi số đếm &gt;= min (kind "period" bỏ qua min).</summary>
        public int min = 1;
        /// <summary>cells: tên BlockMaterial, hoặc "Solid" = mọi vật liệu khác Empty và Water.</summary>
        public string material = "";
        /// <summary>cells: ô ngay trên là Empty hoặc ngoài world.</summary>
        public bool exposed;
        /// <summary>cells: ít nhất 1 trong 8 ô (4 ô ngang cùng tầng y + 4 ô ngang tầng y+1) có vật liệu Water.</summary>
        public bool nearWater;
        /// <summary>cells: chỉ tính ô có y &gt;= minY.</summary>
        public int minY;
        /// <summary>crops: -1 = mọi loại cây.</summary>
        public int cropId = -1;
        /// <summary>structures: "storage" | "house".</summary>
        public string structure = "";
        /// <summary>species: id loài.</summary>
        public string species = "";
        /// <summary>period: như PeriodCondition.</summary>
        public string[] periods = new string[0];

        public void Validate()
        {
            switch (kind)
            {
                case KindCells:
                    CheckMin();
                    if (material != SolidMaterial && !IsMaterialName(material))
                        throw new ArgumentException("Invalid cells material: " + material);
                    break;
                case KindCrops:
                    CheckMin();
                    break;
                case KindStructures:
                    CheckMin();
                    if (structure != "storage" && structure != "house")
                        throw new ArgumentException("Invalid structure: " + structure);
                    break;
                case KindSpecies:
                    CheckMin();
                    if (string.IsNullOrEmpty(species))
                        throw new ArgumentException("species requirement needs a species id.");
                    break;
                case KindPeriod:
                    new PeriodCondition(periods).Validate();
                    break;
                default:
                    throw new ArgumentException("Invalid requirement kind: " + kind);
            }
        }

        void CheckMin()
        {
            if (min < 1) throw new ArgumentException("min must be >= 1.");
        }

        public static bool IsMaterialName(string s) =>
            !string.IsNullOrEmpty(s) && char.IsLetter(s[0])
            && Enum.TryParse(s, false, out BlockMaterial m) && Enum.IsDefined(typeof(BlockMaterial), m);
    }

    [Serializable]
    public class HabitatSpecies
    {
        public string id;
        public HabitatRequirement[] requirements;

        public HabitatSpecies() { }
        public HabitatSpecies(string id, params HabitatRequirement[] requirements)
        {
            this.id = id;
            this.requirements = requirements;
        }
    }

    /// <summary>
    /// Cấu hình môi trường sống M1. Nguồn chuẩn: design/data/habitat_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Số là số tạm, chưa chốt (task m1-sim-8-moi-truong-song).
    /// </summary>
    [Serializable]
    public class HabitatConfig
    {
        public int configVersion = 1;
        /// <summary>Vùng đếm: cột [regionX0, regionX1) × [regionZ0, regionZ1), đủ chiều cao. Cả 4 = -1 → cả world.</summary>
        public int regionX0 = -1, regionZ0 = -1, regionX1 = -1, regionZ1 = -1;
        public string[] unlockedAtStart = { "L1" };
        public HabitatSpecies[] species =
        {
            new HabitatSpecies("L1",
                new HabitatRequirement { kind = HabitatRequirement.KindCells, min = 20, material = "Grass" },
                new HabitatRequirement { kind = HabitatRequirement.KindCrops, min = 1 }),
            new HabitatSpecies("L2",
                new HabitatRequirement { kind = HabitatRequirement.KindCells, min = 10, material = "Grass", nearWater = true }),
            new HabitatSpecies("L3",
                new HabitatRequirement { kind = HabitatRequirement.KindCells, min = 15, material = "Stone", exposed = true },
                new HabitatRequirement { kind = HabitatRequirement.KindStructures, min = 1, structure = "storage" }),
        };

        /// <summary>Vùng là cả world (cả 4 giá trị = -1).</summary>
        public bool WholeWorld => regionX0 == -1 && regionZ0 == -1 && regionX1 == -1 && regionZ1 == -1;

        public void Validate()
        {
            if (configVersion != 1) throw new ArgumentException("Unsupported configVersion: " + configVersion);
            if (!WholeWorld && (regionX0 < 0 || regionZ0 < 0 || regionX0 >= regionX1 || regionZ0 >= regionZ1))
                throw new ArgumentException($"Invalid region: [{regionX0},{regionX1}) x [{regionZ0},{regionZ1})");
            if (species == null) throw new ArgumentException("species must not be null.");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var sp in species)
            {
                if (sp == null || string.IsNullOrEmpty(sp.id)) throw new ArgumentException("Species id must not be empty.");
                if (!ids.Add(sp.id)) throw new ArgumentException("Duplicate species id: " + sp.id);
                if (sp.requirements == null) throw new ArgumentException("requirements must not be null: " + sp.id);
                foreach (var r in sp.requirements)
                {
                    if (r == null) throw new ArgumentException("Null requirement in " + sp.id);
                    r.Validate();
                }
            }
            if (unlockedAtStart != null)
                foreach (var id in unlockedAtStart)
                    if (!ids.Contains(id)) throw new ArgumentException("unlockedAtStart has unknown species: " + id);
        }
    }
}
