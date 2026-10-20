using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class ChunkStreamPlanTests
    {
        static List<int> Expected(int px, int pz, int radius)
        {
            var list = new List<(long d, int c)>();
            for (int c = 0; c < 576; c++)
            {
                long d = ChunkStreamPlan.DistanceSq(px, pz, c % 24, c / 24);
                if (d <= (long)radius * radius) list.Add((d, c));
            }
            list.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.c.CompareTo(b.c));
            var r = new List<int>();
            foreach (var e in list) r.Add(e.c);
            return r;
        }

        [Test]
        public void DistanceSqIsZeroInsideAndToNearestCell()
        {
            Assert.AreEqual(0L, ChunkStreamPlan.DistanceSq(5, 5, 0, 0));
            Assert.AreEqual(25L, ChunkStreamPlan.DistanceSq(-5, 3, 0, 0));
            Assert.AreEqual(25L, ChunkStreamPlan.DistanceSq(20, 0, 0, 0));
            Assert.AreEqual(162L, ChunkStreamPlan.DistanceSq(40, 40, 1, 1));
        }

        [Test]
        public void UnlimitedLoadTakesExactlyColumnsWithinRadius()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160);
            var load = new List<int>(); var unload = new List<int>();
            plan.Update(192, 192, 0, load, unload);
            CollectionAssert.AreEqual(Expected(192, 192, 128), load);
            Assert.IsEmpty(unload);
            Assert.AreEqual(load.Count, plan.LoadedCount);
        }

        [Test]
        public void MaxLoadTakesNearestFirst()
        {
            var exp = Expected(192, 192, 128);
            var plan = new ChunkStreamPlan(24, 24, 128, 160);
            var load = new List<int>(); var unload = new List<int>();
            plan.Update(192, 192, 3, load, unload);
            CollectionAssert.AreEqual(exp.GetRange(0, 3), load);
            plan.Update(192, 192, 3, load, unload);
            CollectionAssert.AreEqual(exp.GetRange(3, 3), load);
        }

        [Test]
        public void HysteresisKeepsColumnsUntilUnloadRadius()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160);
            var load = new List<int>(); var unload = new List<int>();
            plan.Update(192, 192, 0, load, unload);
            plan.Update(212, 192, 0, load, unload);
            foreach (var c in unload)
                Assert.Greater(ChunkStreamPlan.DistanceSq(212, 192, c % 24, c / 24), 160L * 160);
            for (int c = 0; c < 576; c++)
            {
                long d = ChunkStreamPlan.DistanceSq(212, 192, c % 24, c / 24);
                if (d > 128L * 128 && d <= 160L * 160 && !load.Contains(c) && Expected(192, 192, 128).Contains(c))
                    Assert.IsTrue(plan.IsLoaded(c), $"cột {c}");
            }
        }

        [Test]
        public void MissingWithinCountsUnloaded()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160);
            Assert.AreEqual(Expected(192, 192, 120).Count, plan.MissingWithin(192, 192, 120));
            plan.Update(192, 192, 0, new List<int>(), new List<int>());
            Assert.AreEqual(0, plan.MissingWithin(192, 192, 120));
        }

        [Test]
        public void CtorRejectsBadRadii()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChunkStreamPlan(24, 24, 128, 100));
        }
    }
}
