using System;
using UnityEngine;

namespace Game.Client
{
    public enum WalkToActState { Idle, Walking, Done, Unreachable, Cancelled }

    /// <summary>Đi thẳng tới khi ô trong tầm thực hiện (2 ô; đào: tầm đào) rồi làm (0034 mục 3, 7l c1). Không tìm đường; kẹt / tới đích mà chưa trong tầm → Unreachable.</summary>
    public sealed class WalkToAct
    {
        public WalkToActState State { get; private set; }
        public Vector3Int Cell { get; private set; }
        public bool DigReach { get; private set; }
        Action act;

        public static bool InReach(PlayerCharacter p, Vector3Int cell, bool digReach) => digReach ? p.InDigReach(cell) : p.InActReach(cell);

        /// <summary>player null / đang bay / đã trong tầm → act() ngay, Done. Không thì MoveTo(tâm ô), Walking (thay việc đang đi nếu có).</summary>
        public void Begin(PlayerCharacter player, Vector3Int cell, bool digReach, Action act)
        {
            Cell = cell;
            DigReach = digReach;
            this.act = null;
            if (player == null || player.FlyMode || InReach(player, cell, digReach))
            {
                State = WalkToActState.Done;
                act?.Invoke();
                return;
            }
            this.act = act;
            player.MoveTo(new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f));
            State = WalkToActState.Walking;
        }

        /// <summary>Walking → Cancelled; stop = dừng nhân vật đang đi. Trạng thái khác: không đổi.</summary>
        public void Cancel(PlayerCharacter player, bool stop)
        {
            if (State != WalkToActState.Walking) return;
            State = WalkToActState.Cancelled;
            act = null;
            if (stop && player != null) player.StopClickMove();
        }

        /// <summary>Gọi mỗi frame. Trả true đúng frame chuyển sang Unreachable.</summary>
        public bool Tick(PlayerCharacter player)
        {
            if (State != WalkToActState.Walking) return false;
            if (player == null) { State = WalkToActState.Cancelled; act = null; return false; }
            if (InReach(player, Cell, DigReach))
            {
                player.StopClickMove();
                var a = act;
                act = null;
                State = WalkToActState.Done;
                a?.Invoke();
                return false;
            }
            if (player.ClickMove != ClickMoveState.Walking)
            {
                State = WalkToActState.Unreachable;
                act = null;
                return true;
            }
            return false;
        }
    }
}
