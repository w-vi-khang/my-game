using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class TouchGesturesTests
    {
        const float W = 1000f, Dpi = 320f;
        static readonly List<TouchRect> NoButtons = new List<TouchRect>();

        TouchGestures g;
        List<TouchRect> buttons;

        [SetUp]
        public void SetUp()
        {
            g = new TouchGestures(60f, 0.15f, 0.4f);
            buttons = new List<TouchRect> { new TouchRect(1, 100f, 100f, 100f, 100f) };
        }

        static TouchSample S(int id, float x, float y, TouchSamplePhase ph, bool ui = false) => new TouchSample(id, x, y, ph, ui);

        void Up(params TouchSample[] s) => g.Update(s, W, Dpi, buttons);

        const TouchSamplePhase B = TouchSamplePhase.Began, M = TouchSamplePhase.Moved, E = TouchSamplePhase.Ended, T = TouchSamplePhase.Stationary;

        [Test]
        public void JoystickBeganSetsCenter()
        {
            Up(S(1, 300, 400, B));
            Assert.IsTrue(g.JoystickActive);
            Assert.AreEqual(300f, g.JoystickCenterX); Assert.AreEqual(400f, g.JoystickCenterY);
            Assert.AreEqual(0f, g.MoveX); Assert.AreEqual(0f, g.MoveY);
        }

        [Test]
        public void JoystickPartialAndClamped()
        {
            Up(S(5, 200, 300, B));
            Up(S(5, 260, 300, M));
            Assert.AreEqual(0.5f, g.MoveX, 1e-4f); Assert.AreEqual(0f, g.MoveY, 1e-4f);
            Up(S(5, 200, 540, M));
            Assert.AreEqual(0f, g.MoveX, 1e-4f); Assert.AreEqual(1f, g.MoveY, 1e-4f);
            Assert.AreEqual(200f, g.KnobX, 1e-3f); Assert.AreEqual(420f, g.KnobY, 1e-3f);
        }

        [Test]
        public void JoystickDeadZone()
        {
            Up(S(5, 200, 300, B));
            Up(S(5, 210, 300, M));
            Assert.AreEqual(0f, g.MoveX); Assert.AreEqual(0f, g.MoveY);
        }

        [Test]
        public void JoystickEndedReleases()
        {
            Up(S(5, 200, 300, B));
            Up(S(5, 260, 300, M));
            Up(S(5, 260, 300, E));
            Assert.AreEqual(0f, g.MoveX); Assert.AreEqual(0f, g.MoveY);
            Assert.IsFalse(g.JoystickActive);
        }

        [Test]
        public void CameraSwipeYaw()
        {
            Up(S(2, 700, 300, B));
            Up(S(2, 800, 300, M));
            Assert.AreEqual(20f, g.LookYaw, 1e-3f); Assert.AreEqual(0f, g.LookPitch, 1e-3f);
            Up(S(2, 800, 300, T));
            Assert.AreEqual(0f, g.LookYaw);
        }

        [Test]
        public void CameraSwipeUpDecreasesPitch()
        {
            Up(S(2, 700, 300, B));
            Up(S(2, 700, 400, M));
            Assert.AreEqual(-20f, g.LookPitch, 1e-3f);
        }

        [Test]
        public void TwoCameraFingersZoom()
        {
            Up(S(2, 650, 300, B), S(3, 750, 300, B));
            Up(S(2, 600, 300, M), S(3, 800, 300, M));
            Assert.AreEqual(2f, g.ZoomFactor, 1e-4f);
            Assert.AreEqual(0f, g.LookYaw);
        }

        [Test]
        public void ButtonHeldAndPressed()
        {
            Up(S(4, 150, 150, B));
            Assert.IsTrue(g.IsHeld(1)); Assert.IsTrue(g.WasPressed(1));
            Assert.IsFalse(g.JoystickActive);
            Up(S(4, 150, 150, T));
            Assert.IsFalse(g.WasPressed(1)); Assert.IsTrue(g.IsHeld(1));
            Up(S(4, 150, 150, E));
            Assert.IsFalse(g.IsHeld(1));
        }

        [Test]
        public void OverUiIgnored()
        {
            Up(S(1, 200, 300, B, true), S(2, 700, 300, B, true));
            Up(S(1, 300, 300, M, true), S(2, 800, 300, M, true));
            Assert.IsFalse(g.JoystickActive);
            Assert.AreEqual(0f, g.LookYaw);
        }

        [Test]
        public void SecondLeftFingerIgnored()
        {
            Up(S(1, 200, 300, B));
            Up(S(1, 200, 300, T), S(2, 300, 500, B));
            Up(S(1, 200, 300, T), S(2, 400, 500, M));
            Assert.AreEqual(0f, g.LookYaw);
            Assert.AreEqual(200f, g.JoystickCenterX); Assert.AreEqual(300f, g.JoystickCenterY);
        }

        [Test]
        public void MissingFingerReleases()
        {
            Up(S(1, 200, 300, B));
            Up();
            Assert.IsFalse(g.JoystickActive);
        }

        [Test]
        public void DpToPxConversion()
        {
            Assert.AreEqual(48f, TouchGestures.DpToPx(48f, 0f));
            Assert.AreEqual(96f, TouchGestures.DpToPx(48f, 320f));
        }

        [Test]
        public void ResetClearsOutputs()
        {
            Up(S(1, 200, 300, B), S(2, 700, 300, B), S(4, 150, 150, B));
            Up(S(1, 260, 300, M), S(2, 800, 300, M), S(4, 150, 150, T));
            g.Reset();
            Assert.IsFalse(g.JoystickActive); Assert.IsFalse(g.IsHeld(1));
            Assert.AreEqual(0f, g.MoveX); Assert.AreEqual(0f, g.LookYaw); Assert.AreEqual(1f, g.ZoomFactor);
        }
    }
}
