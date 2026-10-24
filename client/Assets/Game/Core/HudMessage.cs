namespace Game.Core
{
    /// <summary>Dòng thông báo HUD (0034 mục 7, brief 0064): hiện khi chữ đổi, ẩn sau Seconds giây. Chữ rỗng → ẩn.</summary>
    public sealed class HudMessage
    {
        float since = float.NegativeInfinity;

        public HudMessage(float seconds) { Seconds = seconds; }

        public float Seconds { get; set; }

        /// <summary>Chữ gần nhất đưa vào Update.</summary>
        public string Text { get; private set; } = "";

        /// <summary>Chữ khác chữ cũ → ghi lại và đặt mốc = now.</summary>
        public void Update(string text, float now)
        {
            if (text == null) text = "";
            if (string.Equals(text, Text)) return;
            Text = text;
            since = now;
        }

        public bool Visible(float now) => Text != "" && now - since < Seconds;
    }
}
