using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l d2 (brief 0062): giao diện túi / túi phụ / kho ô vuông (InventoryView).</summary>
    public class InventoryPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        TerrainEditInput input;
        PlayerCharacter player;
        InventoryView inv;
        Vector3Int spot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            camGo.AddComponent<FollowCamera>();
            go = new GameObject("Terrain");
            go.SetActive(false);
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            sv = go.AddComponent<SimView>();
            input = go.AddComponent<TerrainEditInput>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            go.SetActive(true);
            for (int i = 0; i < 600 && sv.Sim == null; i++) yield return null;
            Assert.IsNotNull(sv.Sim);
            inv = input.Inventory;
            Assert.IsNotNull(inv);

            var s0 = sv.Sim.State.Plots[0].Soil;
            var all = new List<Vector3Int>();
            for (int dz = -8; dz <= 8; dz++)
                for (int dx = -8; dx <= 8; dx++)
                {
                    int x = s0.X + dx, z = s0.Z + dz;
                    if (!view.World.Size.InBounds(x, 0, z)) continue;
                    int h = view.World.SurfaceHeight(x, z);
                    if (sv.Sim.CanMarkTill(new CellPos(x, h, z))) all.Add(new Vector3Int(x, h, z));
                }
            bool found = false;
            foreach (var c in all)
            {
                int n = 0;
                foreach (var d in all)
                {
                    float e = Mathf.Sqrt((d.x - c.x) * (d.x - c.x) + (d.z - c.z) * (d.z - c.z));
                    if (e >= 3.5f && e <= 6.5f) n++;
                }
                if (n >= 6) { spot = c; found = true; break; }
            }
            Assert.IsTrue(found, "Could not find a standing spot around the camp");
            player.Place(view.transform.TransformPoint(new Vector3(spot.x + 0.5f, spot.y + 1f, spot.z + 0.5f)), 0f);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            Assert.IsTrue(player.Ready);
            for (int i = 0; i < 20; i++) yield return null;
            sv.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        IEnumerator Frames(int n = 2)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        IEnumerator PlaceAt(int x, int z)
        {
            player.Place(view.transform.TransformPoint(new Vector3(x + 0.5f, view.World.SurfaceHeight(x, z) + 1f, z + 0.5f)), 0f);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            Assert.IsTrue(player.Ready);
            for (int i = 0; i < 5; i++) yield return null;
        }

        /// <summary>Đứng cạnh khối kho 0 (trong tầm thao tác).</summary>
        IEnumerator GoNearStorage()
        {
            var c = sv.Sim.State.Storages[0].Cell;
            yield return PlaceAt(c.X + 2, c.Z);
        }

        IEnumerator GoFarFromStorage()
        {
            foreach (var dz in new[] { 14, -14, 20, -20 })
                foreach (var dx in new[] { 14, -14, 0 })
                {
                    int x = spot.x + dx, z = spot.z + dz;
                    if (!view.World.Size.InBounds(x, 0, z)) continue;
                    bool far = true;
                    foreach (var s in sv.Sim.State.Storages)
                        if (Mathf.Sqrt((s.Cell.X - x) * (s.Cell.X - x) + (s.Cell.Z - z) * (s.Cell.Z - z)) < 12f) far = false;
                    if (!far) continue;
                    yield return PlaceAt(x, z);
                    yield break;
                }
            Assert.Fail("No spot >= 12 cells from storage");
        }

        [UnityTest]
        public IEnumerator BagAndHotbarToggle()
        {
            Assert.IsFalse(inv.BagOpen);
            Assert.IsTrue(inv.HotbarShown);
            inv.ToggleHotbar();
            Assert.IsFalse(inv.HotbarShown);
            inv.ToggleBag();
            Assert.IsTrue(inv.BagOpen);
            Assert.IsTrue(inv.HotbarShown);
            inv.ToggleBag();
            Assert.IsFalse(inv.HotbarShown);
            inv.ToggleHotbar();
            Assert.IsTrue(inv.HotbarShown);
            yield break;
        }

        [UnityTest]
        public IEnumerator SlotsShowIconAndCount()
        {
            view.Bag.Add(BlockMaterial.Dirt, 3);
            inv.ToggleBag();
            yield return Frames();
            var dirt = ItemId.Block(BlockMaterial.Dirt);
            Assert.AreEqual(dirt, inv.SlotAt(ItemContainer.Bag, 1).Item);
            Assert.AreEqual(3, inv.SlotAt(ItemContainer.Bag, 1).Count);
            Assert.AreEqual("3", inv.SlotCountText(ItemContainer.Bag, 1));
            Assert.AreEqual("1", inv.SlotCountText(ItemContainer.Bag, 0)); // camera quan sát
            Assert.AreEqual("", inv.SlotCountText(ItemContainer.Bag, 2));
            Assert.AreEqual(DropView.ColorOf(dirt), InventoryView.IconColor(dirt));
            inv.NextPage(ItemContainer.Bag);
            Assert.AreEqual(0, inv.BagPage); // 1 trang, vòng
        }

        [UnityTest]
        public IEnumerator InfoPanelCounts()
        {
            view.Bag.AddSeeds(0, 4);
            int s0 = sv.Sim.State.Stock.Seeds[0];
            inv.ToggleBag();
            yield return Frames();
            Assert.IsTrue(inv.LeftClickAt(inv.SlotScreenCenter(ItemContainer.Bag, 1)));
            Assert.IsTrue(inv.InfoVisible);
            StringAssert.Contains("Túi hạt Cây A", inv.InfoText); // vi-text
            StringAssert.Contains("Trong túi (gồm túi phụ): 4", inv.InfoText); // vi-text
            StringAssert.Contains("Trong kho: " + s0, inv.InfoText); // vi-text
            view.Bag.Hotbar.AddToSlot(0, ItemId.SeedBag(0), 2);
            yield return Frames(1);
            StringAssert.Contains("Trong túi (gồm túi phụ): 6", inv.InfoText); // vi-text
            Assert.IsTrue(inv.LeftClickAt(inv.SlotScreenCenter(ItemContainer.Bag, 1)));
            Assert.IsFalse(inv.InfoVisible);
        }

        [UnityTest]
        public IEnumerator RightClickSlotHoldsAndCloses()
        {
            view.Bag.Add(BlockMaterial.Dirt, 2);
            view.Bag.AddProduce(0, 1);
            inv.ToggleBag();
            yield return Frames();
            Assert.IsTrue(inv.RightClickAt(inv.SlotScreenCenter(ItemContainer.Bag, 2)));
            Assert.IsFalse(input.Control.HasHeld);
            Assert.IsTrue(inv.BagOpen);
            StringAssert.Contains("Không cầm được", input.HudText()); // vi-text
            Assert.IsTrue(inv.RightClickAt(inv.SlotScreenCenter(ItemContainer.Bag, 1)));
            Assert.AreEqual(ItemId.Block(BlockMaterial.Dirt), input.Control.Held);
            Assert.IsFalse(inv.BagOpen);
            Assert.IsFalse(inv.HasSelection);
        }

        [UnityTest]
        public IEnumerator HotbarKeysHold()
        {
            var dirt = ItemId.Block(BlockMaterial.Dirt);
            view.Bag.Hotbar.AddToSlot(0, dirt, 2);
            Assert.IsTrue(inv.UseHotbarKey(0));
            Assert.AreEqual(dirt, input.Control.Held);
            input.Control.ClearToDefault();
            Assert.IsFalse(inv.UseHotbarKey(1)); // ô trống
            Assert.IsFalse(inv.UseHotbarKey(5)); // ngoài 2 ô
            inv.ToggleBag();
            Assert.IsFalse(inv.UseHotbarKey(0)); // túi mở: 1–0 không cầm
            inv.ToggleBag();
            inv.ToggleHotbar(); // ẩn
            Assert.IsTrue(inv.UseHotbarKey(0)); // phím vẫn dùng được khi ẩn
            yield break;
        }

        [UnityTest]
        public IEnumerator StockOpensWithBagAndAutoCloses()
        {
            yield return GoNearStorage();
            Assert.IsTrue(sv.OpenStockAt(0));
            Assert.IsTrue(inv.StockOpen);
            Assert.IsTrue(inv.BagOpen);
            Assert.Greater(sv.Sim.State.Stock.Seeds[0], 0);
            Assert.AreEqual(ItemId.SeedBag(0), inv.SlotAt(ItemContainer.Stock, 0).Item);
            inv.ToggleBag();
            Assert.IsFalse(inv.StockOpen);
            Assert.IsTrue(sv.OpenStockAt(0));
            yield return GoFarFromStorage();
            yield return Frames();
            Assert.IsFalse(inv.StockOpen);
            Assert.IsTrue(inv.BagOpen);
        }

        [UnityTest]
        public IEnumerator PointerBlockAndConsumedClick()
        {
            view.Bag.Add(BlockMaterial.Dirt, 1);
            inv.ToggleBag();
            yield return Frames();
            var p = inv.SlotScreenCenter(ItemContainer.Bag, 1);
            Assert.IsTrue(InventoryView.BlocksPointer(p));
            inv.ToggleBag();
            yield return Frames();
            Assert.IsFalse(InventoryView.BlocksPointer(p));
            Assert.IsTrue(InventoryView.BlocksPointer(inv.SlotScreenCenter(ItemContainer.Hotbar, 0)));
            inv.ToggleHotbar();
            yield return Frames();
            Assert.IsFalse(InventoryView.BlocksPointer(inv.SlotScreenCenter(ItemContainer.Hotbar, 0)));
            Assert.IsFalse(InventoryView.BlocksPointer(new Vector2(Screen.width / 2, Screen.height / 2)));
        }

        [UnityTest]
        public IEnumerator EscAndSettings()
        {
            var settings = go.GetComponent<SettingsView>();
            Assert.IsNotNull(settings);
            inv.ToggleBag();
            Assert.IsTrue(settings.OtherPanelOpen());
            inv.CloseAll();
            Assert.IsFalse(InventoryView.AnyOpen);
            Assert.IsFalse(settings.OtherPanelOpen());
            yield break;
        }

        IEnumerator Drag(Vector2 from, Vector2 to)
        {
            inv.PointerDown(from);
            yield return null;
            inv.PointerMove(Vector2.Lerp(from, to, 0.5f));
            inv.PointerMove(to);
            yield return null;
            inv.PointerUp(to);
            yield return null;
        }

        static readonly ItemId Dirt = ItemId.Block(BlockMaterial.Dirt);
        static readonly ItemId Stone = ItemId.Block(BlockMaterial.Stone);
        static readonly ItemId Sand = ItemId.Block(BlockMaterial.Sand);

        [UnityTest]
        public IEnumerator DragBagToHotbarMovesStack()
        {
            view.Bag.Add(BlockMaterial.Dirt, 5);
            inv.ToggleBag();
            yield return Frames();
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Bag, 1), inv.SlotScreenCenter(ItemContainer.Hotbar, 1));
            Assert.AreEqual(Dirt, view.Bag.Hotbar.Get(1).Item);
            Assert.AreEqual(5, view.Bag.Hotbar.Get(1).Count);
            Assert.IsTrue(view.Bag.Main.Get(1).IsEmpty);
            Assert.IsFalse(inv.Dragging);
        }

        [UnityTest]
        public IEnumerator DragSwapsAndMerges()
        {
            view.Bag.Add(BlockMaterial.Dirt, 3);
            view.Bag.Hotbar.AddToSlot(0, Stone, 2);
            inv.ToggleBag();
            yield return Frames();
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Bag, 1), inv.SlotScreenCenter(ItemContainer.Hotbar, 0));
            Assert.AreEqual(Dirt, view.Bag.Hotbar.Get(0).Item);
            Assert.AreEqual(3, view.Bag.Hotbar.Get(0).Count);
            Assert.AreEqual(Stone, view.Bag.Main.Get(1).Item);
            Assert.AreEqual(2, view.Bag.Main.Get(1).Count);
            view.Bag.Main.AddToSlot(2, Dirt, 4);
            yield return Frames(1);
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Bag, 2), inv.SlotScreenCenter(ItemContainer.Hotbar, 0));
            Assert.AreEqual(7, view.Bag.Hotbar.Get(0).Count);
            Assert.IsTrue(view.Bag.Main.Get(2).IsEmpty);
        }

        [UnityTest]
        public IEnumerator DragBetweenBagAndStock()
        {
            yield return GoNearStorage();
            Assert.IsTrue(sv.OpenStockAt(0));
            var stock = sv.Sim.State.Stock;
            int s0 = stock.Seeds[0];
            view.Bag.AddSeeds(0, 3);
            yield return Frames();
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Bag, 1), inv.SlotScreenCenter(ItemContainer.Stock, 0));
            Assert.AreEqual(s0 + 3, stock.Seeds[0]);
            Assert.IsTrue(view.Bag.Main.Get(1).IsEmpty);
            StringAssert.Contains("Đã chuyển 3", input.HudText()); // vi-text
            Assert.AreEqual(ItemId.SeedBag(0), inv.SlotAt(ItemContainer.Stock, 0).Item);
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Stock, 0), inv.SlotScreenCenter(ItemContainer.Bag, 2));
            Assert.AreEqual(ItemId.SeedBag(0), view.Bag.Main.Get(2).Item);
            Assert.AreEqual(s0 + 3, view.Bag.Main.Get(2).Count);
            Assert.AreEqual(0, stock.Seeds[0]);
            view.Bag.Add(BlockMaterial.Dirt, 2);
            yield return Frames(1);
            Assert.AreEqual(Dirt, view.Bag.Main.Get(1).Item);
            yield return Drag(inv.SlotScreenCenter(ItemContainer.Bag, 1), inv.SlotScreenCenter(ItemContainer.Stock, 0));
            Assert.AreEqual(2, view.Bag.Main.Get(1).Count);
            StringAssert.Contains("Kho không nhận", input.HudText()); // vi-text
        }

        [UnityTest]
        public IEnumerator ClickIsNotDrag()
        {
            view.Bag.Add(BlockMaterial.Dirt, 1);
            inv.ToggleBag();
            yield return Frames();
            var p = inv.SlotScreenCenter(ItemContainer.Bag, 1);
            Assert.IsTrue(inv.PointerDown(p));
            inv.PointerMove(p + new Vector2(3f, 0f));
            Assert.IsTrue(inv.PointerUp(p + new Vector2(3f, 0f)));
            Assert.IsTrue(inv.InfoVisible);
            Assert.IsFalse(inv.Dragging);
            Assert.AreEqual(Dirt, view.Bag.Main.Get(1).Item);
        }

        [UnityTest]
        public IEnumerator DropOutsideCancels()
        {
            view.Bag.Add(BlockMaterial.Dirt, 1);
            inv.ToggleBag();
            yield return Frames();
            var mid = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Assert.IsFalse(inv.Covers(mid));
            var p = inv.SlotScreenCenter(ItemContainer.Bag, 1);
            inv.PointerDown(p);
            inv.PointerMove(mid);
            Assert.IsTrue(inv.Dragging);
            Assert.IsTrue(InventoryView.BlocksPointer(mid));
            inv.PointerUp(mid);
            Assert.IsFalse(inv.Dragging);
            Assert.AreEqual(Dirt, view.Bag.Main.Get(1).Item);
        }

        [UnityTest]
        public IEnumerator KeysDefaultTargetTabAndParts()
        {
            yield return GoNearStorage();
            Assert.IsTrue(sv.OpenStockAt(0));
            view.Bag.AddSeeds(0, 9);
            Assert.IsTrue(inv.Select(ItemContainer.Bag, 1));
            Assert.AreEqual(ItemContainer.Hotbar, inv.CurrentTarget);
            Assert.AreEqual(5, inv.TransferPart(2));
            Assert.AreEqual(5, view.Bag.Hotbar.Count(ItemId.SeedBag(0)));
            Assert.AreEqual(4, view.Bag.Main.Get(1).Count);
            inv.NextTarget();
            Assert.AreEqual(ItemContainer.Stock, inv.CurrentTarget);
            StringAssert.Contains("Chuyển tới: kho", inv.InfoText); // vi-text
            Assert.AreEqual(2, inv.TransferPart(3));
            Assert.AreEqual(2, inv.TransferPart(1));
            Assert.IsTrue(view.Bag.Main.Get(1).IsEmpty);
            Assert.IsFalse(inv.HasSelection);
        }

        [UnityTest]
        public IEnumerator TabWithoutStock()
        {
            view.Bag.Add(BlockMaterial.Dirt, 2);
            inv.ToggleBag();
            Assert.IsTrue(inv.Select(ItemContainer.Bag, 1));
            Assert.AreEqual(ItemContainer.Hotbar, inv.CurrentTarget);
            inv.NextTarget();
            Assert.AreEqual(ItemContainer.Hotbar, inv.CurrentTarget);
            Assert.AreEqual(1, inv.TransferPart(2));
            Assert.IsTrue(inv.Select(ItemContainer.Hotbar, 0));
            Assert.AreEqual(ItemContainer.Bag, inv.CurrentTarget);
            yield break;
        }

        [UnityTest]
        public IEnumerator TransferNeedsBagOpen()
        {
            view.Bag.Hotbar.AddToSlot(0, Dirt, 2);
            Assert.IsTrue(inv.Select(ItemContainer.Hotbar, 0));
            Assert.IsFalse(inv.TransferActive);
            Assert.AreEqual(0, inv.TransferPart(1));
            Assert.AreEqual(2, view.Bag.Hotbar.Get(0).Count);
            yield break;
        }

        [UnityTest]
        public IEnumerator TargetFullMessage()
        {
            inv.ToggleBag();
            view.Bag.Hotbar.AddToSlot(0, Stone, 999);
            view.Bag.Hotbar.AddToSlot(1, Sand, 999);
            view.Bag.Add(BlockMaterial.Dirt, 3);
            Assert.IsTrue(inv.Select(ItemContainer.Bag, 1));
            Assert.AreEqual(0, inv.TransferPart(1));
            StringAssert.Contains("Không chuyển được: túi phụ đầy", input.HudText()); // vi-text
            yield break;
        }
    }
}
