using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class ControlStateTests
    {
        static RightClickTarget T(bool reach = true, bool held = false, bool mode = false, bool ripe = false, bool storage = false, bool drop = false) =>
            new RightClickTarget { InTargetReach = reach, HeldUsable = held, ModeUsable = mode, RipeCrop = ripe, Storage = storage, Drop = drop };

        [Test]
        public void ToggleAndHold()
        {
            var s = new ControlState();
            int changed = 0;
            s.Changed += () => changed++;
            Assert.AreEqual(ControlMode.Default, s.Mode);
            s.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(ControlMode.Dig, s.Mode);
            s.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(ControlMode.Default, s.Mode);
            s.ToggleMode(ControlMode.Till);
            s.Hold(ItemId.SeedBag(0));
            Assert.AreEqual(ControlMode.Default, s.Mode);
            Assert.IsTrue(s.HasHeld);
            s.ToggleMode(ControlMode.Rain);
            Assert.AreEqual(ControlMode.Rain, s.Mode);
            Assert.IsFalse(s.HasHeld);
            s.ClearToDefault();
            Assert.AreEqual(ControlMode.Default, s.Mode);
            Assert.IsFalse(s.HasHeld);
            int c = changed;
            s.ToggleMode(ControlMode.Default);
            Assert.AreEqual(c, changed);
        }

        [Test]
        public void RightClickPriority()
        {
            var seed = new ControlState(); seed.Hold(ItemId.SeedBag(0));
            var block = new ControlState(); block.Hold(ItemId.Block(BlockMaterial.Dirt));
            var dig = new ControlState(); dig.ToggleMode(ControlMode.Dig);
            var rain = new ControlState(); rain.ToggleMode(ControlMode.Rain);
            var till = new ControlState(); till.ToggleMode(ControlMode.Till);
            var def = new ControlState();

            Assert.AreEqual(RightClickAction.OutOfReach, RightClick.Resolve(seed, T(reach: false, held: true, mode: true, ripe: true, storage: true, drop: true)));
            Assert.AreEqual(RightClickAction.UseHeld, RightClick.Resolve(seed, T(held: true, ripe: true)));
            Assert.AreEqual(RightClickAction.Harvest, RightClick.Resolve(seed, T(ripe: true)));
            Assert.AreEqual(RightClickAction.Harvest, RightClick.Resolve(block, T(held: true, ripe: true)));
            Assert.AreEqual(RightClickAction.PlaceBlock, RightClick.Resolve(block, T(held: true)));
            Assert.AreEqual(RightClickAction.Dig, RightClick.Resolve(dig, T(mode: true, ripe: true)));
            Assert.AreEqual(RightClickAction.Harvest, RightClick.Resolve(rain, T(ripe: true)));
            Assert.AreEqual(RightClickAction.OpenStorage, RightClick.Resolve(def, T(storage: true, drop: true)));
            Assert.AreEqual(RightClickAction.PickDrop, RightClick.Resolve(def, T(drop: true)));
            Assert.AreEqual(RightClickAction.Till, RightClick.Resolve(till, T(mode: true)));
            Assert.AreEqual(RightClickAction.None, RightClick.Resolve(def, T()));
            Assert.Throws<ArgumentNullException>(() => RightClick.Resolve(null, T()));
        }
    }
}
