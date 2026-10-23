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
                    Assert.IsTrue(plan.IsLoaded(c), $"column {c}");
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

        static List<int> ExpectedMulti(int[] xs, int[] zs, int radius)
        {
            var list = new List<(long d, int c)>();
            for (int c = 0; c < 576; c++)
            {
                long dMin = long.MaxValue;
                bool inside = false;
                for (int i = 0; i < xs.Length; i++)
                {
                    long d = ChunkStreamPlan.DistanceSq(xs[i], zs[i], c % 24, c / 24);
                    dMin = Math.Min(dMin, d);
                    if (d <= (long)radius * radius) inside = true;
                }
                if (inside) list.Add((dMin, c));
            }
            list.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.c.CompareTo(b.c));
            var r = new List<int>();
            foreach (var e in list) r.Add(e.c);
            return r;
        }

        [Test]
        public void TwoViewpointsLoadUnionOfRegions()
        {
            var a = Expected(192, 60, 128);
            var b = Expected(192, 330, 128);
            foreach (var c in a) CollectionAssert.DoesNotContain(b, c);
            var plan = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var load = new List<int>(); var unload = new List<int>();
            var vps = new[] { new StreamViewpoint(192, 60, 128, 160), new StreamViewpoint(192, 330, 128, 160) };
            plan.Update(vps, 0, load, unload);
            CollectionAssert.AreEqual(ExpectedMulti(new[] { 192, 192 }, new[] { 60, 330 }, 128), load);
            Assert.AreEqual(a.Count + b.Count, load.Count);
            Assert.AreEqual(load.Count, plan.LoadedCount);
            Assert.IsEmpty(unload);
        }

        [Test]
        public void ColumnInBothRegionsLoadsOnce()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var load = new List<int>(); var unload = new List<int>();
            var vps = new[] { new StreamViewpoint(192, 150, 128, 160), new StreamViewpoint(192, 200, 128, 160) };
            plan.Update(vps, 0, load, unload);
            CollectionAssert.AllItemsAreUnique(load);
            Assert.AreEqual(load.Count, plan.LoadedCount);
            plan.Update(vps, 0, load, unload);
            Assert.IsEmpty(load);
            Assert.IsEmpty(unload);
        }

        [Test]
        public void ViewpointLeavingUnloadsOnlyUnheldColumns()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var load = new List<int>(); var unload = new List<int>();
            var a = new StreamViewpoint(192, 60, 128, 160);
            plan.Update(new[] { a, new StreamViewpoint(192, 330, 128, 160) }, 0, load, unload);
            var held = new List<int>(load);
            plan.Update(new[] { a }, 0, load, unload);
            var expected = new List<int>();
            foreach (var c in held)
                if (ChunkStreamPlan.DistanceSq(192, 60, c % 24, c / 24) > 160L * 160) expected.Add(c);
            expected.Sort();
            CollectionAssert.AreEqual(expected, unload);
            foreach (var c in Expected(192, 60, 128)) CollectionAssert.DoesNotContain(unload, c);
            Assert.AreEqual(held.Count - expected.Count, plan.LoadedCount);
        }

        [Test]
        public void SingleViewpointMatchesLegacyUpdate()
        {
            var legacy = new ChunkStreamPlan(24, 24, 128, 160);
            var multi = new ChunkStreamPlan(24, 24, 128, 160);
            var l1 = new List<int>(); var u1 = new List<int>();
            var l2 = new List<int>(); var u2 = new List<int>();
            for (int f = 0; f < 6; f++)
            {
                int x = f < 3 ? 192 : 212;
                legacy.Update(x, 192, 3, l1, u1);
                multi.Update(new[] { new StreamViewpoint(x, 192, 128, 160) }, 3, l2, u2);
                CollectionAssert.AreEqual(l1, l2);
                CollectionAssert.AreEqual(u1, u2);
                Assert.AreEqual(legacy.LoadedCount, multi.LoadedCount);
            }
        }

        [Test]
        public void UpdateIsDeterministic()
        {
            var p1 = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var p2 = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var l1 = new List<int>(); var u1 = new List<int>();
            var l2 = new List<int>(); var u2 = new List<int>();
            for (int f = 0; f < 12; f++)
            {
                var vps = new[] { new StreamViewpoint(192, 60, 128, 160), new StreamViewpoint(192, 330 - f * 20, 128, 160) };
                p1.Update(vps, 5, l1, u1);
                p2.Update(vps, 5, l2, u2);
                CollectionAssert.AreEqual(l1, l2);
                CollectionAssert.AreEqual(u1, u2);
            }
        }

        [Test]
        public void TooManyViewpointsThrows()
        {
            var plan = new ChunkStreamPlan(24, 24, 128, 160, 2);
            var load = new List<int>(); var unload = new List<int>();
            var three = new[] { new StreamViewpoint(1, 1, 128, 160), new StreamViewpoint(2, 2, 128, 160), new StreamViewpoint(3, 3, 128, 160) };
            Assert.Throws<ArgumentException>(() => plan.Update(three, 0, load, unload));
            Assert.AreEqual(0, plan.LoadedCount);
            Assert.Throws<ArgumentException>(() => plan.Update(new StreamViewpoint[0], 0, load, unload));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChunkStreamPlan(24, 24, 128, 160, 0));
        }
    }
}
