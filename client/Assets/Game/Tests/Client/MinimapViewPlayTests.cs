using System.Collections;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7c (brief 0016): bản đồ dựng từ world v3 — ảnh nền, vùng nhìn, tên map, dấu người chơi, chặn chuột.</summary>
    public class MinimapViewPlayTests
    {
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
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        static void AssertUvMatches(MinimapView map)
        {
            map.Viewport.VisibleUv(out float u, out float v, out float w, out float h);
            var r = map.UvRect;
            Assert.AreEqual(u, r.x, 1e-4f, "u");
            Assert.AreEqual(v, r.y, 1e-4f, "v");
            Assert.AreEqual(w, r.width, 1e-4f, "w");
            Assert.AreEqual(h, r.height, 1e-4f, "h");
        }

        [UnityTest]
        public IEnumerator TextureMatchesCoreImage()
        {
            var tex = map.Texture;
            Assert.AreEqual(384, tex.width);
            Assert.AreEqual(384, tex.height);
            Assert.AreEqual(FilterMode.Point, tex.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, tex.wrapMode);
            var px = tex.GetPixels32();
            var rgba = MinimapImage.Build(view.World);
            Assert.AreEqual(rgba.Length, px.Length * 4);
            for (int i = 0; i < px.Length; i++)
                if (px[i].r != rgba[i * 4] || px[i].g != rgba[i * 4 + 1] || px[i].b != rgba[i * 4 + 2] || px[i].a != 255)
                    Assert.Fail($"pixel {i} (x={i % 384}, z={i / 384}) differs");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosedByDefaultAndToggles()
        {
            Assert.IsFalse(map.IsOpen);
            Assert.IsFalse(map.PanelActive);
            Assert.IsFalse(MinimapView.IsAnyOpen);
            map.SetOpen(true);
            Assert.IsTrue(map.IsOpen);
            Assert.IsTrue(map.PanelActive);
            Assert.IsTrue(MinimapView.IsAnyOpen);
            yield return null;
            map.SetOpen(false);
            Assert.IsFalse(map.PanelActive);
            Assert.IsFalse(MinimapView.IsAnyOpen);
        }

        [UnityTest]
        public IEnumerator UvRectAndBordersFollowViewport()
        {
            map.SetOpen(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertUvMatches(map);
            Assert.AreEqual(1f, map.Viewport.Zoom, 1e-5f);
            Assert.Greater(map.BorderCount, 0);
            Assert.Greater(map.BorderVertexCount, 0, "border vertices not built");
            Assert.Less(map.BorderVertexCount, 65000, "exceeds UI vertex limit");
            Debug.Log($"MINIMAPTEST border {map.BorderCount} lines, {map.BorderVertexCount} vertices, viewport {map.Viewport.PanelWidth:F0}x{map.Viewport.PanelHeight:F0}, build {map.BuildMs:F1} ms");

            var vp = map.Viewport;
            vp.ZoomAt(vp.PanelWidth * 0.5f, vp.PanelHeight * 0.5f, 4f);
            yield return null;
            Assert.AreEqual(4f, vp.Zoom, 1e-4f);
            AssertUvMatches(map);
        }

        [UnityTest]
        public IEnumerator LabelsShowMapNamesAtLabelPositions()
        {
            map.SetOpen(true);
            yield return null;
            Assert.AreEqual(view.Maps.MapCount, map.LabelCount);
            var pos = MinimapOverlay.LabelPositions(view.Maps);
            for (int m = 0; m < map.LabelCount; m++)
            {
                Assert.AreEqual(view.Maps.Name(m), map.LabelText(m));
                Assert.IsTrue(map.LabelVisible(m), $"map name {m} is hidden");
                map.Viewport.WorldToPanel(pos[2 * m], pos[2 * m + 1], out float px, out float py);
                Assert.AreEqual(px, map.LabelPanelPosition(m).x, 0.5f, $"map {m} x");
                Assert.AreEqual(py, map.LabelPanelPosition(m).y, 0.5f, $"map {m} y");
            }
        }

        [UnityTest]
        public IEnumerator MarkerFollowsCameraPositionAndHeading()
        {
            map.SetOpen(true);
            camGo.transform.position = new Vector3(100.5f, 40f, 250.5f);
            camGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return null;
            map.Viewport.WorldToPanel(100.5f, 250.5f, out float px, out float py);
            Assert.AreEqual(px, map.MarkerPanelPosition.x, 0.5f);
            Assert.AreEqual(py, map.MarkerPanelPosition.y, 0.5f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(270f, map.MarkerAngle)), 0.5f, "facing +x → marker points right");
        }

        [UnityTest]
        public IEnumerator ScreenPanAndZoomUsePanelCoordinates()
        {
            map.SetOpen(true);
            yield return null;
            var vp = map.Viewport;
            vp.ZoomAt(vp.PanelWidth * 0.5f, vp.PanelHeight * 0.5f, 4f);
            var a = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var b = a + new Vector2(40f, 0f);
            Vector2 pa = map.ScreenToPanel(a), pb = map.ScreenToPanel(b);
            Assert.Greater(pb.x - pa.x, 0f, "right on screen = right in viewport");
            float expected = vp.CenterX - (pb.x - pa.x) / vp.PixelsPerColumn;
            map.PanScreen(a, b);
            Assert.AreEqual(expected, vp.CenterX, 1e-3f);

            var pc = map.ScreenToPanel(a);
            vp.PanelToWorld(pc.x, pc.y, out float wx0, out float wz0);
            map.ZoomAtScreen(a, 1.5f);
            vp.PanelToWorld(pc.x, pc.y, out float wx1, out float wz1);
            Assert.AreEqual(6f, vp.Zoom, 1e-4f);
            Assert.AreEqual(wx0, wx1, 1e-3f);
            Assert.AreEqual(wz0, wz1, 1e-3f);
        }

        [UnityTest]
        public IEnumerator BlocksPointerOnlyOverButtonWhenClosed()
        {
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(MinimapView.BlocksPointer(map.ButtonScreenCenter), "button center");
            Assert.IsFalse(MinimapView.BlocksPointer(new Vector2(5f, 5f)), "bottom-left corner when closed");
            map.SetOpen(true);
            Assert.IsTrue(MinimapView.BlocksPointer(new Vector2(5f, 5f)), "bottom-left corner when open");
            yield return null;
        }

        [UnityTest]
        public IEnumerator IslandWorldHasNoMap()
        {
            var go2 = new GameObject("Island");
            go2.AddComponent<TerrainChunkView>(); // mặc định IslandV2, Maps = null
            var map2 = go2.AddComponent<MinimapView>();
            for (int i = 0; i < 3; i++)
                yield return null;
            Assert.IsFalse(map2.IsReady);
            map2.SetOpen(true);
            Assert.IsFalse(map2.IsOpen);
            Assert.IsNull(go2.GetComponentInChildren<Canvas>(true));
            Object.Destroy(go2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DigUpdatesTexturePixel()
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
            var before = map.Texture.GetPixels32()[idx];
            Assert.AreEqual(EditResult.Ok, view.Dig(new Vector3Int(fx, top, fz)));
            yield return null;
            var after = map.Texture.GetPixels32()[idx];
            uint c = MinimapImage.ColumnColor(w, fx, fz);
            Assert.AreEqual(new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, 255), after);
            Assert.AreNotEqual(before, after, "pixel must change");
            Assert.GreaterOrEqual(map.UpdatedPixelCount, 1);
            CollectionAssert.AreEqual(MinimapImage.Build(w), ToBytes(map.Texture.GetPixels32()), "texture = image rebuilt from edited world");
        }

        [UnityTest]
        public IEnumerator LandmarkIconsAtLandmarkPositions()
        {
            map.SetOpen(true);
            yield return null;
            var lms = view.Landmarks;
            Assert.AreEqual(lms.Count, map.LandmarkIconCount);
            for (int i = 0; i < lms.Count; i++)
            {
                var l = lms[i];
                map.Viewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float px, out float py);
                Assert.AreEqual(px, map.LandmarkIconPosition(i).x, 0.01f, "icon x " + i);
                Assert.AreEqual(py, map.LandmarkIconPosition(i).y, 0.01f, "icon y " + i);
                Assert.AreEqual(l.Kind == LandmarkKind.Checkpoint, map.LandmarkIconIsDiamond(i));
                AssertColor((Color)LandmarkStyle.ColorOf(l), map.LandmarkIconColor(i));
                Assert.AreEqual(l.Name, map.LandmarkLabelText(i));
                Assert.IsFalse(map.LandmarkLabelVisible(i), "zoom 1 hides names");
            }
            map.Viewport.ZoomAt(map.Viewport.PanelWidth * 0.5f, map.Viewport.PanelHeight * 0.5f, 3f);
            yield return null;
            Assert.GreaterOrEqual(map.Viewport.Zoom, 2f);
            for (int i = 0; i < lms.Count; i++)
            {
                var l = lms[i];
                Assert.IsTrue(map.LandmarkLabelVisible(i), "zoom ≥ 2 shows name " + i);
                map.Viewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float px, out float py);
                Assert.AreEqual(px, map.LandmarkIconPosition(i).x, 0.01f);
                Assert.AreEqual(py, map.LandmarkIconPosition(i).y, 0.01f);
                Assert.AreEqual(py - 13f, map.LandmarkLabelPosition(i).y, 0.01f);
            }
            map.SetOpen(false);
        }

        static void AssertColor(Color e, Color a)
        {
            Assert.AreEqual(e.r, a.r, 1e-3f);
            Assert.AreEqual(e.g, a.g, 1e-3f);
            Assert.AreEqual(e.b, a.b, 1e-3f);
        }

        static byte[] ToBytes(Color32[] px)
        {
            var b = new byte[px.Length * 4];
            for (int i = 0; i < px.Length; i++)
            {
                b[i * 4] = px[i].r;
                b[i * 4 + 1] = px[i].g;
                b[i * 4 + 2] = px[i].b;
                b[i * 4 + 3] = px[i].a;
            }
            return b;
        }
    }
}
