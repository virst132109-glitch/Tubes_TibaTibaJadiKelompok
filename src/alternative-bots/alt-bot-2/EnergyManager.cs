using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ============================================================
// EnergyManager - Bot Alternatif 2
// Strategi Greedy: Pilih firepower OPTIMAL berdasarkan jarak
// Heuristic: fp = min(3.0, 500/distance)
// Gerakan: zigzag tenang via state machine per tick
//          ANTI-STUCK: murni Set-commands, tidak ada blocking
// ============================================================
public class EnergyManager : Bot
{
    static void Main(string[] args) { new EnergyManager().Start(); }
    EnergyManager() : base(BotInfo.FromFile("EnergyManager.json")) { }

    // Arah zigzag: 1 = kanan, -1 = kiri
    private int moveDir = 1;

    // Penghitung tick untuk ganti arah zigzag
    private int tickCount = 0;

    // Berapa tick sebelum ganti arah (lebih besar = lebih jarang ganti)
    private const int ZIGZAG_INTERVAL = 20;

    // Sudut belok zigzag (kecil = tenang, tidak panik)
    private const double ZIGZAG_ANGLE = 25.0;

    // Jarak maju per tick
    private const double MOVE_DIST = 30.0;

    // Kecepatan putar gun (terkontrol agar tidak berputar terlalu cepat)
    private const double GUN_TURN_SPEED = 12.0;

    // Flag apakah sedang ada target
    private bool hasTarget = false;

    // Posisi target terakhir
    private double targetX = 0, targetY = 0;

    public override void Run()
    {
        BodyColor   = Color.FromArgb(0x00, 0x64, 0x00);
        TurretColor = Color.FromArgb(0x00, 0xFF, 0x00);
        RadarColor  = Color.FromArgb(0xAD, 0xFF, 0x2F);
        BulletColor = Color.FromArgb(0x00, 0xFF, 0x00);
        ScanColor   = Color.FromArgb(0x00, 0xFF, 0x00);
        TracksColor = Color.FromArgb(0x00, 0x40, 0x00);
        GunColor    = Color.FromArgb(0x00, 0x80, 0x00);

        // Loop utama: HANYA Set-commands dan Go()
        // Tidak ada blocking command sama sekali = tidak bisa stuck
        while (IsRunning)
        {
            tickCount++;

            // Ganti arah zigzag setiap ZIGZAG_INTERVAL tick
            // Ini membuat gerakan teratur dan tidak panik
            if (tickCount % ZIGZAG_INTERVAL == 0)
            {
                moveDir *= -1;
                // Belok dengan sudut kecil = tenang, sesuai nama EnergyManager
                SetTurnRight(ZIGZAG_ANGLE * moveDir);
            }

            // Selalu maju ke depan (tidak mundur kecuali terpaksa)
            SetForward(MOVE_DIST);

            // Radar terus berputar mencari musuh
            SetTurnRadarRight(45);

            // Eksekusi semua perintah sekaligus
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);
        hasTarget = true;
        targetX   = e.X;
        targetY   = e.Y;

        // GREEDY - Fungsi Seleksi (Firepower Optimal):
        // Hitung firepower yang memaksimalkan damage per energi
        // Makin dekat = firepower lebih besar = damage lebih besar
        // Makin jauh  = firepower lebih kecil = hemat energi
        double fp = Math.Max(0.1, Math.Min(3.0, 500.0 / dist));

        // Arahkan gun ke musuh dengan kecepatan terkontrol
        double gunBearing = GunBearingTo(e.X, e.Y);
        double actualTurn = Math.Max(-GUN_TURN_SPEED,
                            Math.Min(GUN_TURN_SPEED, gunBearing));
        SetTurnGunLeft(actualTurn);

        // Tembak jika gun sudah cukup akurat
        if (Math.Abs(gunBearing) <= 8 && GunHeat == 0)
            SetFire(fp);

        // Jaga jarak optimal: tidak terlalu dekat, tidak terlalu jauh
        if (dist > 350)
        {
            // Musuh jauh: pelan-pelan mendekati
            double bearing = BearingTo(e.X, e.Y);
            SetTurnRight(NormalizeRelativeAngle(bearing) * 0.3);
            SetForward(MOVE_DIST);
        }
        else if (dist < 120)
        {
            // Musuh terlalu dekat: mundur sedikit
            SetBack(MOVE_DIST);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Kena peluru: langsung ganti arah
        moveDir   *= -1;
        tickCount  = 0;
        SetTurnRight(ZIGZAG_ANGLE * 2 * moveDir);
        SetForward(MOVE_DIST * 2);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // ANTI-STUCK: saat nabrak dinding
        // Mundur dan reset zigzag ke arah baru
        moveDir   *= -1;
        tickCount  = 0;
        SetBack(50);
        SetTurnRight(90); // Belok 90 derajat menjauhi dinding
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak bot: mundur atau maju tergantung posisi
        double bearing = BearingTo(e.X, e.Y);
        if (bearing > -90 && bearing < 90)
            SetBack(50);
        else
            SetForward(50);
        moveDir  *= -1;
        tickCount = 0;
    }

    public override void OnRoundStarted(RoundStartedEvent e)
    {
        moveDir   = 1;
        tickCount = 0;
        hasTarget = false;
    }
}