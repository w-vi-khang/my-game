using Game.Core.Sim;
using Game.Core.Text;

namespace Game.Core.View
{
    /// <summary>Chữ trạng thái trên đầu sinh vật và lý do đình trệ (sim bước 6, tạm cho prototype).</summary>
    public static class CreatureStatus
    {
        public static string AbilityName(Strings t, WorkAbility a) => a switch
        {
            WorkAbility.Till => t.Get("creature.ability_till"),
            WorkAbility.Sow => t.Get("creature.ability_sow"),
            WorkAbility.Water => t.Get("creature.ability_water"),
            WorkAbility.Harvest => t.Get("creature.ability_harvest"),
            WorkAbility.Haul => t.Get("creature.ability_haul"),
            _ => a.ToString(),
        };

        public static string ReasonText(Strings t, StallReason r) => r switch
        {
            StallReason.NoPath => t.Get("creature.reason_no_path"),
            StallReason.NoSeeds => t.Get("creature.reason_no_seeds"),
            StallReason.StorageFull => t.Get("creature.reason_storage_full"),
            StallReason.NoAbility => t.Get("creature.reason_no_ability"),
            _ => r.ToString(),
        };

        /// <summary>"L1 · rảnh" | "L1 · đi" | "L1 · đi → xới" | "L1 · xới 45%" (phần trăm làm tròn xuống).</summary>
        public static string Text(Strings t, SimState s, Creature c)
        {
            string head = t.Format("creature.head", c.SpeciesId);
            if (c.State == CreatureState.Working && c.StageId >= 0)
            {
                var st = s.Stages[c.StageId];
                return head + t.Format("creature.working", AbilityName(t, st.Ability), st.ProgressPermille / 10);
            }
            if (c.State == CreatureState.Moving)
                return c.StageId >= 0 ? head + t.Format("creature.moving_to", AbilityName(t, s.Stages[c.StageId].Ability)) : head + t.Get("creature.moving");
            return head + t.Get("creature.idle");
        }
    }
}
