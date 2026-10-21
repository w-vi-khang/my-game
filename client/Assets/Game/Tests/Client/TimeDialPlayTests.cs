using System.Collections;
using Game.Core;
using Game.Core.DayCycle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7f-3 (brief 0024): vòng chọn 4 mốc + dòng "Đang ở buổi" — dựng, mở/đóng, thứ tự, chọn mốc, mờ khi xoay, chặn con trỏ, ẩn khi bản đồ mở.</summary>
    public class TimeDialPlayTests
    {
        const int H = 3_600_000;
        long now;
        double t;
        GameObject lightGo, camGo;
        DayCycleView day;
        TimeDial dial;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            now = 12 * H;
            t = 100.0;
            lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            camGo = new GameObject("Cam");
            camGo.AddComponent<Camera>();
            day = camGo.AddComponent<DayCycleView>();
            day.sun = light;
            day.clockMs = () => now;
            day.secondsNow = () => t;
            dial = camGo.AddComponent<TimeDial>();
            for (int i = 0; i < 5 && !dial.IsReady; i++)
                yield return null;
            Assert.IsTrue(dial.IsReady, "Vòng chưa dựng");
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildsClosedWithPeriodText()
        {
            Assert.IsFalse(dial.IsOpen);
            Assert.IsFalse(dial.RingActive);
            Assert.IsTrue(dial.CanvasActive);
            Assert.AreEqual("Đang ở buổi: Ban ngày", dial.PeriodText);
            Assert.AreEqual(TimeDialShape.Sun, dial.ButtonShape);
            now = 0;
            yield return null;
            Assert.AreEqual("Đang ở buổi: Ban đêm", dial.PeriodText);
            Assert.AreEqual(TimeDialShape.Moon, dial.ButtonShape);
            now = 5 * H;
            yield return null;
            Assert.AreEqual("Đang ở buổi: Bình minh", dial.PeriodText);
            Assert.AreEqual(TimeDialShape.Moon, dial.ButtonShape);
        }

        [UnityTest]
        public IEnumerator ButtonTogglesRing()
        {
            dial.PointerDown(dial.ButtonScreenCenter);
            yield return null;
            Assert.IsTrue(dial.IsOpen);
            Assert.IsTrue(dial.RingActive);
            Assert.IsTrue(TimeDial.IsAnyOpen);
            dial.PointerDown(dial.ButtonScreenCenter);
            yield return null;
            Assert.IsFalse(dial.IsOpen);
            Assert.IsFalse(dial.RingActive);
        }

        [UnityTest]
        public IEnumerator LandmarksClockwiseWithNames()
        {
            dial.SetOpen(true);
            Canvas.ForceUpdateCanvases();
            var noon = dial.LandmarkScreenCenter(TimeLandmark.Noon);
            var midnight = dial.LandmarkScreenCenter(TimeLandmark.Midnight);
            var dawn = dial.LandmarkScreenCenter(TimeLandmark.Dawn);
            var dusk = dial.LandmarkScreenCenter(TimeLandmark.Dusk);
            Assert.Greater(noon.y, midnight.y);
            Assert.Greater(dusk.x, dawn.x);
            Assert.AreEqual(noon.x, midnight.x, 1f);
            Assert.AreEqual(dawn.y, dusk.y, 1f);
            Assert.AreEqual("Giữa trưa", dial.LandmarkLabel(TimeLandmark.Noon));
            Assert.AreEqual("Hoàng hôn", dial.LandmarkLabel(TimeLandmark.Dusk));
            Assert.AreEqual("Giữa đêm", dial.LandmarkLabel(TimeLandmark.Midnight));
            Assert.AreEqual("Bình minh", dial.LandmarkLabel(TimeLandmark.Dawn));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChoosingLandmarkRotatesAndCloses()
        {
            dial.SetOpen(true);
            dial.PointerDown(dial.LandmarkScreenCenter(TimeLandmark.Dawn));
            yield return null;
            Assert.AreEqual(18 * H, dial.LastSelectResult);
            Assert.IsTrue(day.IsRotating);
            Assert.IsFalse(dial.IsOpen);
            Assert.AreEqual(6 * H, day.Day.PhaseMs);
        }

        [UnityTest]
        public IEnumerator CurrentLandmarkHighlightedAndDoesNothing()
        {
            dial.SetOpen(true);
            Assert.IsTrue(dial.IsHighlighted(TimeLandmark.Noon));
            Assert.IsFalse(dial.IsHighlighted(TimeLandmark.Midnight));
            Assert.IsFalse(dial.IsHighlighted(TimeLandmark.Dawn));
            Assert.IsFalse(dial.IsHighlighted(TimeLandmark.Dusk));
            dial.PointerDown(dial.LandmarkScreenCenter(TimeLandmark.Noon));
            yield return null;
            Assert.AreEqual(0, dial.LastSelectResult);
            Assert.IsTrue(dial.IsOpen);
            Assert.IsFalse(day.IsRotating);
            Assert.AreEqual(0, day.Day.OffsetMs);
        }

        [UnityTest]
        public IEnumerator DimmedAndIgnoredWhileRotating()
        {
            day.Select(TimeLandmark.Dawn);
            yield return null;
            Assert.AreEqual(0.4f, dial.ControlsAlpha, 1e-4f);
            dial.SetOpen(true);
            int off = day.Day.OffsetMs;
            dial.PointerDown(dial.LandmarkScreenCenter(TimeLandmark.Dusk));
            Assert.AreEqual(-1, dial.LastSelectResult);
            Assert.AreEqual(off, day.Day.OffsetMs);
            Assert.IsTrue(dial.IsOpen);
            t += 3.1;
            yield return null;
            Assert.AreEqual(1f, dial.ControlsAlpha, 1e-4f);
        }

        [UnityTest]
        public IEnumerator OutsideClosesAndBlocksPointer()
        {
            var outside = new Vector2(5, 5);
            Assert.IsTrue(TimeDial.BlocksPointer(dial.ButtonScreenCenter));
            Assert.IsFalse(TimeDial.BlocksPointer(outside));
            dial.SetOpen(true);
            Assert.IsTrue(TimeDial.BlocksPointer(outside));
            dial.PointerDown(outside);
            Assert.IsFalse(dial.IsOpen);
            Assert.IsTrue(TimeDial.BlocksPointer(outside));
            yield return null;
            Assert.IsFalse(TimeDial.BlocksPointer(outside));
        }

        [UnityTest]
        public IEnumerator HiddenWhileMinimapOpen()
        {
            var go = new GameObject("Terrain");
            var view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            var map = go.AddComponent<MinimapView>();
            for (int i = 0; i < 5 && !map.IsReady; i++)
                yield return null;
            Assert.IsTrue(map.IsReady, "Bản đồ chưa dựng");
            dial.SetOpen(true);
            map.SetOpen(true);
            yield return null;
            Assert.IsFalse(dial.CanvasActive);
            Assert.IsFalse(dial.IsOpen);
            Assert.IsFalse(TimeDial.BlocksPointer(dial.ButtonScreenCenter));
            map.SetOpen(false);
            yield return null;
            Assert.IsTrue(dial.CanvasActive);
            Object.Destroy(go);
        }
    }
}
