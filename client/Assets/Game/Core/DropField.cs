using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum ItemKind : byte { Block = 0, SeedBag = 1 }

    /// <summary>Loại vật phẩm: khối (Value = BlockMaterial) hoặc túi hạt (Value = chỉ số loại cây).</summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public readonly ItemKind Kind;
        public readonly byte Value;
        public ItemId(ItemKind kind, byte value) { Kind = kind; Value = value; }
        public static ItemId Block(BlockMaterial m) => new ItemId(ItemKind.Block, (byte)m);
        public static ItemId SeedBag(int crop)
        {
            if (crop < 0 || crop > 255)
                throw new ArgumentOutOfRangeException(nameof(crop), crop, "Loại cây phải trong 0..255");
            return new ItemId(ItemKind.SeedBag, (byte)crop);
        }
        public BlockMaterial Material => (BlockMaterial)Value;
        public int Crop => Value;
        public bool Equals(ItemId other) => Kind == other.Kind && Value == other.Value;
        public override bool Equals(object obj) => obj is ItemId o && Equals(o);
        public override int GetHashCode() => ((int)Kind << 8) | Value;
        public static bool operator ==(ItemId a, ItemId b) => a.Equals(b);
        public static bool operator !=(ItemId a, ItemId b) => !a.Equals(b);
        public override string ToString() => Kind == ItemKind.Block ? "Block:" + Material : "SeedBag:" + Crop;
    }

    /// <summary>Một chồng vật rơi: một loại vật phẩm, số lượng ≥ 1, nằm ở ô (X, Y, Z); ô bên dưới luôn đặc.</summary>
    public sealed class DropStack
    {
        public int Id { get; }
        public ItemId Item { get; }
        public int Count { get; internal set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Z { get; internal set; }
        internal DropStack(int id, ItemId item, int count, int x, int y, int z)
        {
            Id = id; Item = item; Count = count; X = x; Y = y; Z = z;
        }
    }

    /// <summary>
    /// Vật rơi theo ô (0028, task contract 7e): gộp chồng cùng loại trong ô, rơi khi mất khối bên dưới, đẩy lên khi ô bị đặt khối,
    /// nhặt trong tầm việc tay. Tất định: thứ tự theo Stacks.
    /// </summary>
    public sealed class DropField
    {
        readonly WorldData world;
        readonly List<DropStack> stacks = new List<DropStack>();
        readonly Dictionary<(int, int, int), List<DropStack>> byCell = new Dictionary<(int, int, int), List<DropStack>>();
        int nextId = 1;

        public DropField(WorldData world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public IReadOnlyList<DropStack> Stacks => stacks;

        /// <summary>Tăng 1 mỗi lần gọi công khai làm đổi chồng (thêm, gộp, rơi, đẩy, nhặt).</summary>
        public int Version { get; private set; }

        /// <summary>Phát cho mỗi chồng được nhặt, sau khi cộng vào túi, trước khi gỡ khỏi Stacks.</summary>
        public event Action<DropStack> Collected;

        public IReadOnlyList<DropStack> At(int x, int y, int z) =>
            byCell.TryGetValue((x, y, z), out var list) ? list : (IReadOnlyList<DropStack>)Array.Empty<DropStack>();

        public DropStack Find(int id)
        {
            foreach (var s in stacks)
                if (s.Id == id)
                    return s;
            return null;
        }

        public DropStack Add(int x, int y, int z, ItemId item, int count)
        {
            if (!world.Size.InBounds(x, y, z))
                throw new ArgumentOutOfRangeException(nameof(y), "Ô ngoài map");
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Số lượng phải ≥ 1");
            if (WorldEditor.IsSolidAt(world, x, y, z))
                throw new ArgumentException("Ô đã đặc");
            y = Settle(x, y, z);
            var existing = FindSame(x, y, z, item);
            DropStack result;
            if (existing != null)
            {
                existing.Count += count;
                result = existing;
            }
            else
            {
                result = new DropStack(nextId++, item, count, x, y, z);
                stacks.Add(result);
                CellList(x, y, z).Add(result);
            }
            Version++;
            return result;
        }

        /// <summary>Gọi khi ô (x, y, z) vừa thành không đặc: chồng ở ô ngay trên rơi xuống.</summary>
        public void OnCellCleared(int x, int y, int z)
        {
            if (!world.Size.InBounds(x, y + 1, z) || !byCell.ContainsKey((x, y + 1, z)))
                return;
            bool changed = false;
            foreach (var s in stacks.ToArray())
            {
                if (s.X != x || s.Y != y + 1 || s.Z != z)
                    continue;
                Move(s, Settle(x, y + 1, z));
                changed = true;
            }
            if (changed)
                Version++;
        }

        public bool CanPushUp(int x, int y, int z)
        {
            if (!byCell.ContainsKey((x, y, z)))
                return true;
            return FindFreeAbove(x, y, z) >= 0;
        }

        /// <summary>Gọi khi ô (x, y, z) vừa thành đặc: chồng trong ô bị đẩy lên ô không đặc đầu tiên phía trên.</summary>
        public void PushUp(int x, int y, int z)
        {
            if (!byCell.ContainsKey((x, y, z)))
                return;
            int ty = FindFreeAbove(x, y, z);
            if (ty < 0)
                throw new InvalidOperationException("Không còn ô trống phía trên để đẩy vật rơi");
            bool changed = false;
            foreach (var s in stacks.ToArray())
            {
                if (s.X != x || s.Y != y || s.Z != z)
                    continue;
                Move(s, ty);
                changed = true;
            }
            if (changed)
                Version++;
        }

        public int CollectInReach(float feetX, float feetY, float feetZ, PlayerBag bag)
        {
            if (bag == null)
                throw new ArgumentNullException(nameof(bag));
            int n = 0;
            foreach (var s in stacks.ToArray())
            {
                if (!PlayerRules.InHandReach(feetX, feetY, feetZ, s.X, s.Y, s.Z))
                    continue;
                Take(s, bag);
                n++;
            }
            if (n > 0)
                Version++;
            return n;
        }

        public bool TryCollect(int stackId, float feetX, float feetY, float feetZ, PlayerBag bag)
        {
            if (bag == null)
                throw new ArgumentNullException(nameof(bag));
            var s = Find(stackId);
            if (s == null || !PlayerRules.InHandReach(feetX, feetY, feetZ, s.X, s.Y, s.Z))
                return false;
            Take(s, bag);
            Version++;
            return true;
        }

        void Take(DropStack s, PlayerBag bag)
        {
            bag.Add(s.Item, s.Count);
            Collected?.Invoke(s);
            Remove(s);
        }

        /// <summary>Hạ ô (x, y) xuống tới ô ngay trên khối đặc.</summary>
        int Settle(int x, int y, int z)
        {
            while (!WorldEditor.IsSolidAt(world, x, y - 1, z))
                y--;
            return y;
        }

        int FindFreeAbove(int x, int y, int z)
        {
            for (int ty = y + 1; ty < world.Size.Y; ty++)
                if (!WorldEditor.IsSolidAt(world, x, ty, z))
                    return ty;
            return -1;
        }

        DropStack FindSame(int x, int y, int z, ItemId item)
        {
            if (byCell.TryGetValue((x, y, z), out var list))
                foreach (var s in list)
                    if (s.Item == item)
                        return s;
            return null;
        }

        List<DropStack> CellList(int x, int y, int z)
        {
            if (!byCell.TryGetValue((x, y, z), out var list))
                byCell[(x, y, z)] = list = new List<DropStack>();
            return list;
        }

        void RemoveFromCell(DropStack s)
        {
            var key = (s.X, s.Y, s.Z);
            var list = byCell[key];
            list.Remove(s);
            if (list.Count == 0)
                byCell.Remove(key);
        }

        void Remove(DropStack s)
        {
            RemoveFromCell(s);
            stacks.Remove(s);
        }

        /// <summary>Chuyển chồng s tới ô cùng (X, Z), cao độ ty: gộp nếu ô đích có chồng cùng loại (chồng có sẵn giữ id).</summary>
        void Move(DropStack s, int ty)
        {
            var target = FindSame(s.X, ty, s.Z, s.Item);
            if (target != null)
            {
                target.Count += s.Count;
                Remove(s);
                return;
            }
            RemoveFromCell(s);
            s.Y = ty;
            CellList(s.X, ty, s.Z).Add(s);
        }
    }
}
