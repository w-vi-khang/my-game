using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum TouchSamplePhase { Began, Moved, Stationary, Ended }

    /// <summary>Một ngón chạm trong một khung (px màn hình, y hướng lên). OverUi = đang đè lên bảng/UI.</summary>
    public struct TouchSample
    {
        public int Id;
        public float X, Y;
        public TouchSamplePhase Phase;
        public bool OverUi;

        public TouchSample(int id, float x, float y, TouchSamplePhase phase, bool overUi = false)
        {
            Id = id; X = x; Y = y; Phase = phase; OverUi = overUi;
        }
    }

    /// <summary>Ô nút chữ nhật theo px màn hình (góc dưới-trái, y hướng lên).</summary>
    public struct TouchRect
    {
        public int Id;
        public float X, Y, W, H;

        public TouchRect(int id, float x, float y, float w, float h)
        {
            Id = id; X = x; Y = y; W = w; H = h;
        }

        public bool Contains(float px, float py) => X <= px && px < X + W && Y <= py && py < Y + H;
    }

    /// <summary>
    /// Lõi cử chỉ cảm ứng thuần C# (task cảm ứng (a), brief 0101). Mỗi ngón được gán chủ MỘT LẦN lúc Began
    /// (nút → bỏ qua nếu đè UI → joystick nửa trái → camera nửa phải), giữ tới khi thả.
    /// </summary>
    public sealed class TouchGestures
    {
        enum Kind { Joystick, Camera, Button }

        sealed class Finger
        {
            public int Id;
            public Kind Kind;
            public int ButtonId;
            public float X, Y;
            public float PrevX, PrevY;
            public bool HasPrev;
        }

        readonly float joystickRadiusDp;
        readonly float deadZone;
        readonly float lookDegPerDp;
        readonly List<Finger> fingers = new List<Finger>(8);
        readonly Stack<Finger> pool = new Stack<Finger>();
        readonly List<int> pressed = new List<int>(4);
        Finger joystick;

        public float MoveX { get; private set; }
        public float MoveY { get; private set; }
        public bool JoystickActive { get; private set; }
        public float JoystickCenterX { get; private set; }
        public float JoystickCenterY { get; private set; }
        public float KnobX { get; private set; }
        public float KnobY { get; private set; }
        public float JoystickRadiusPx { get; private set; }
        public float LookYaw { get; private set; }
        public float LookPitch { get; private set; }
        public float ZoomFactor { get; private set; } = 1f;

        public TouchGestures(float joystickRadiusDp, float deadZone, float lookDegPerDp)
        {
            this.joystickRadiusDp = joystickRadiusDp;
            this.deadZone = deadZone;
            this.lookDegPerDp = lookDegPerDp;
        }

        public static float DpToPx(float dp, float dpi) => dp * (dpi <= 0f ? 160f : dpi) / 160f;

        public bool IsHeld(int buttonId)
        {
            for (int i = 0; i < fingers.Count; i++)
                if (fingers[i].Kind == Kind.Button && fingers[i].ButtonId == buttonId) return true;
            return false;
        }

        public bool WasPressed(int buttonId) => pressed.Contains(buttonId);

        public void Reset()
        {
            for (int i = fingers.Count - 1; i >= 0; i--) Release(i);
            joystick = null;
            pressed.Clear();
            MoveX = MoveY = 0f;
            JoystickActive = false;
            JoystickCenterX = JoystickCenterY = KnobX = KnobY = 0f;
            JoystickRadiusPx = 0f;
            LookYaw = LookPitch = 0f;
            ZoomFactor = 1f;
        }

        public void Update(IReadOnlyList<TouchSample> samples, float screenWidth, float dpi, IReadOnlyList<TouchRect> buttons)
        {
            LookYaw = LookPitch = 0f;
            ZoomFactor = 1f;
            pressed.Clear();
            lastDpi = dpi;
            JoystickRadiusPx = DpToPx(joystickRadiusDp, dpi);
            int n = samples == null ? 0 : samples.Count;

            // Ngón đã biết mà khung này không còn → coi như thả.
            for (int i = fingers.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int k = 0; k < n; k++)
                    if (samples[k].Id == fingers[i].Id) { found = true; break; }
                if (!found) Release(i);
            }

            for (int k = 0; k < n; k++)
            {
                TouchSample s = samples[k];
                int idx = IndexOf(s.Id);
                if (idx >= 0)
                {
                    if (s.Phase == TouchSamplePhase.Ended) { Release(idx); continue; }
                    fingers[idx].X = s.X;
                    fingers[idx].Y = s.Y;
                    continue;
                }
                if (s.Phase != TouchSamplePhase.Began) continue;

                int btn = -1;
                if (buttons != null)
                    for (int b = 0; b < buttons.Count; b++)
                        if (buttons[b].Contains(s.X, s.Y)) { btn = buttons[b].Id; break; }
                if (btn >= 0)
                {
                    Finger f = Acquire(s, Kind.Button);
                    f.ButtonId = btn;
                    pressed.Add(btn);
                    continue;
                }
                if (s.OverUi) continue;
                if (s.X < screenWidth * 0.5f)
                {
                    if (joystick != null) continue;
                    joystick = Acquire(s, Kind.Joystick);
                    continue;
                }
                Acquire(s, Kind.Camera);
            }

            UpdateJoystick();
            UpdateCamera();
        }

        void UpdateJoystick()
        {
            if (joystick == null)
            {
                MoveX = MoveY = 0f;
                JoystickActive = false;
                return;
            }
            JoystickActive = true;
            JoystickCenterX = joystick.PrevX;
            JoystickCenterY = joystick.PrevY;
            float r = JoystickRadiusPx > 0f ? JoystickRadiusPx : 1f;
            float vx = (joystick.X - JoystickCenterX) / r;
            float vy = (joystick.Y - JoystickCenterY) / r;
            float len = (float)Math.Sqrt(vx * vx + vy * vy);
            if (len > 1f) { vx /= len; vy /= len; len = 1f; }
            KnobX = JoystickCenterX + vx * r;
            KnobY = JoystickCenterY + vy * r;
            if (len < deadZone) { MoveX = MoveY = 0f; }
            else { MoveX = vx; MoveY = vy; }
        }

        void UpdateCamera()
        {
            Finger a = null, b = null;
            int count = 0;
            for (int i = 0; i < fingers.Count; i++)
            {
                Finger f = fingers[i];
                if (f.Kind != Kind.Camera) continue;
                count++;
                if (a == null || f.Id < a.Id) { b = a; a = f; }
                else if (b == null || f.Id < b.Id) b = f;
            }
            if (count == 1 && a.HasPrev)
            {
                float k = lookDegPerDp * 160f / CurrentDpi;
                LookYaw = (a.X - a.PrevX) * k;
                LookPitch = -(a.Y - a.PrevY) * k;
            }
            else if (count >= 2 && a.HasPrev && b.HasPrev)
            {
                float pdx = a.PrevX - b.PrevX, pdy = a.PrevY - b.PrevY;
                float prevDist = (float)Math.Sqrt(pdx * pdx + pdy * pdy);
                if (prevDist > 1f)
                {
                    float cdx = a.X - b.X, cdy = a.Y - b.Y;
                    ZoomFactor = (float)Math.Sqrt(cdx * cdx + cdy * cdy) / prevDist;
                }
            }
            for (int i = 0; i < fingers.Count; i++)
            {
                Finger f = fingers[i];
                if (f.Kind != Kind.Camera) continue;
                f.PrevX = f.X; f.PrevY = f.Y; f.HasPrev = true;
            }
        }

        float CurrentDpi => lastDpi <= 0f ? 160f : lastDpi;
        float lastDpi = 160f;

        int IndexOf(int id)
        {
            for (int i = 0; i < fingers.Count; i++)
                if (fingers[i].Id == id) return i;
            return -1;
        }

        Finger Acquire(TouchSample s, Kind kind)
        {
            Finger f = pool.Count > 0 ? pool.Pop() : new Finger();
            f.Id = s.Id; f.Kind = kind; f.ButtonId = 0;
            f.X = s.X; f.Y = s.Y; f.PrevX = s.X; f.PrevY = s.Y;
            f.HasPrev = kind == Kind.Camera;
            fingers.Add(f);
            return f;
        }

        void Release(int index)
        {
            Finger f = fingers[index];
            if (f == joystick) joystick = null;
            fingers.RemoveAt(index);
            pool.Push(f);
        }
    }
}
