using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ============================================================
// AggressiveHunter - Bot Alternatif 1
// Strategi Greedy: Selalu serang musuh dengan energi terendah
// Heuristic: target energy minimum = peluang kill tertinggi
// Gerakan: angka 8 via SetForward/SetTurnRight per tick
//          ANTI-STUCK: tidak ada blocking command sama sekali
// ============================================================
public class AggressiveHunter : Bot
{
    static void Main(string[] args) { new AggressiveHunter().Start(); }
    AggressiveHunter() : base(BotInfo.FromFile("AggressiveHunter.json")) { }

    // Data target terkunci
    private int    lockedId     = -1;
    private double lockedEnergy = double.MaxValue;
    private double lockedX      = 0;
    private double lockedY      = 0;
    private double lockedSpeed  = 0;
    private double lockedDir    = 0;
    private double lockedDist   = double.MaxValue;

    // State mesin gerakan angka 8
    // Angka 8 dibagi 4 fase, masing-masing 9 tick
    private int phase     = 0; // Fase saat ini (0-3)
    private int phaseTick = 0; // Tick dalam fase ini

    // Konstanta gerakan
    private const int    TICKS_PER_PHASE = 9;   // Durasi tiap fase
    private const double TURN_PER_TICK   = 20.0; // Derajat belok per tick
    private const double MOVE_PER_TICK   = 20.0; // Jarak maju per tick

    // Kecepatan putar gun terkontrol
    private const double GUN_TURN_SPEED = 12.0;

    public override void Run()
    {
        BodyColor   = Color.FromArgb(0x8B, 0x00, 0x00);
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00);
        RadarColor  = Color.FromArgb(0xFF, 0x45, 0x00);
        BulletColor = Color.FromArgb(0xFF, 0x00, 0x00);
        ScanColor   = Color.FromArgb(0xFF, 0x00, 0x00);
        TracksColor = Color.FromArgb(0x4A, 0x00, 0x00);
        GunColor    = Color.FromArgb(0xCC, 0x00, 0x00);

        // Loop utama: HANYA berisi Set-commands dan Go()
        // Tidak ada Forward/TurnRight blocking sama sekali
        // Semua logika gerak diatur via phase state machine
        while (IsRunning)
        {
            // Jalankan satu tick gerakan angka 8
            ExecuteFigure8Tick();

            // Radar terus berputar mencari musuh
            SetTurnRadarRight(360);

            // Eksekusi semua perintah sekaligus
            Go();
        }
    }

    // State machine gerakan angka 8 per tick
    // Tidak ada blocking = tidak bisa stuck
    private void ExecuteFigure8Tick()
    {
        phaseTick++;

        // Setiap fase terdiri dari TICKS_PER_PHASE tick
        // 4 fase = 2 lingkaran = 1 angka 8
        switch (phase % 4)
        {
            case 0:
                // Fase 0: Lingkaran kanan atas
                SetTurnRight(TURN_PER_TICK);
                SetForward(MOVE_PER_TICK);
                break;
            case 1:
                // Fase 1: Lingkaran kanan bawah
                SetTurnRight(TURN_PER_TICK);
                SetForward(MOVE_PER_TICK);
                break;
            case 2:
                // Fase 2: Lingkaran kiri atas
                SetTurnLeft(TURN_PER_TICK);
                SetForward(MOVE_PER_TICK);
                break;
            case 3:
                // Fase 3: Lingkaran kiri bawah
                SetTurnLeft(TURN_PER_TICK);
                SetForward(MOVE_PER_TICK);
                break;
        }

        // Pindah ke fase berikutnya setelah TICKS_PER_PHASE tick
        if (phaseTick >= TICKS_PER_PHASE)
        {
            phaseTick = 0;
            phase = (phase + 1) % 4;
        }
    }

    // Paksa pindah fase saat stuck/nabrak
    // Ini kunci anti-stuck: langsung skip ke fase baru
    private void BreakOutOfStuck()
    {
        phaseTick = 0;
        phase     = (phase + 2) % 4; // Skip 2 fase = arah berlawanan
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist   = DistanceTo(e.X, e.Y);
        double energy = e.Energy;

        // GREEDY: pilih target dengan energi terendah
        bool isWeaker        = energy < lockedEnergy;
        bool sameButCloser   = energy == lockedEnergy && dist < lockedDist;
        bool isCurrentTarget = e.ScannedBotId == lockedId;

        if (lockedId == -1 || isWeaker || sameButCloser || isCurrentTarget)
        {
            lockedId     = e.ScannedBotId;
            lockedEnergy = energy;
            lockedX      = e.X;
            lockedY      = e.Y;
            lockedSpeed  = e.Speed;
            lockedDir    = e.Direction;
            lockedDist   = dist;
        }

        if (e.ScannedBotId != lockedId) return;

        // Hitung firepower berdasarkan jarak
        double fp;
        if (lockedDist < 150)      fp = 3.0;
        else if (lockedDist < 300) fp = 2.0;
        else if (lockedDist < 500) fp = 1.0;
        else                       fp = 0.5;

        // Sesuaikan jika target hampir mati
        if (lockedEnergy < 10)
            fp = Math.Min(fp, lockedEnergy / 2.0 + 0.1);

        // Linear prediction: prediksi posisi musuh saat peluru tiba
        double bulletSpeed = 20 - 3 * fp;
        double travelTime  = lockedDist / bulletSpeed;
        double futureX = lockedX + Math.Sin(lockedDir * Math.PI / 180)
                         * lockedSpeed * travelTime;
        double futureY = lockedY + Math.Cos(lockedDir * Math.PI / 180)
                         * lockedSpeed * travelTime;

        // Arahkan gun ke prediksi posisi musuh
        double gunBearing = GunBearingTo(futureX, futureY);
        double actualTurn = Math.Max(-GUN_TURN_SPEED,
                            Math.Min(GUN_TURN_SPEED, gunBearing));
        SetTurnGunLeft(actualTurn);

        // Tembak jika akurat dan gun tidak panas
        if (Math.Abs(gunBearing) <= 8 && GunHeat == 0)
            Fire(fp);

        // Radar lock ke target
        SetTurnRadarRight(NormalizeRelativeAngle(
            BearingTo(lockedX, lockedY) - RadarDirection + Direction) * 2);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Kena peluru: langsung pindah fase untuk keluar dari posisi berbahaya
        BreakOutOfStuck();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // ANTI-STUCK UTAMA: saat nabrak dinding
        // Set mundur SEKARANG juga via Set-command
        // lalu langsung ganti fase agar angka 8 menjauh dari dinding
        SetBack(60);
        BreakOutOfStuck();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak bot: mundur dan ganti fase
        double bearing = BearingTo(e.X, e.Y);
        if (bearing > -90 && bearing < 90) SetBack(60);
        else SetForward(60);
        BreakOutOfStuck();
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        // Target mati: reset untuk cari target baru
        if (e.VictimId == lockedId)
        {
            lockedId     = -1;
            lockedEnergy = double.MaxValue;
        }
    }

    public override void OnRoundStarted(RoundStartedEvent e)
    {
        lockedId     = -1;
        lockedEnergy = double.MaxValue;
        phase        = 0;
        phaseTick    = 0;
    }
}