using Game.Core.Sim;

namespace Game.Core.View
{
    /// <summary>Chữ trạng thái trên đầu sinh vật và lý do đình trệ (sim bước 6, tạm cho prototype).</summary>
    public static class CreatureStatus
    {
        public static string AbilityName(WorkAbility a) => a switch
        {
            WorkAbility.Till => "xới",
            WorkAbility.Sow => "gieo",
            WorkAbility.Water => "tưới",
            WorkAbility.Harvest => "thu hoạch",
            WorkAbility.Haul => "vận chuyển",
            _ => a.ToString(),
        };

        public static string ReasonText(StallReason r) => r switch
        {
            StallReason.NoPath => "không có đường",
            StallReason.NoSeeds => "hết hạt",
            StallReason.StorageFull => "kho đầy",
            StallReason.NoAbility => "thiếu năng lực",
            _ => r.ToString(),
        };

        /// <summary>"L1 · rảnh" | "L1 · đi" | "L1 · đi → xới" | "L1 · xới 45%" (phần trăm làm tròn xuống).</summary>
        public static string Text(SimState s, Creature c)
        {
            string head = c.SpeciesId + " · ";
            if (c.State == CreatureState.Working && c.StageId >= 0)
            {
                var st = s.Stages[c.StageId];
                return head + AbilityName(st.Ability) + " " + (st.ProgressPermille / 10) + "%";
            }
            if (c.State == CreatureState.Moving)
                return c.StageId >= 0 ? head + "đi → " + AbilityName(s.Stages[c.StageId].Ability) : head + "đi";
            return head + "rảnh";
        }
    }
}
