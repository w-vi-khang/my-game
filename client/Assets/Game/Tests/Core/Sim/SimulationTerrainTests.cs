using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimulationTerrainTests
    {
        static WorldData Trench(bool bridge)
        {
            var w = TestWorlds.Flat(2);
            for (int z = 0; z < 16; z++)
            {
                w.Set(5, 1, z, Cell.Make(BlockMaterial.Empty));
                w.Set(5, 2, z, Cell.Make(BlockMaterial.Empty));
            }
            if (bridge) w.Set(5, 2, 3, Cell.Make(BlockMaterial.Dirt));
            return w;
        }

        static Simulation Build(SimConfig cfg, WorldData w, out WorldEditor editor, out MaterialInventory inv)
        {
            var sim = new Simulation(cfg, w);
            inv = new MaterialInventory();
            editor = new WorldEditor(w, inv, 1);
            editor.CellChanged += sim.OnCellChanged;
            editor.IsOccupied = sim.IsOccupied;
            return sim;
        }

        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        static void Run(Simulation sim, int ticks)
        {
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        [Test]
        public void DigCutsPathThenPlaceRestores()
        {
            var sim = Build(new SimConfig(), Trench(true), out var editor, out _);
            int c = sim.AddCreature("L1", new CellPos(1, 3, 3));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(9, 3, 3));
            Run(sim, 10);
            Assert.AreEqual(new CellPos(2, 3, 3), sim.State.Creatures[c].Cell);
            Assert.AreEqual(CreatureState.Moving, sim.State.Creatures[c].State);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 2, 3));
            sim.Tick();
            Assert.AreEqual(StageStatus.Blocked, sim.State.Stages[s].Status);
            Assert.AreEqual(StallReason.NoPath, sim.State.Stages[s].Stall);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
            Assert.AreEqual(-1, sim.State.Creatures[c].StageId);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(5, 2, 3, BlockMaterial.Dirt));
            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, sim.State.Stages[s].Status);
            Assert.AreEqual(CreatureState.Moving, sim.State.Creatures[c].State);
        }

        [Test]
        public void NoPathAtAssignBlocks()
        {
            var sim = Build(new SimConfig(), Trench(false), out _, out _);
            int c = sim.AddCreature("L1", new CellPos(1, 3, 3));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(9, 3, 3));
            sim.Tick();
            Assert.AreEqual(StageStatus.Blocked, sim.State.Stages[s].Status);
            Assert.AreEqual(StallReason.NoPath, sim.State.Stages[s].Stall);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
        }

        [Test]
        public void TriesNextCandidateWhenNoPath()
        {
            var sim = Build(new SimConfig(), Trench(false), out _, out _);
            int a = sim.AddCreature("L1", new CellPos(4, 3, 3));
            int b = sim.AddCreature("L1", new CellPos(8, 3, 9));
            sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(8, 3, 3));
            sim.Tick();
            Assert.AreNotEqual(-1, sim.State.Creatures[b].StageId);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[a].State);
        }

        [Test]
        public void DigUnderCreatureStepsAside()
        {
            var sim = Build(new SimConfig(), TestWorlds.Flat(1), out var editor, out _);
            int c = sim.AddCreature("L1", new CellPos(5, 2, 5));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            Assert.AreEqual(new CellPos(6, 2, 5), sim.State.Creatures[c].Cell);
        }

        [Test]
        public void FallsWhenNoNeighbor()
        {
            var w = TestWorlds.Flat(0);
            w.Set(5, 1, 5, Cell.Make(BlockMaterial.Dirt));
            w.Set(5, 2, 5, Cell.Make(BlockMaterial.Dirt));
            w.Set(5, 3, 5, Cell.Make(BlockMaterial.Dirt));
            var sim = Build(new SimConfig(), w, out var editor, out _);
            int c = sim.AddCreature("L1", new CellPos(5, 4, 5));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 3, 5));
            sim.Tick();
            Assert.AreEqual(new CellPos(5, 3, 5), sim.State.Creatures[c].Cell);
        }

        [Test]
        public void FallsUntilGround()
        {
            var w = TestWorlds.Flat(0);
            w.Set(5, 1, 5, Cell.Make(BlockMaterial.Dirt));
            w.Set(5, 2, 5, Cell.Make(BlockMaterial.Dirt));
            w.Set(5, 3, 5, Cell.Make(BlockMaterial.Dirt));
            var sim = Build(new SimConfig(), w, out var editor, out _);
            int c = sim.AddCreature("L1", new CellPos(5, 4, 5));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 3, 5));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 2, 5));
            sim.Tick();
            Assert.AreEqual(new CellPos(5, 2, 5), sim.State.Creatures[c].Cell);
        }

        [Test]
        public void DisplacedWorkerKeepsProgress()
        {
            var sim = Build(new SimConfig(), TestWorlds.Flat(1), out var editor, out _);
            int c = sim.AddCreature("L1", new CellPos(5, 2, 5));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(5, 2, 5));
            Run(sim, 11);
            Assert.AreEqual(10000, sim.State.Stages[s].WorkDone);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            Assert.AreEqual(new CellPos(6, 2, 5), sim.State.Creatures[c].Cell);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
            Assert.AreEqual(StageStatus.Blocked, sim.State.Stages[s].Status);
            Assert.AreEqual(StallReason.NoPath, sim.State.Stages[s].Stall);
            Assert.AreEqual(10000, sim.State.Stages[s].WorkDone);
        }

        [Test]
        public void OccupiedCoversFrame()
        {
            var cfg = new SimConfig();
            var list = new List<SpeciesWork>(cfg.species) { new SpeciesWork("Tall", "Till", "Sow", 100, 100, 1500, 1, 1, 2) };
            cfg.species = list.ToArray();
            var sim = Build(cfg, TestWorlds.Flat(0), out var editor, out var inv);
            sim.AddCreature("Tall", new CellPos(3, 1, 3));
            Assert.IsTrue(sim.IsOccupied(3, 1, 3));
            Assert.IsTrue(sim.IsOccupied(3, 2, 3));
            Assert.IsFalse(sim.IsOccupied(3, 3, 3));
            inv.Add(BlockMaterial.Dirt);
            Assert.AreEqual(EditResult.Occupied, editor.TryPlace(3, 1, 3, BlockMaterial.Dirt));
        }

        [Test]
        public void AddCreatureRejectsFloating()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(0));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", new CellPos(3, 5, 3)));
        }
    }
}
