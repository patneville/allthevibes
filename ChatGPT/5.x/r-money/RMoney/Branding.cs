using System.Drawing;

namespace RMoney
{
    public static class Branding
    {
        public const string AppName = "r-money";
        public const string Tagline = "Spend → Save → Invest, in the right order.";
        public const string Version = "1.0.0";

        public static readonly Color Bg = Color.FromArgb(11, 18, 32);
        public static readonly Color Panel = Color.FromArgb(18, 28, 48);
        public static readonly Color Accent = Color.FromArgb(0, 214, 170);
        public static readonly Color Text = Color.White;
        public static readonly Color Muted = Color.FromArgb(160, 170, 190);

        public static Font H1 => new Font("Segoe UI", 16f, FontStyle.Bold);
        public static Font H2 => new Font("Segoe UI", 11f, FontStyle.Bold);
        public static Font Body => new Font("Segoe UI", 10f, FontStyle.Regular);
        public static Font BodyLarge => new Font("Segoe UI", 10.5f, FontStyle.Regular);
        public static Font Mono => new Font("Consolas", 9.5f, FontStyle.Regular);
    }
}