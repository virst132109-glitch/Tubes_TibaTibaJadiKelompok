using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ============================================================
// TibaTibaJadiKelompok - Bot Utama
// Strategi Greedy: Tembak HANYA saat gun bearing < 3 derajat
// Heuristic: akurasi maksimum = setiap peluru hampir pasti kena
// Gerakan: wall hugging (menyusuri pinggir arena terus-menerus)
// ============================================================
public class TibaTibaJadiKelompok : Bot
{
    static void Main(string[] args) { new TibaTibaJadiKelompok().Start(); }
    TibaTibaJadiKelompok() : base(BotInfo.FromFile("TibaTibaJadiKelompok.json")) { }

    // Flag untuk mengaktifkan Rescan() setelah tembak
    // agar radar tidak kehilangan target saat bot bergerak
    double moveAmount;
    bool peek;

    // Arah belok di sudut arena: 1 = searah jarum jam, -1 = berlawanan
    int wallDir = 1;

    public override void Run()
    {
        // Semua warna hitam legam agar terlihat misterius di arena
        BodyColor   = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        TurretColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        RadarColor  = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        BulletColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam legam
        ScanColor   = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        TracksColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        GunColor    = Color.FromArgb(0x00, 0x00, 0x00); // Hitam

        // Jarak tempuh per sisi = panjang sisi terpanjang arena
        // agar bot pasti sampai ke sudut berikutnya
        moveAmount = Math.Max(ArenaWidth, ArenaHeight);
        peek = false;

        // Posisikan bot menghadap dinding terdekat sebelum mulai wall hugging
        TurnRight(Direction % 90);
        Forward(moveAmount);

        // Setelah sampai dinding, arahkan gun ke luar arena (ke tengah)
        // dan mulai pergerakan searah jarum jam
        peek = true;
        TurnGunRight(90);
        TurnRight(90);

        // Loop utama: jalan terus menyusuri pinggir arena
        while (IsRunning)
        {
            peek = true;
            Forward(moveAmount);  // Jalan sepanjang sisi arena
            peek = false;
            TurnRight(90 * wallDir); // Belok di sudut
        }
    }

    // Event: radar mendeteksi musuh
    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);

        // GREEDY - Fungsi Seleksi:
        // Hitung sudut antara arah gun saat ini dengan posisi musuh
        // Ini adalah inti dari heuristic akurasi maksimum
        double gunBearing = GunBearingTo(e.X, e.Y);

        // Putar gun menuju musuh
        // Kecepatan putar gun dibatasi 20 derajat/turn oleh engine
        TurnGunLeft(gunBearing);

        // Hitung firepower secara greedy:
        // Makin kecil gunBearing = makin akurat = boleh pakai firepower lebih besar
        // Makin dekat musuh = makin besar damage yang dihasilkan
        double fp = Math.Min(3 - Math.Abs(gunBearing), Energy - 0.1);
        fp = Math.Max(0.1, fp); // Minimal firepower 0.1

        // GREEDY: Tembak HANYA jika gun sudah benar-benar mengarah ke musuh
        // Threshold 3 derajat = hit probability sangat tinggi
        if (Math.Abs(gunBearing) <= 3 && GunHeat == 0)
            Fire(fp);

        // Jika peek aktif, scan ulang agar target tidak hilang
        // saat bot berjalan menyusuri dinding
        if (peek) Rescan();
    }

    // Event: bot kena peluru
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Balik arah wall hugging agar pola tidak tertebak musuh
        wallDir *= -1;
    }

    // Event: bot menabrak dinding
    public override void OnHitWall(HitWallEvent e)
    {
        // Mundur sedikit untuk keluar dari dinding
        Back(30);
        // Belok sesuai arah wall hugging saat ini
        TurnRight(90 * wallDir);
        // Reset peek agar tidak Rescan saat posisi belum stabil
        peek = false;
    }

    // Event: bot menabrak bot lain
    public override void OnHitBot(HitBotEvent e)
    {
        // Tentukan arah mundur berdasarkan posisi bot yang ditabrak
        double bearing = BearingTo(e.X, e.Y);
        // Jika bot yang ditabrak ada di depan, mundur
        // Jika ada di belakang, maju
        if (bearing > -90 && bearing < 90) Back(80);
        else Forward(80);
    }

    // Event: ronde baru dimulai - reset semua state
    public override void OnRoundStarted(RoundStartedEvent e)
    {
        peek    = false;
        wallDir = 1;
    }
}