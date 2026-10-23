using System;

namespace Game.Core
{
    public enum ControlMode : byte { Default = 0, Dig = 1, Till = 2, Rain = 3 }

    /// <summary>Chế độ / vật cầm trên PC (0034 mục 3–4): loại trừ nhau.</summary>
    public sealed class ControlState
    {
        public ControlMode Mode { get; private set; }
        public bool HasHeld { get; private set; }
        /// <summary>Chỉ có nghĩa khi HasHeld.</summary>
        public ItemId Held { get; private set; }
        /// <summary>Chỉ phát khi Mode / HasHeld / Held thật sự đổi.</summary>
        public event Action Changed;

        /// <summary>m == Default hoặc m == Mode → Default; khác → Mode = m. Luôn bỏ vật cầm.</summary>
        public void ToggleMode(ControlMode m) => Set(m == ControlMode.Default || m == Mode ? ControlMode.Default : m, false, default);

        public void Hold(ItemId item) => Set(ControlMode.Default, true, item);

        public void ClearToDefault() => Set(ControlMode.Default, false, default);

        void Set(ControlMode mode, bool hasHeld, ItemId held)
        {
            if (!hasHeld) held = default;
            bool changed = mode != Mode || hasHeld != HasHeld || (hasHeld && held != Held);
            Mode = mode;
            HasHeld = hasHeld;
            Held = held;
            if (changed) Changed?.Invoke();
        }
    }

    public enum RightClickAction : byte { None = 0, OutOfReach, UseHeld, Dig, Till, Rain, Harvest, OpenStorage, PickDrop, PlaceBlock }

    /// <summary>Dữ kiện về chỗ chuột phải; client tính (luật của từng vật / chế độ nằm ở nơi khác).</summary>
    public struct RightClickTarget
    {
        public bool InTargetReach;  // ô đích trong tầm thao tác (PlayerRules.InTargetReach)
        public bool HeldUsable;     // vật đang cầm dùng được ở chỗ này
        public bool ModeUsable;     // hành động của chế độ đang bật có tác dụng ở chỗ này
        public bool RipeCrop;       // ô ruộng có cây chín
        public bool Storage;        // khối kho
        public bool Drop;           // có vật rơi
    }

    public static class RightClick
    {
        /// <summary>Thứ tự ưu tiên chuột phải (0034 mục 3). Không kiểm mana; client kiểm sau.</summary>
        public static RightClickAction Resolve(ControlState state, in RightClickTarget t)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!t.InTargetReach) return RightClickAction.OutOfReach;
            if (state.HasHeld && state.Held.Kind != ItemKind.Block && t.HeldUsable) return RightClickAction.UseHeld;
            if (state.Mode != ControlMode.Default && t.ModeUsable)
            {
                switch (state.Mode)
                {
                    case ControlMode.Dig: return RightClickAction.Dig;
                    case ControlMode.Till: return RightClickAction.Till;
                    default: return RightClickAction.Rain;
                }
            }
            if (t.RipeCrop) return RightClickAction.Harvest;
            if (t.Storage) return RightClickAction.OpenStorage;
            if (t.Drop) return RightClickAction.PickDrop;
            if (state.HasHeld && state.Held.Kind == ItemKind.Block && t.HeldUsable) return RightClickAction.PlaceBlock;
            return RightClickAction.None;
        }
    }
}
