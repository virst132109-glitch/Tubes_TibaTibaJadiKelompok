using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ============================================================
// ClosestTarget - Bot Alternatif 3
// Strategi Greedy: Selalu kejar dan tabrak musuh TERDEKAT
// Heuristic: jarak minimum = hit rate tertinggi
//            + memaksimalkan Ram Damage Bonus 30%
// ============================================================
public class ClosestTarget : Bot
{
    static void Main(string[] args) { new ClosestTarget().Start(); }
    ClosestTarget() : base(BotInfo.FromFile("ClosestTarget.json")) { }

    // Arah putar radar saat mencari musuh
    private int turnDir = 1;

    // Data musuh terdekat yang menjadi target
    private double closestX    = 0;
    private double closestY    = 0;
    private double closestDist = double.MaxValue;

    // Kecepatan putar gun yang terkontrol
    // Lebih lambat = terlihat lebih presisi saat mengincar
    private const double GUN_TURN_SPEED = 10.0;

    public override void Run()
    {
        // Warna biru untuk identitas ClosestTarget
        BodyColor   = Color.FromArgb(0x00, 0x00, 0x8B); // Dark Blue
        TurretColor = Color.FromArgb(0x00, 0x00, 0xFF); // Blue
        RadarColor  = Color.FromArgb(0x00, 0xBF, 0xFF); // Deep Sky Blue
        BulletColor = Color.FromArgb(0x87, 0xCE, 0xFA); // Light Blue
        ScanColor   = Color.FromArgb(0x00, 0xFF, 0xFF); // Cyan
        TracksColor = Color.FromArgb(0x00, 0x00, 0x4B); // Very Dark Blue
        GunColor    = Color.FromArgb(0x00, 0x00, 0xCC); // Medium Blue

        while (IsRunning)
        {
            // Radar berputar terus mencari musuh
            // Kecepatan putar radar tidak dibatasi karena
            // kita butuh menemukan musuh terdekat secepat mungkin
            SetTurnRadarRight(360 * turnDir);
            Go();
        }
    }

    // Event: radar mendeteksi musuh
    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);

        // GREEDY - Fungsi Seleksi:
        // Dari semua musuh yang terdeteksi, selalu pilih yang PALING DEKAT
        // Logika: musuh terdekat = peluru paling mudah kena
        //         + mempercepat waktu sampai ke musuh untuk ramming
        if (dist < closestDist)
        {
            // Update target terdekat
            closestDist = dist;
            closestX    = e.X;
            closestY    = e.Y;
        }

        // Arahkan body langsung ke musuh terdekat untuk ramming
        double bearing = BearingTo(closestX, closestY);
        if (bearing >= 0) turnDir = 1;
        else turnDir = -1;

        // Putar body dan maju langsung ke target
        // Ini adalah inti strategi ramming
        SetTurnLeft(bearing);
        SetForward(closestDist + 5); // +5 agar tidak berhenti tepat di target

        // Arahkan gun ke target dengan kecepatan terkontrol
        double gunBearing = GunBearingTo(closestX, closestY);

        // Batasi kecepatan putar gun agar tidak berputar terlalu cepat
        double actualTurn = Math.Max(-GUN_TURN_SPEED,
                            Math.Min(GUN_TURN_SPEED, gunBearing));
        SetTurnGunLeft(actualTurn);

        // Tembak ringan sambil mengejar untuk menguras energi musuh
        // Firepower rendah agar tidak membunuh musuh sebelum ditabrak
        // (membunuh dengan tabrak = Ram Damage Bonus 30%)
        if (Math.Abs(gunBearing) <= 15 && GunHeat == 0)
            SetFire(1.0);
    }

    // Event: bot menabrak bot lain (momen ramming!)
    public override void OnHitBot(HitBotEvent e)
    {
        // GREEDY: Saat nabrak musuh, tembak sekeras mungkin
        // tapi perhitungkan energi musuh agar bisa Ram Bonus
        double en = e.Energy;
        if (en > 16)       Fire(3);   // Musuh kuat: tembak keras
        else if (en > 10)  Fire(2);   // Musuh sedang
        else if (en > 4)   Fire(1);   // Musuh lemah
        else if (en > 2)   Fire(0.5); // Musuh hampir mati
        else if (en > 0.4) Fire(0.1); // Finish musuh

        // Terus tabrak setelah tembak
        SetForward(40);
    }

    // Event: bot menabrak dinding
    public override void OnHitWall(HitWallEvent e)
    {
        // Mundur dan belok agar tidak stuck di dinding
        SetBack(40);
        SetTurnRight(45 * turnDir);
    }

    // Event: bot kena peluru
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Ganti arah sedikit saat kena peluru
        // agar tidak terus kena dari arah yang sama
        turnDir *= -1;
        SetTurnRight(20 * turnDir);
    }

    // Event: ronde baru dimulai - reset semua state
    public override void OnRoundStarted(RoundStartedEvent e)
    {
        closestDist = double.MaxValue;
        turnDir     = 1;
    }
}