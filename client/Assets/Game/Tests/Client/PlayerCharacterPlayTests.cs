using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7h (brief 0035): nhân vật người chơi PC — xuất hiện, đi, nhảy, va chạm, rơi, biên, nước, camera theo, tầm với, ô chiếm, F3, minimap.</summary>
    public class PlayerCharacterPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        DemoPlain dp;
        PlayerCharacter player;
        FollowCamera follow;
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
            go.AddComponent<TerrainEditInput>();
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

        IEnumerator Build(int x0, int x1, int y0, int y1, int z0, int z1)
        {
            int n = (x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1);
            view.Bag.Add(BlockMaterial.Stone, n);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                        Assert.AreEqual(EditResult.Ok, view.Place(C(x, y, z), BlockMaterial.Stone), $"Đặt ({x},{y},{z})");
            yield return null;
            yield return null;
        }

        Vector3 Feet => player.FeetLocal;

        [UnityTest]
        public IEnumerator SpawnsAndStandsOnGround()
        {
            Assert.IsTrue(player.Ready);
            yield return Run(Vector2.zero, false, 1f);
            Assert.IsTrue(player.Controller.isGrounded, "đứng trên đất");
            Assert.AreEqual(player.transform, view.streamTarget, "chunk nạp theo nhân vật");
            var f = Feet;
            bool nearCheckpoint = false;
            foreach (var l in view.Landmarks)
                if (l.Kind == LandmarkKind.Checkpoint && l.Map == 0 && Mathf.Abs(f.x - (l.X + 0.5f)) < 0.2f && Mathf.Abs(f.z - (l.Z - 1.5f)) < 0.2f)
                {
                    nearCheckpoint = true;
                    MinimapImage.ColumnInfo(view.World, l.X, l.Z - 2, out int s, out _);
                    Assert.Less(Mathf.Abs(f.y - (s + 1)), 0.1f, "chân ở mặt đất cạnh checkpoint");
                }
            if (!nearCheckpoint)
            {
                var spawn = PlayerCharacter.PlainSpawn(plain);
                Assert.Less(Mathf.Abs(f.x - spawn.x), 0.1f, "x");
                Assert.Less(Mathf.Abs(f.z - spawn.z), 0.1f, "z");
                Assert.Less(Mathf.Abs(f.y - spawn.y), 0.1f, "y");
            }
        }

        [UnityTest]
        public IEnumerator WalksAlongCameraYaw()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            var p0 = Feet;
            follow.SetAngles(90f, 20f);
            yield return Run(new Vector2(0, 1), false, 1f);
            var p1 = Feet;
            Assert.GreaterOrEqual(p1.x - p0.x, 3.5f, "đi theo +x");
            Assert.Less(Mathf.Abs(p1.z - p0.z), 0.5f, "không lệch z");
        }

        [UnityTest]
        public IEnumerator JumpsOneBlockStepButNotTwo()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            yield return Build(A.x - 1, A.x + 1, G + 1, G + 1, A.z + 2, A.z + 2);
            follow.SetAngles(0f, 20f);
            yield return Run(new Vector2(0, 1), true, 1.5f);
            Assert.GreaterOrEqual(Feet.y, G + 2 - 0.1f, $"lên được bậc 1 ô (y={Feet.y:F2})");
            Assert.Greater(Feet.z, A.z + 2, "qua bậc");

            yield return Teleport(new Vector3(A.x + 6.5f, G + 1, A.z + 0.5f));
            yield return Build(A.x + 5, A.x + 7, G + 1, G + 2, A.z + 2, A.z + 2);
            yield return Run(new Vector2(0, 1), true, 1.5f);
            // Nhảy liên tục nên có thể đang lơ lửng ở đỉnh (≈ G + 2,28) — đứng yên 1 giây cho hạ xuống rồi mới đo bậc.
            yield return Run(Vector2.zero, false, 1f);
            Assert.Less(Feet.z, A.z + 2 - 0.3f, $"không qua tường 2 ô (z={Feet.z:F2})");
            Assert.Less(Feet.y, G + 2, $"không lên bậc 2 ô (y={Feet.y:F2})");
        }

        [UnityTest]
        public IEnumerator DoesNotWalkThroughBlocks()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            yield return Build(A.x - 1, A.x + 1, G + 1, G + 2, A.z + 2, A.z + 2);
            follow.SetAngles(0f, 20f);
            yield return Run(new Vector2(0, 1), false, 1.5f);
            Assert.LessOrEqual(Feet.z, A.z + 2 - 0.4f + 0.05f, $"z={Feet.z:F2}");
        }

        [UnityTest]
        public IEnumerator FallsWhenGroundDugBelow()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            Assert.AreEqual(EditResult.Ok, view.Dig(C(A.x, G, A.z)));
            Assert.AreEqual(EditResult.Ok, view.Dig(C(A.x, G - 1, A.z)));
            Assert.AreEqual(EditResult.Ok, view.Dig(C(A.x, G - 2, A.z)));
            yield return Run(Vector2.zero, false, 1f);
            Assert.Less(Feet.y, G - 1, $"y={Feet.y:F2}");
        }

        [UnityTest]
        public IEnumerator StaysInsideWorldBounds()
        {
            MinimapImage.ColumnInfo(view.World, 1, A.z, out int s, out int w);
            yield return Teleport(new Vector3(1.5f, s + w + 1, A.z + 0.5f));
            follow.SetAngles(270f, 20f);
            yield return Run(new Vector2(0, 1), false, 2f);
            Assert.GreaterOrEqual(Feet.x, 0.4f - 0.01f, $"x={Feet.x:F2}");
        }

        IEnumerator Distance(float seconds, System.Action<float> result)
        {
            var p0 = Feet;
            yield return Run(new Vector2(0, 1), false, seconds);
            var p1 = Feet;
            result(Mathf.Sqrt((p1.x - p0.x) * (p1.x - p0.x) + (p1.z - p0.z) * (p1.z - p0.z)));
        }

        [UnityTest]
        public IEnumerator WaterHalvesSpeed()
        {
            int wx = -1, ws = 0;
            for (int x = 1; x < 60 && wx < 0; x++)
            {
                MinimapImage.ColumnInfo(view.World, x, A.z, out int s, out int d);
                if (d >= 4) { wx = x; ws = s; }
            }
            if (wx < 0) Assert.Inconclusive("Không có cột nước sâu ≥ 4 ở z = A.z");
            yield return Teleport(new Vector3(wx + 0.5f, ws + 1, A.z + 0.5f));
            for (int i = 0; i < 180 && !player.Controller.isGrounded; i++)
                yield return null;
            Assert.IsTrue(player.Controller.isGrounded, "chạm đáy");
            yield return null;
            Assert.IsTrue(player.InWater, "đang trong nước");
            follow.SetAngles(90f, 20f);
            float dWater = 0f;
            yield return Distance(1f, d => dWater = d);

            // Camera dưới nước (đo trước khi rời chỗ nước).
            follow.pitch = 0f;
            follow.distance = follow.minDistance;
            follow.SnapNow();
            yield return null;
            yield return null;
            Assert.IsTrue(camGo.GetComponent<UnderwaterView>().IsUnderwater, "camera dưới nước");

            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            follow.SetAngles(90f, 20f);
            float dLand = 0f;
            yield return Distance(1f, d => dLand = d);
            Assert.Greater(dLand, 1f, "đi được trên cạn");
            float ratio = dWater / dLand;
            Assert.That(ratio, Is.InRange(0.4f, 0.6f), $"dWater={dWater:F2} dLand={dLand:F2}");
        }

        [UnityTest]
        public IEnumerator CameraPullsInWhenBlocked()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            follow.SetAngles(0f, 10f);
            follow.distance = 6f;
            yield return null;
            Assert.AreEqual(6f, follow.CurrentDistance, 0.01f);
            yield return Build(A.x, A.x, G + 1, G + 3, A.z - 2, A.z - 2);
            yield return null;
            yield return null;
            Assert.Less(follow.CurrentDistance, 2f, $"d={follow.CurrentDistance:F2}");
            var cell = view.CellAt(camGo.transform.position);
            var m = view.World.Size.InBounds(cell.x, cell.y, cell.z) ? view.World.GetMaterial(cell.x, cell.y, cell.z) : BlockMaterial.Empty;
            Assert.That(m == BlockMaterial.Empty || m == BlockMaterial.Water, $"camera trong ô đặc {cell} = {m}");
        }

        [UnityTest]
        public IEnumerator ReachLimitsTools()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            var tei = go.GetComponent<TerrainEditInput>();
            var far = C(A.x + 10, G, A.z);
            var before = view.World.GetMaterial(far.x, far.y, far.z);
            Assert.AreEqual(EditResult.OutOfReach, tei.UseTool(far, far));
            Assert.AreEqual(before, view.World.GetMaterial(far.x, far.y, far.z), "ô xa không đổi");
            tei.ShowAim(true, far, far);
            Assert.IsFalse(tei.OutlineVisible, "khung ngoài tầm");
            var near = C(A.x + 9, G, A.z);
            tei.ShowAim(true, near, near);
            Assert.IsTrue(tei.OutlineVisible, "khung trong tầm");
            Assert.AreEqual(EditResult.Ok, tei.UseTool(near, near));
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(near.x, near.y, near.z));

            tei.ToggleTool();
            view.Bag.Add(BlockMaterial.Dirt, 1);
            var pc = C(A.x + 11, G + 1, A.z);
            Assert.AreEqual(EditResult.OutOfReach, tei.UseTool(pc, pc));
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(pc.x, pc.y, pc.z));
            tei.ShowAim(true, pc, pc);
            Assert.IsFalse(tei.GhostVisible, "bản mờ ngoài tầm");
        }

        [UnityTest]
        public IEnumerator CannotPlaceIntoPlayerCells()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            view.Bag.Add(BlockMaterial.Dirt, 2);
            Assert.AreEqual(EditResult.Occupied, view.Place(C(A.x, G + 1, A.z), BlockMaterial.Dirt));
            Assert.IsTrue(player.OccupiesCell(A.x, G + 2, A.z));
            Assert.IsFalse(player.OccupiesCell(A.x, G + 3, A.z));
            Assert.AreEqual(EditResult.Ok, view.Place(C(A.x + 1, G + 1, A.z), BlockMaterial.Dirt));
        }

        [UnityTest]
        public IEnumerator FlyModeToggle()
        {
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            var fly = camGo.GetComponent<DebugFlyCamera>();
            player.SetFlyMode(true);
            Assert.IsTrue(fly.enabled);
            Assert.IsFalse(follow.enabled);
            var p0 = player.transform.position;
            yield return Run(new Vector2(0, 1), false, 0.5f);
            // Run trả input về 0; gán lại và chạy để chắc FlyMode xoá input kể cả khi readKeyboard = false.
            player.MoveInput = new Vector2(0, 1);
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.Less(Vector3.Distance(p0, player.transform.position), 0.05f, "nhân vật đứng yên khi bay");
            camGo.transform.position += Vector3.up * 20f;
            Assert.IsTrue(player.InToolReach(C(A.x + 3, G, A.z)), "tầm theo nhân vật");
            player.SetFlyMode(false);
            Assert.IsTrue(follow.enabled);
            var pivot = player.transform.position + Vector3.up * follow.pivotHeight;
            Assert.LessOrEqual(Vector3.Distance(camGo.transform.position, pivot), follow.maxDistance + 0.01f);
        }

        [UnityTest]
        public IEnumerator MinimapFollowsPlayer()
        {
            var map = go.AddComponent<MinimapView>();
            for (int i = 0; i < 5 && !map.IsReady; i++)
                yield return null;
            Assert.IsTrue(map.IsReady, "Bản đồ chưa dựng");
            yield return Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f));
            map.SetOpen(true);
            player.SetFlyMode(true);
            camGo.transform.position += Vector3.right * 10f;
            yield return null;
            var fp = view.transform.InverseTransformPoint(player.transform.position);
            map.Viewport.WorldToPanel(fp.x, fp.z, out float px, out float py);
            Assert.AreEqual(px, map.MarkerPanelPosition.x, 0.5f);
            Assert.AreEqual(py, map.MarkerPanelPosition.y, 0.5f);
        }
    }
}
