using System.Collections;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7g (brief 0025): ô minimap tròn góc trên-phải — mặt nạ tròn, chung texture, theo người chơi, tên map, dấu mốc, bấm mở/đóng, chặn con trỏ.</summary>
    public class MinimapCornerPlayTests
    {
        const float K = 188f / 64f;
        GameObject camGo, go;
        TerrainChunkView view;
        MinimapView map;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(192, 40, 60);
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            map = go.AddComponent<MinimapView>();
            for (int i = 0; i < 5 && !map.IsReady; i++)
                yield return null;
            Assert.IsTrue(map.IsReady, "Map not built");
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        static int FirstColumnOf(MapTable maps, int m, out int z)
        {
            for (z = 0; z < maps.SizeZ; z++)
                for (int x = 0; x < maps.SizeX; x++)
                    if (maps.Get(x, z) == m)
                        return x;
            z = -1;
            return -1;
        }

        void Place(Vector3 pos, Vector3 forward)
        {
            camGo.transform.position = pos;
            camGo.transform.rotation = Quaternion.LookRotation(forward);
        }

        [UnityTest]
        public IEnumerator CornerBuiltWithCircleMaskAndSharedTexture()
        {
            Assert.IsFalse(map.IsOpen);
            Assert.IsTrue(map.CornerActive);
            Assert.IsFalse(map.PanelActive);
            Assert.IsTrue(map.CornerMaskIsDisk);
            Assert.IsTrue(ReferenceEquals(map.CornerTexture, map.Texture));
            Assert.AreEqual("N", map.CornerNorthText);
            Assert.AreEqual(K, map.CornerViewport.PixelsPerColumn, 1e-4f);
            Assert.Greater(map.CornerBorderVertexCount, 0);
            Assert.Less(map.CornerBorderVertexCount, 65000);
            Assert.AreEqual(view.Landmarks.Count, map.CornerLandmarkCount);
            Debug.Log($"MINIMAPCORNER border vertices {map.CornerBorderVertexCount}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CornerFollowsCameraPositionAndHeading()
        {
            Place(new Vector3(100.5f, 40, 250.5f), Vector3.right);
            yield return null;
            Assert.AreEqual(-100.5f * K, map.CornerContentPosition.x, 0.01f);
            Assert.AreEqual(-250.5f * K, map.CornerContentPosition.y, 0.01f);
            var uv = map.CornerUvRect;
            Assert.AreEqual((100.5f - 32f) / 384f, uv.x, 1e-4f);
            Assert.AreEqual((250.5f - 32f) / 384f, uv.y, 1e-4f);
            Assert.AreEqual(64f / 384f, uv.width, 1e-4f);
            Assert.AreEqual(64f / 384f, uv.height, 1e-4f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(270f, map.CornerMarkerAngle)), 0.5f);

            Place(new Vector3(192.5f, 40, 192.5f), Vector3.forward);
            yield return null;
            Assert.AreEqual(-192.5f * K, map.CornerContentPosition.x, 0.01f);
            Assert.AreEqual(-192.5f * K, map.CornerContentPosition.y, 0.01f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, map.CornerMarkerAngle)), 0.5f);
        }

        [UnityTest]
        public IEnumerator CornerShowsMapNameUnderPlayer()
        {
            for (int m = 0; m < view.Maps.MapCount; m++)
            {
                int x = FirstColumnOf(view.Maps, m, out int z);
                Assert.GreaterOrEqual(x, 0, "map " + m + " has no column");
                Place(new Vector3(x + 0.5f, 40, z + 0.5f), Vector3.forward);
                yield return null;
                Assert.AreEqual(view.Maps.Name(m), map.CornerMapName, "map " + m);
            }
            Assert.AreEqual(MapTable.Ocean, view.Maps.Get(2, 2));
            Place(new Vector3(2.5f, 40, 2.5f), Vector3.forward);
            yield return null;
            Assert.IsNull(map.CornerMapName);
            Place(new Vector3(-10f, 40, -10f), Vector3.forward);
            yield return null;
            Assert.IsNull(map.CornerMapName);
        }

        [UnityTest]
        public IEnumerator CornerLandmarkIconsAtLandmarkPositions()
        {
            for (int i = 0; i < view.Landmarks.Count; i++)
            {
                var l = view.Landmarks[i];
                map.CornerViewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float px, out float py);
                var p = map.CornerLandmarkPosition(i);
                Assert.AreEqual(px, p.x, 0.01f, "landmark " + i);
                Assert.AreEqual(py, p.y, 0.01f, "landmark " + i);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DigVisibleInCorner()
        {
            var w = view.World;
            int wl = view.Editor.SeaLevel;
            int fx = -1, fz = -1, top = -1;
            for (int z = 80; z < 160 && fx < 0; z++)
                for (int x = 150; x < 234 && fx < 0; x++)
                {
                    MinimapImage.ColumnInfo(w, x, z, out int s, out int d);
                    if (d == 0 && s % MinimapImage.TierHeight == 0 && s >= wl + 2 && Cell.IsDiggable(w.Get(x, s, z)))
                    {
                        fx = x;
                        fz = z;
                        top = s;
                    }
                }
            Assert.GreaterOrEqual(fx, 0, "no column at the edge of the tier nearest the camera");
            int idx = fx + w.Size.X * fz;
            var tex = (Texture2D)map.CornerTexture;
            var before = tex.GetPixels32()[idx];
            Assert.AreEqual(EditResult.Ok, view.Dig(new Vector3Int(fx, top, fz)));
            yield return null;
            var after = ((Texture2D)map.CornerTexture).GetPixels32()[idx];
            uint c = MinimapImage.ColumnColor(w, fx, fz);
            Assert.AreEqual(new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, 255), after);
            Assert.AreNotEqual(before, after, "pixel must change");
        }

        [UnityTest]
        public IEnumerator PressCornerOpensAndCloseButtonCloses()
        {
            Assert.IsTrue(map.PressAt(map.ButtonScreenCenter));
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(map.IsOpen);
            Assert.IsTrue(map.PanelActive);
            Assert.IsFalse(map.CornerActive);
            Assert.AreEqual("Đóng (M)", map.CloseButtonText); // vi-text

            Assert.IsTrue(map.PressAt(map.ButtonScreenCenter));
            yield return null;
            Assert.IsFalse(map.IsOpen);
            Assert.IsTrue(map.CornerActive);

            Assert.IsFalse(map.PressAt(new Vector2(5, 5)));
            Assert.IsFalse(map.IsOpen);
        }

        [UnityTest]
        public IEnumerator CornerHitTestIsCircular()
        {
            Assert.IsTrue(MinimapView.BlocksPointer(map.CornerScreenPoint(Vector2.zero)));
            Assert.IsTrue(MinimapView.BlocksPointer(map.CornerScreenPoint(new Vector2(0, -95))));
            Assert.IsFalse(MinimapView.BlocksPointer(map.CornerScreenPoint(new Vector2(-95, -95))));
            Assert.IsFalse(MinimapView.BlocksPointer(new Vector2(5, 5)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ToggleFrameBlocksPointer()
        {
            Assert.IsTrue(map.PressAt(map.ButtonScreenCenter));
            map.SetOpen(false);
            Assert.IsTrue(MinimapView.BlocksPointer(new Vector2(5, 5)));
            yield return null;
            Assert.IsFalse(MinimapView.BlocksPointer(new Vector2(5, 5)));
        }
    }
}
