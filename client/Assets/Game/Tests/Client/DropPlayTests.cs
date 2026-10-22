using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim 7e (brief 0037): vật rơi — túi hạt đầu game, F (Thu), click nhặt, hiển thị gộp.</summary>
    public class DropPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        DemoPlain dp;
        PlayerCharacter player;
        FollowCamera follow;
        TerrainEditInput input;
        DropView dv;
        StressPlain plain;
        Vector3Int A;
        int G;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            follow = camGo.AddComponent<FollowCamera>();
            var uw = camGo.AddComponent<UnderwaterView>();
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            dp = go.AddComponent<DemoPlain>();
            input = go.AddComponent<TerrainEditInput>();
            dv = input.DropView;
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            uw.terrain = view;
            yield return null;
            yield return null;
            for (int i = 0; i < 60 && !player.Ready; i++)
                yield return null;
            Assert.IsTrue(dp.Plain.HasValue);
            plain = dp.Plain.Value;
            G = plain.Ground;
            A = new Vector3Int(plain.X0 + 6, plain.Ground, plain.Z0 + 6);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        IEnumerator Run(Vector2 move, bool jump, float seconds)
        {
            player.MoveInput = move;
            player.JumpInput = jump;
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                t += Time.deltaTime;
            }
            player.MoveInput = Vector2.zero;
            player.JumpInput = false;
        }

        IEnumerator Teleport(Vector3 feetLocal)
        {
            player.Place(view.transform.TransformPoint(feetLocal), 0f);
            for (int i = 0; i < 300 && !player.Ready; i++)
                yield return null;
            if (!player.Ready)
                Assert.Fail($"Chunk chưa sẵn sàng tại {feetLocal}");
            yield return Run(Vector2.zero, false, 0.3f);
        }

        static Vector3Int C(int x, int y, int z) => new Vector3Int(x, y, z);

        static ItemId B(BlockMaterial m) => ItemId.Block(m);
        Vector3 Local(float x, float y, float z) => new Vector3(x, y, z);

        [UnityTest]
        public IEnumerator StarterBagNearSpawnAndHud()
        {
            Assert.IsNotNull(player.StarterStack);
            Assert.AreEqual(ItemId.SeedBag(0), player.StarterStack.Item);
            Assert.AreEqual(4, player.StarterStack.Count);
            CollectionAssert.Contains(view.Editor.Drops.Stacks, player.StarterStack);
            StringAssert.DoesNotContain("Túi hạt Cây A", input.HudText());
            Assert.GreaterOrEqual(input.CollectNearby(), 1);
            Assert.AreEqual(4, view.Bag.GetSeeds(0));
            StringAssert.Contains("Túi hạt Cây A: 4", input.HudText());
            yield return null;
        }

        [UnityTest]
        public IEnumerator CollectNearbyTakesOnlyWithin8()
        {
            yield return Teleport(Local(A.x + 0.5f, G + 1, A.z + 0.5f));
            input.CollectNearby();
            yield return Run(Vector2.zero, false, 0.3f);
            int dirt0 = view.Bag.Get(BlockMaterial.Dirt), stone0 = view.Bag.Get(BlockMaterial.Stone);
            var near = view.Editor.Drops.Add(A.x + 2, G + 1, A.z, B(BlockMaterial.Dirt), 2);
            var far = view.Editor.Drops.Add(A.x + 9, G + 1, A.z, B(BlockMaterial.Stone), 1);
            Assert.AreEqual(1, input.CollectNearby());
            Assert.AreEqual(dirt0 + 2, view.Bag.Get(BlockMaterial.Dirt));
            Assert.AreEqual(stone0, view.Bag.Get(BlockMaterial.Stone));
            Assert.IsNotNull(view.Editor.Drops.Find(far.Id));
            Assert.IsNull(view.Editor.Drops.Find(near.Id));
            Assert.AreEqual(1, dv.FlyingCount);
            yield return Run(Vector2.zero, false, 0.3f);
            Assert.AreEqual(0, dv.FlyingCount);
        }

        [UnityTest]
        public IEnumerator ClickPicksOnlyInReachAndFarDoesNotBlockDig()
        {
            yield return Teleport(Local(A.x + 0.5f, G + 1, A.z + 0.5f));
            input.CollectNearby();
            yield return Run(Vector2.zero, false, 0.3f);
            int dirt0 = view.Bag.Get(BlockMaterial.Dirt);
            var near = view.Editor.Drops.Add(A.x + 2, G + 1, A.z, B(BlockMaterial.Dirt), 2);
            var far = view.Editor.Drops.Add(A.x + 9, G + 1, A.z, B(BlockMaterial.Stone), 1);
            var rayNear = new Ray(view.transform.TransformPoint(Local(A.x + 2.5f, G + 6f, A.z + 0.5f)), Vector3.down);
            Assert.AreEqual(near.Id, input.PickDrop(rayNear)?.Id);
            var rayFar = new Ray(view.transform.TransformPoint(Local(A.x + 9.5f, G + 6f, A.z + 0.5f)), Vector3.down);
            Assert.IsNull(input.PickDrop(rayFar));
            Assert.IsTrue(input.Pick(rayFar, out var dig, out _));
            Assert.AreEqual(C(A.x + 9, G, A.z), dig);
            Assert.IsTrue(input.CollectDrop(near));
            Assert.AreEqual(dirt0 + 2, view.Bag.Get(BlockMaterial.Dirt));
            Assert.IsFalse(input.CollectDrop(far));
            Assert.IsNotNull(view.Editor.Drops.Find(far.Id));
            Assert.IsTrue(WorldEditor.IsSolidAt(view.World, A.x + 2, G, A.z));
        }

        [UnityTest]
        public IEnumerator DigDropsVisibleThenCollectAndPlace()
        {
            yield return Teleport(Local(A.x + 0.5f, G + 1, A.z + 0.5f));
            input.CollectNearby();
            yield return Run(Vector2.zero, false, 0.3f);
            var c = C(A.x + 2, G, A.z);
            var m = view.World.GetMaterial(c.x, c.y, c.z);
            int b0 = view.Bag.Get(m);
            Assert.AreEqual(EditResult.Ok, input.UseTool(c, c + Vector3Int.up));
            var at = view.Editor.Drops.At(c.x, c.y, c.z);
            Assert.AreEqual(1, at.Count);
            Assert.AreEqual(B(m), at[0].Item);
            Assert.AreEqual(1, at[0].Count);
            Assert.AreEqual(b0, view.Bag.Get(m));
            yield return null;
            Assert.AreEqual(view.Editor.Drops.Stacks.Count, dv.LastInstanceCount);
            Assert.AreEqual(1, input.CollectNearby());
            Assert.AreEqual(b0 + 1, view.Bag.Get(m));
            Assert.AreEqual(EditResult.Ok, view.Place(c, m));
            Assert.AreEqual(b0, view.Bag.Get(m));
        }

        [UnityTest]
        public IEnumerator DropViewDrawsGroupsWithInstancedMaterials()
        {
            yield return Teleport(Local(A.x + 0.5f, G + 1, A.z + 0.5f));
            var d = view.Editor.Drops.Add(A.x + 2, G + 1, A.z, B(BlockMaterial.Dirt), 3);
            var s = view.Editor.Drops.Add(A.x + 2, G + 1, A.z + 2, ItemId.SeedBag(0), 4);
            yield return null;
            Assert.AreEqual(view.Editor.Drops.Stacks.Count + dv.FlyingCount, dv.LastInstanceCount);
            var mat = dv.MaterialFor(B(BlockMaterial.Dirt));
            Assert.IsTrue(mat.enableInstancing);
            var got = mat.GetColor("_BaseColor");
            var want = DropView.ColorOf(B(BlockMaterial.Dirt));
            Assert.AreEqual(want.r, got.r, 1e-3f);
            Assert.AreEqual(want.g, got.g, 1e-3f);
            Assert.AreEqual(want.b, got.b, 1e-3f);
            Assert.AreEqual(want.a, got.a, 1e-3f);
            var lc = DropView.LocalCenter(d);
            Assert.AreEqual(A.x + 2.5f, lc.x, 1e-4f);
            Assert.AreEqual(G + 1.15f, lc.y, 1e-4f);
            Assert.AreEqual(A.z + 0.5f, lc.z, 1e-4f);
            Assert.AreEqual(G + 1.2f, DropView.LocalCenter(s).y, 1e-4f);
            Assert.AreEqual("Đất × 3", TerrainEditInput.DropLabel(d));
            Assert.AreEqual("Túi hạt Cây A", TerrainEditInput.ItemLabel(ItemId.SeedBag(0)));
        }
    }
}
