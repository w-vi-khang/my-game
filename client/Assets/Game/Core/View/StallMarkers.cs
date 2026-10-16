using System.Collections.Generic;
using Game.Core.Sim;

namespace Game.Core.View
{
    /// <summary>Dấu đình trệ đang hiện (sim bước 6): công đoạn → sự kiện StallStarted gần nhất; StallCleared → bỏ. Sự kiện khác bỏ qua.</summary>
    public sealed class StallMarkers
    {
        readonly Dictionary<int, SimEvent> active = new Dictionary<int, SimEvent>();

        public int Count => active.Count;
        public IEnumerable<SimEvent> Active => active.Values;
        public bool TryGet(int stageId, out SimEvent e) => active.TryGetValue(stageId, out e);

        public void ApplyEvents(IReadOnlyList<SimEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Kind == SimEventKind.StallStarted) active[e.StageId] = e;
                else if (e.Kind == SimEventKind.StallCleared) active.Remove(e.StageId);
            }
        }
    }
}
